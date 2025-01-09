using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CommandLine;
using CsvHelper;
using CsvHelper.Configuration;
using dotenv.net;
using Flurl.Http;
using LibCalTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileSystemGlobbing;

DotEnv.Load();
var config = new ConfigurationBuilder().AddEnvironmentVariables().Build();
var parser = new Parser(settings =>
{
    settings.AutoHelp = true;
    settings.CaseInsensitiveEnumValues = true;
    settings.HelpWriter = Console.Error;
});
await parser.ParseArguments<UpdateOptions, BatchOptions, PrintSchemaOptions>(args)
    .MapResult<UpdateOptions, BatchOptions, PrintSchemaOptions, Task>(RunUpdate, RunBatch, PrintSchema, NoOpOnError);

async Task RunUpdate(UpdateOptions updateOptions)
{
    try
    {
        var libCalClient = new LibCalClient();
        await libCalClient.Authorize(config["LIBCAL_CLIENT_ID"], config["LIBCAL_CLIENT_SECRET"]);
        await using var db = new Database(config);

        bool Updating(DataSources source) => updateOptions.Sources.HasFlag(source);

        if (Updating(DataSources.Events))
        {
            var calendarIds = await libCalClient.GetCalendarIds();
            foreach (var calendarId in calendarIds)
            {
                var events = await libCalClient.GetEvents(calendarId, updateOptions.FromDate, updateOptions.ToDate);
                if (!events.Any()) { continue; }

                // Should the number of ids being sent per call be limited? Haven't hit the API max yet
                var registrations =
                    (await libCalClient.GetRegistrations(events.Select(e => e.Id)))
                    .ToDictionary(r => r.EventId, r => r.Registrants);
                // @ sign because event is a reserved keyword
                foreach (var @event in events)
                {
                    // This line would throw if the above call didn't return an entry for one of the ids
                    @event.Registrants = registrations[@event.Id];
                    foreach (var registrant in @event.Registrants) { registrant.UserHash = Hash(registrant.Email); }
                    foreach (var category in @event.Category) { category.EventId = @event.Id; }
                    // just truncate strings longer than 2000 for oracle
                    @event.Description = Truncate(@event.Description, 2000);
                    @event.MoreInfo = Truncate(@event.MoreInfo, 2000);
                    db.Upsert(@event);
                }
            }
        }

        if (Updating(DataSources.Appointments))
        {
            var bookings = await libCalClient.GetAppointmentBookings(updateOptions.FromDate, updateOptions.ToDate);
            // HashSet.Add returns true only if the element was not already in the set,
            // so these are used to filter out ids we already saw on this run
            var questionsSeen = new HashSet<long>();
            var usersSeen = new HashSet<long>();
            foreach (var booking in bookings)
            {
                booking.UserHash = Hash(booking.Email);
                var newQuestionIds = new List<long>();
                foreach (var answer in booking.Answers)
                {
                    answer.BookingId = booking.Id;
                    answer.Answer = Truncate(answer.Answer, 2000);
                    if (questionsSeen.Add(answer.QuestionId)) { newQuestionIds.Add(answer.QuestionId); }
                }

                if (newQuestionIds.Any())
                {
                    foreach (var question in await libCalClient.GetAppointmentQuestions(newQuestionIds))
                    {
                        // If question.Options is null, assign an empty list to it
                        foreach (var option in question.Options ??= new List<QuestionOption>())
                        {
                            option.QuestionId = question.Id;
                        }

                        db.Upsert(question);
                    }
                }

                db.Upsert(booking);
                if (usersSeen.Add(booking.UserId))
                {
                    try
                    {
                        var user = await libCalClient.GetAppointmentUser(booking.UserId);
                        user.Description = Truncate(user.Description, 2000);
                        db.Upsert(user);
                    }
                    catch (FlurlHttpException exception)
                    {
                        var response = await exception.GetResponseStringAsync();
                        if (response == "No user/data found. Ensure user has MyScheduler enabled." ||
                            response == "no user/data found. ensure user has appointments enabled.")
                        {
                            // just skip these for now
                        }
                        else { throw; }
                    }
                }
            }
        }

        if (Updating(DataSources.Spaces))
        {
            var bookings = await libCalClient.GetSpaceBookings(updateOptions.FromDate, updateOptions.ToDate);
            foreach (var booking in bookings)
            {
                booking.UserHash = Hash(booking.Account);
                db.Upsert2(booking);
            }
        }

        await db.SaveChangesAsync();
    }
    catch (FlurlHttpException exception)
    {
        Console.Error.WriteLine(await exception.GetResponseStringAsync());
        throw;
    }
}

