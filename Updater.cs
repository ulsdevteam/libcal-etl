using Flurl.Http;
using LibCalTypes;

class Updater
{
    public Updater(Database database, LibCalClient libCalClient)
    {
        Database = database;
        LibCalClient = libCalClient;
    }

    Database Database { get; }
    LibCalClient LibCalClient { get; }

    public async Task UpdateEvents(DateTime fromDate, DateTime toDate)
    {
        var calendarIds = await LibCalClient.GetCalendarIds();
        foreach (var calendarId in calendarIds)
        {
            var events = await LibCalClient.GetEvents(calendarId, fromDate, toDate);
            if (!events.Any()) continue;
            // Should the number of ids being sent per call be limited? Haven't hit the API max yet
            var registrations = (await LibCalClient.GetRegistrations(events.Select(e => e.Id)))
                .ToDictionary(r => r.EventId, r => r.Registrants);
            foreach (var @event in events)
            {
                @event.Registrants = registrations[@event.Id];
                foreach (var category in @event.Category) category.EventId = @event.Id;
                Database.Upsert(@event);
            }
        }
    }

    public async Task UpdateAppointments(DateTime fromDate, DateTime toDate)
    {
        var bookings = await LibCalClient.GetAppointmentBookings(fromDate, toDate);
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
                foreach (var question in await LibCalClient.GetAppointmentQuestions(newQuestionIds))
                {
                    // If question.Options is null, assign an empty list to it
                    foreach (var option in question.Options ??= new List<QuestionOption>())
                        option.QuestionId = question.Id;
                    Database.Upsert(question);
                }

            Database.Upsert(booking);
            if (usersSeen.Add(booking.UserId))
                try
                {
                    var user = await LibCalClient.GetAppointmentUser(booking.UserId);
                    Database.Upsert(user);
                }
                catch (FlurlHttpException exception)
                {
                    var response = await exception.GetResponseStringAsync();
                    if (response == "No user/data found. Ensure user has MyScheduler enabled.")
                    {
                        // TODO: is it ok to just skip these?
                    }
                    else
                    {
                        throw;
                    }
                }
        }
    }

    public async Task UpdateSpaces()
    {
        var bookings = await LibCalClient.GetSpaceBookings();
        foreach (var booking in bookings) Database.Upsert(booking);
    }
}