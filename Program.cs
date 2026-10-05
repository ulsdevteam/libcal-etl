#nullable enable annotations

using System.Security.Cryptography;
using System.Text;
using CommandLine;
using dotenv.net;
using Flurl.Http;
using LibCalTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;

// How many rows to stage/PUT/MERGE per Snowflake round trip. Bigger batches mean fewer round trips
// (faster), but also mean a single bad row fails a bigger batch and produces a wider re-run window.
const int SnowflakeBatchSize = 500;

DotEnv.Load();
var config = new ConfigurationBuilder().AddEnvironmentVariables().Build();
var parser = new Parser(settings =>
{
    settings.AutoHelp = true;
    settings.CaseInsensitiveEnumValues = true;
    settings.HelpWriter = Console.Error;
});
await parser.ParseArguments<UpdateOptions, PrintSchemaOptions>(args)
    .MapResult<UpdateOptions, PrintSchemaOptions, Task>(RunUpdate, PrintSchema, NoOpOnError);

async Task RunUpdate(UpdateOptions updateOptions)
{
    try
    {
        var libCalClient = new LibCalClient();
        await libCalClient.Authorize(config["LIBCAL_CLIENT_ID"], config["LIBCAL_CLIENT_SECRET"]);
        await using var db = new Database(config);
        // Keep one Snowflake session open for the whole run: the bulk path relies on TEMPORARY staging
        // tables surviving across calls, and it avoids re-paying connection/warehouse-resume cost per batch.
        await db.Database.OpenConnectionAsync();

        bool Updating(DataSources source) => updateOptions.Sources.HasFlag(source);

        if (Updating(DataSources.Events))
        {
            var calendarIds = await libCalClient.GetCalendarIds();
            foreach (var calendarId in calendarIds)
            {
                var events = await libCalClient.GetEvents(calendarId, updateOptions.FromDate, updateOptions.ToDate);
                if (!events.Any()) { continue; }

                // GetEvents splits the requested range into 30-day windows (see GetInDateInterval); if an
                // event's own date range straddles a window boundary, LibCal can return that same event
                // from two different windowed requests. Dedupe here rather than downstream, since a
                // duplicate id would also collide in the Events MERGE later, not just in registrations.
                events = events.DistinctBy(e => e.Id).ToList();
                // Should the number of ids being sent per call be limited? Haven't hit the API max yet
                var registrations =
                    (await libCalClient.GetRegistrations(events.Select(e => e.Id)))
                    .ToDictionary(r => r.EventId, r => r.Registrants);

                await SaveEventBatchAsync(db, calendarId, events, registrations);
            }
        }

        if (Updating(DataSources.Appointments))
        {
            var bookings = await libCalClient.GetAppointmentBookings(updateOptions.FromDate, updateOptions.ToDate);
            // HashSet.Add returns true only if the element was not already in the set,
            // so these are used to filter out ids we already saw on this run
            var questionsSeen = new HashSet<long>();
            var usersSeen = new HashSet<long>();

            foreach (var batch in bookings.Chunk(SnowflakeBatchSize))
            {
                await SaveAppointmentBatchAsync(db, libCalClient, batch, questionsSeen, usersSeen);
            }
        }

        if (Updating(DataSources.Spaces))
        {
            var bookings = await libCalClient.GetSpaceBookings(updateOptions.FromDate, updateOptions.ToDate, updateOptions.LimitLocations);

            foreach (var batch in bookings.Chunk(SnowflakeBatchSize))
            {
                try
                {
                    foreach (var booking in batch) { booking.UserHash = Hash(booking.Account); }
                    await SnowflakeBulkLoader.BulkUpsertAsync(db.Database, "LIBCAL_SPACE_BOOKINGS",
                        SnowflakeBulkLoader.PrimaryKeyColumns(db.Model, typeof(SpaceBooking)),
                        SnowflakeBulkLoader.MapColumns<SpaceBooking>(db.Model), batch);
                }
                catch (Exception ex)
                {
                    var minDate = batch.Min(b => b.FromDate);
                    var maxDate = batch.Max(b => b.FromDate);
                    Console.Error.WriteLine(
                        $"[Space booking batch] {batch.Length} bookings, from-date range {minDate:O} to {maxDate:O} - failed to save: {ex.Message}");
                }
            }
        }
    }
    catch (FlurlHttpException exception)
    {
        Console.Error.WriteLine(await exception.GetResponseStringAsync());
        throw;
    }
}

