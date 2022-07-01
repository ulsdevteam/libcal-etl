using System.Globalization;
using CommandLine;
using CsvHelper;
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
await parser.ParseArguments<UpdateOptions, BatchOptions>(args)
    .MapResult<UpdateOptions, BatchOptions, Task>(RunUpdate, RunBatch, _ => Task.CompletedTask);

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
        var bookings = await libCalClient.GetSpaceBookings();
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
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        if (string.IsNullOrEmpty(csv.GetField(csv.ColumnCount -1)))
        {
            // Old room booking format
            do
            {
                csv.Read();
                csv.ReadHeader();
            } while (await csv.ReadAsync());
        }
        else
        {
            csv.ReadHeader();
            while (await csv.ReadAsync())
            {
                // TODO: some sort of scheme to generate an id
                var booking = new SpaceBooking();
                db.Add(booking);
            }
        }
    }

    await db.SaveChangesAsync();
}