async Task RunBatch(BatchOptions batchOptions)
{
    await using var db = new Database(config);
    // This is used to expand out glob/wildcard patterns in the input
    var fileMatcher = new Matcher();
    fileMatcher.AddIncludePatterns(batchOptions.Files);
    foreach (var path in fileMatcher.GetResultsInFullPath(Directory.GetCurrentDirectory()))
    {
        Console.WriteLine(path);
        using var reader = new StreamReader(path);
        // These files sometimes have more headers than actual data, which would throw an exception when reading
        // We override that by setting MissingFieldFound to a no-op function
        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture) { MissingFieldFound = _ => { } };
        using var csv = new CsvReader(reader, csvConfig);
        await csv.ReadAsync();
        if (string.IsNullOrEmpty(csv.GetField(2)))
        {
            // Old room booking format
            await csv.ReadAsync();
            csv.ReadHeader();
            while (await csv.ReadAsync())
            {
                if (string.IsNullOrEmpty(csv.GetField(2)))
                {
                    // Skip empty & header lines between datasets
                    await csv.ReadAsync();
                    await csv.ReadAsync();
                }
                else
                {
                    var fromDate = ConstructDate("Date", "Start Time");
                    var duration = csv.GetField("Duration (minutes)");
                    db.Add(new ArchivedSpaceBooking
                    {
                        // FirstName = csv.GetField("First Name"),
                        // LastName = csv.GetField("Last Name"),
                        // Email = csv.GetField("Email"),
                        // Account = csv.GetField("Account"),
                        // PublicNickname = csv.GetField("Booking Nickname"),
                        UserHash = Hash(csv.GetField("Account")),
                        FromDate = fromDate,
                        ToDate = string.IsNullOrEmpty(duration) ? null : fromDate?.AddMinutes(int.Parse(duration)),
                        CreatedDate = ConstructDate("Booking Created"),
                        Status = csv.GetField("Status"),
                        ShowedUp = csv.GetField("User Showed Up?"),
                        SpaceName = csv.GetField("Room"),
                    });
                }
            }
        }
        else
        {
            csv.ReadHeader();
            while (await csv.ReadAsync())
            {
                db.Add(new ArchivedSpaceBooking
                {
                    BookingId = csv.GetField("Booking ID"),
                    SpaceId = csv.GetField("Space ID"),
                    SpaceName = csv.GetField("Space Name"),
                    Location = csv.GetField("Location"),
                    Zone = csv.GetField("Zone"),
                    Category = csv.GetField("Category"),
                    // FirstName = csv.GetField("First Name"),
                    // LastName = csv.GetField("Last Name"),
                    // Email = csv.GetField("Email"),
                    // PublicNickname = csv.GetField("Public Nickname"),
                    // Account = csv.GetField("Account"),
                    UserHash = Hash(csv.GetField("Account")),
                    FromDate = ConstructDate("From Date", "From Time"),
                    ToDate = ConstructDate("To Date", "To Time"),
                    CreatedDate = ConstructDate("Created Date", "Created Time"),
                    EventId = csv.GetField("Event ID"),
                    EventTitle = csv.GetField("Event Title"),
                    EventStart = ConstructDate("Event Start"),
                    EventEnd = ConstructDate("Event End"),
                    Status = csv.GetField("Status"),
                    CancelledByUser = csv.GetField("Cancelled By User"),
                    CancelledAt = ConstructDate("Cancelled At"),
                    ShowedUp = csv.GetField("Showed Up"),
                    CheckedInDate = ConstructDate("Checked In Date", "Checked In Time"),
                    CheckedOutDate = ConstructDate("Checked Out Date", "Checked Out Time"),
                    Cost = csv.GetField("Cost"),
                    BookingFormAnswers = csv.GetField("Booking Form Answers"),
                });
            }
        }

        DateTime? ConstructDate(string datePart, string timePart = null)
        {
            var date = datePart is null ? null : csv.GetField(datePart);
            var time = timePart is null ? null : csv.GetField(timePart);
            if (string.IsNullOrEmpty(date)) { return null; }
            return string.IsNullOrEmpty(time) ? DateTime.Parse(date) : DateTime.Parse(date + " " + time);
        }
    }

    await db.SaveChangesAsync();
}

async Task PrintSchema(PrintSchemaOptions _)
{
    await using var db = new Database(config);
    Console.WriteLine(db.Database.GenerateCreateScript());
}

Task NoOpOnError(IEnumerable<Error> _) => Task.CompletedTask;

string Truncate(string str, int len) => str.Length > len ? str[..len] : str;

string Hash(string str) =>
    Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(str.ToLowerInvariant()))).ToLowerInvariant();