// Stages one calendar's worth of Events (plus their owned Categories/Registrants/FutureDates) and
// upserts/replaces them in a handful of Snowflake round trips instead of one EF SaveChanges per event.
async Task SaveEventBatchAsync(Database db, int calendarId, List<Event> events, Dictionary<long, List<Registrant>> registrations)
{
    try
    {
        foreach (var @event in events)
        {
            // Default to an empty list rather than throwing if the registrations call didn't return an
            // entry for this event (e.g. an event with no registrants may not appear in the response at all) -
            // more important than ever now that one missing id would otherwise fail the whole calendar's batch
            @event.Registrants = registrations.GetValueOrDefault(@event.Id) ?? [];
            @event.Category ??= [];
            @event.FutureDates ??= [];
            foreach (var registrant in @event.Registrants) { registrant.UserHash = Hash(registrant.Email); }
            foreach (var category in @event.Category) { category.EventId = @event.Id; }
        }

        await SnowflakeBulkLoader.BulkUpsertAsync(db.Database, "LIBCAL_EVENTS", SnowflakeBulkLoader.PrimaryKeyColumns(db.Model, typeof(Event)),
            SnowflakeBulkLoader.MapColumns<Event>(db.Model), events);
        await SnowflakeBulkLoader.BulkReplaceChildrenAsync(db.Database, "LIBCAL_CATEGORIES",
            SnowflakeBulkLoader.OwnedCollectionForeignKeyColumn(db.Model, typeof(Event), nameof(Event.Category)),
            SnowflakeBulkLoader.MapColumns<Category>(db.Model), events.SelectMany(e => e.Category).ToList());
        await SnowflakeBulkLoader.BulkReplaceChildrenAsync(db.Database, "LIBCAL_EVENT_REGISTRANTS",
            SnowflakeBulkLoader.OwnedCollectionForeignKeyColumn(db.Model, typeof(Event), nameof(Event.Registrants)),
            RegistrantColumns(db.Model), events.SelectMany(e => e.Registrants.Select(r => (EventId: e.Id, Registrant: r))).ToList());
        await SnowflakeBulkLoader.BulkReplaceChildrenAsync(db.Database, "LIBCAL_FUTURE_DATES",
            SnowflakeBulkLoader.OwnedCollectionForeignKeyColumn(db.Model, typeof(Event), nameof(Event.FutureDates)),
            FutureDateColumns(db.Model), events.SelectMany(e => e.FutureDates.Select(fd => (OriginalEventId: e.Id, FutureDate: fd))).ToList());
    }
    catch (Exception ex)
    {
        var minDate = events.Min(e => e.Start);
        var maxDate = events.Max(e => e.Start);
        Console.Error.WriteLine(
            $"[Calendar {calendarId}] {events.Count} events, start range {minDate:O} to {maxDate:O} - failed to save batch: {ex.Message}");
    }
}

