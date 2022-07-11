using System.Globalization;
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
var dbOptions = new DbContextOptionsBuilder()
    .UseSqlite("Filename=test.db")
    .Options;
var parser = new Parser(settings =>
{
    settings.AutoHelp = true;
    settings.CaseInsensitiveEnumValues = true;
    settings.HelpWriter = Console.Error;
});
await parser.ParseArguments<UpdateOptions, BatchOptions, PrintSchemaOptions>(args)
    .MapResult<UpdateOptions, BatchOptions, PrintSchemaOptions, Task>(RunUpdate, RunBatch, PrintSchema, _ => Task.CompletedTask);

async Task RunUpdate(UpdateOptions updateOptions)
{
    var libCalClient = new LibCalClient();
    await libCalClient.Authorize(config["LIBCAL_CLIENT_ID"], config["LIBCAL_CLIENT_SECRET"]);
    await using var db = new Database(dbOptions);

    if (updateOptions.Sources.HasFlag(DataSources.Events))
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
                @event.Registrants = registrations[@event.Id];
                foreach (var category in @event.Category) { category.EventId = @event.Id; }

                db.Upsert(@event);
            }
        }
    }

    if (updateOptions.Sources.HasFlag(DataSources.Appointments))
    {
        var bookings = await libCalClient.GetAppointmentBookings(updateOptions.FromDate, updateOptions.ToDate);
        var questionsSeen = new HashSet<long>();
        var usersSeen = new HashSet<long>();
        foreach (var booking in bookings)
        {
            var newQuestionIds = new List<long>();
            foreach (var answer in booking.Answers)
            {
                answer.BookingId = booking.Id;
                // HashSet.Add returns true only if the element was not already in the set, so this filters out question ids we already saw
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
                    db.Upsert(user);
                }
                catch (FlurlHttpException exception)
                {
                    var response = await exception.GetResponseStringAsync();
                    if (response == "No user/data found. Ensure user has MyScheduler enabled.")
                    {
                        // TODO: is it ok to just skip these?
                    }
                    else { throw; }
                }
            }
        }
    }

    if (updateOptions.Sources.HasFlag(DataSources.Spaces))
    {
        var bookings = await libCalClient.GetSpaceBookings(updateOptions.FromDate, updateOptions.ToDate);
        foreach (var booking in bookings) { db.Upsert(booking); }
    }

    await db.SaveChangesAsync();
}

async Task RunBatch(BatchOptions batchOptions)
{
    await using var db = new Database(dbOptions);
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
                        FirstName = csv.GetField("First Name"),
                        LastName = csv.GetField("Last Name"),
                        Email = csv.GetField("Email"),
                        Account = csv.GetField("Account"),
                        PublicNickname = csv.GetField("Booking Nickname"),
                        FromDate = fromDate,
                        ToDate = duration is null ? null : fromDate?.AddMinutes(int.Parse(duration)),
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
                    FirstName = csv.GetField("First Name"),
                    LastName = csv.GetField("Last Name"),
                    Email = csv.GetField("Email"),
                    PublicNickname = csv.GetField("Public Nickname"),
                    Account = csv.GetField("Account"),
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
    await using var db = new Database(dbOptions);
    Console.WriteLine(db.Database.GenerateCreateScript());
}