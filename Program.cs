using dotenv.net;
using Flurl.Http;
using LibCalTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

DotEnv.Load();
var config = new ConfigurationBuilder().AddEnvironmentVariables().Build();
var libCalClient = new LibCalClient();
await libCalClient.Authorize(config["LIBCAL_CLIENT_ID"], config["LIBCAL_CLIENT_SECRET"]);
var dbOptions = new DbContextOptionsBuilder()
    .UseSqlite("Filename=test.db")
    .Options;
await using var db = new Database(dbOptions);
db.Database.EnsureCreated();
var fromDate = DateTime.Today.AddMonths(-3);
var toDate = DateTime.Today;

await UpdateEvents(fromDate, toDate);
await UpdateAppointments(fromDate, toDate);
await UpdateSpaces();

await db.SaveChangesAsync();

async Task UpdateEvents(DateTime fromDate, DateTime toDate)
{
    var calendarIds = await libCalClient.GetCalendarIds();
    foreach (var calendarId in calendarIds)
    {
        var events = await libCalClient.GetEvents(calendarId, fromDate, toDate);
        if (!events.Any()) continue;
        // Should the number of ids being sent per call be limited? Haven't hit the API max yet
        var registrations = (await libCalClient.GetRegistrations(events.Select(e => e.Id)))
            .ToDictionary(r => r.EventId, r => r.Registrants);
        foreach (var @event in events)
        {
            @event.Registrants = registrations[@event.Id];
            foreach (var category in @event.Category)
            {
                category.EventId = @event.Id;
            }
            db.Upsert(@event);
        }
    }
}

async Task UpdateAppointments(DateTime fromDate, DateTime toDate)
{
    var bookings = await libCalClient.GetAppointmentBookings(fromDate, toDate);
    var questionsSeen = new HashSet<long>();
    var usersSeen = new HashSet<long>();
    foreach (var booking in bookings)
    {
        var newQuestionIds = new List<long>();
        foreach (var answer in booking.Answers)
        {
            answer.BookingId = booking.Id;
            // HashSet.Add returns true only if the element was not already in the set, so this filters out question ids we already saw
            if (questionsSeen.Add(answer.QuestionId))
                newQuestionIds.Add(answer.QuestionId);
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
                else throw;
            }
        }
    }
}

async Task UpdateSpaces()
{
    var bookings = await libCalClient.GetSpaceBookings();
    foreach (var booking in bookings)
    {
        db.Upsert(booking);
    }
}