// Stages one batch of Appointment Bookings (plus the Questions/Options/Users they reference) and
// upserts/replaces them in a handful of Snowflake round trips instead of one EF SaveChanges per booking.
async Task SaveAppointmentBatchAsync(Database db, LibCalClient libCalClient, AppointmentBooking[] batch,
    HashSet<long> questionsSeen, HashSet<long> usersSeen)
{
    // Ids newly claimed by this batch, tracked so they can be released for retry by a later batch if this one fails
    var newQuestionIds = new List<long>();
    var newUserIds = new List<long>();
    try
    {
        foreach (var booking in batch)
        {
            booking.UserHash = Hash(booking.Email);
            foreach (var answer in booking.Answers ?? [])
            {
                answer.BookingId = booking.Id;
                if (questionsSeen.Add(answer.QuestionId)) { newQuestionIds.Add(answer.QuestionId); }
            }

            if (usersSeen.Add(booking.UserId)) { newUserIds.Add(booking.UserId); }
        }

        var questions = new List<AppointmentQuestion>();
        if (newQuestionIds.Any())
        {
            questions = await libCalClient.GetAppointmentQuestions(newQuestionIds);
            foreach (var question in questions)
            {
                // If question.Options is null, assign an empty list to it
                foreach (var option in question.Options ??= new List<QuestionOption>())
                {
                    option.QuestionId = question.Id;
                }
            }
        }

        var users = new List<AppointmentUser>();
        foreach (var userId in newUserIds)
        {
            try
            {
                var user = await libCalClient.GetAppointmentUser(userId);
                users.Add(user);
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

        // Questions/Users/Options are upserted/replaced before Bookings/Answers so a MERGE never needs to
        // see a QuestionId/UserId that isn't there yet (though Snowflake doesn't enforce FKs at write time
        // regardless - this ordering is just to keep the data consistent, not to satisfy a constraint).
        await SnowflakeBulkLoader.BulkUpsertAsync(db.Database, "LIBCAL_APPOINTMENT_QUESTIONS",
            SnowflakeBulkLoader.PrimaryKeyColumns(db.Model, typeof(AppointmentQuestion)),
            SnowflakeBulkLoader.MapColumns<AppointmentQuestion>(db.Model), questions);
        await SnowflakeBulkLoader.BulkReplaceChildrenAsync(db.Database, "LIBCAL_QUESTION_OPTIONS",
            SnowflakeBulkLoader.OwnedCollectionForeignKeyColumn(db.Model, typeof(AppointmentQuestion), nameof(AppointmentQuestion.Options)),
            SnowflakeBulkLoader.MapColumns<QuestionOption>(db.Model), questions.SelectMany(q => q.Options).ToList());
        await SnowflakeBulkLoader.BulkUpsertAsync(db.Database, "LIBCAL_APPOINTMENT_USERS",
            SnowflakeBulkLoader.PrimaryKeyColumns(db.Model, typeof(AppointmentUser)),
            SnowflakeBulkLoader.MapColumns<AppointmentUser>(db.Model), users);
        await SnowflakeBulkLoader.BulkUpsertAsync(db.Database, "LIBCAL_APPOINTMENT_BOOKINGS",
            SnowflakeBulkLoader.PrimaryKeyColumns(db.Model, typeof(AppointmentBooking)),
            SnowflakeBulkLoader.MapColumns<AppointmentBooking>(db.Model), batch);
        await SnowflakeBulkLoader.BulkReplaceChildrenAsync(db.Database, "LIBCAL_QUESTION_ANSWERS",
            SnowflakeBulkLoader.OwnedCollectionForeignKeyColumn(db.Model, typeof(AppointmentBooking), nameof(AppointmentBooking.Answers)),
            SnowflakeBulkLoader.MapColumns<QuestionAnswer>(db.Model), batch.SelectMany(b => b.Answers ?? []).ToList());
    }
    catch (Exception ex)
    {
        var minDate = batch.Min(b => b.FromDate);
        var maxDate = batch.Max(b => b.FromDate);
        Console.Error.WriteLine(
            $"[Appointment booking batch] {batch.Length} bookings, from-date range {minDate:O} to {maxDate:O} - failed to save: {ex.Message}");
        // Nothing in this batch actually persisted, so let a later batch retry any ids this one claimed
        foreach (var questionId in newQuestionIds) { questionsSeen.Remove(questionId); }
        foreach (var userId in newUserIds) { usersSeen.Remove(userId); }
    }
}

async Task PrintSchema(PrintSchemaOptions _)
{
    await using var db = new Database(config);
    Console.WriteLine(db.Database.GenerateCreateScript());
}

Task NoOpOnError(IEnumerable<Error> _) => Task.CompletedTask;

string Hash(string str) =>
    Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(str.ToLowerInvariant()))).ToLowerInvariant();

// The rest of the entity->column mappings are derived straight from the EF model via
// SnowflakeBulkLoader.MapColumns<T>() at each call site, instead of being hand-written here. Registrant
// and FutureDate are the two exceptions: their parent-Event id is a shadow property with no CLR member
// (see Database.cs's OwnsMany config), so MapColumns can't read it off the entity - it has to travel
// alongside the entity in a small wrapper tuple instead, built here by re-parenting the model-derived
// Registrant/FutureDate columns and appending the one shadow column (name still pulled from the model).

(string, Func<(long EventId, Registrant Registrant), object?>)[] RegistrantColumns(IModel model) =>
    SnowflakeBulkLoader.MapColumns<Registrant>(model)
        .Reparent<Registrant, (long EventId, Registrant Registrant)>(x => x.Registrant)
        .Append((SnowflakeBulkLoader.OwnedCollectionForeignKeyColumn(model, typeof(Event), nameof(Event.Registrants)),
            (Func<(long EventId, Registrant Registrant), object?>)(x => x.EventId)))
        .ToArray();

(string, Func<(long OriginalEventId, FutureDate FutureDate), object?>)[] FutureDateColumns(IModel model) =>
    SnowflakeBulkLoader.MapColumns<FutureDate>(model)
        .Reparent<FutureDate, (long OriginalEventId, FutureDate FutureDate)>(x => x.FutureDate)
        .Append((SnowflakeBulkLoader.OwnedCollectionForeignKeyColumn(model, typeof(Event), nameof(Event.FutureDates)),
            (Func<(long OriginalEventId, FutureDate FutureDate), object?>)(x => x.OriginalEventId)))
        .ToArray();