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
await parser.ParseArguments<UpdateOptions, BatchOptions, PrintSchemaOptions>(args)
    .MapResult<UpdateOptions, BatchOptions, PrintSchemaOptions, Task>(RunUpdate, RunBatch, PrintSchema, NoOpOnError);

async Task RunUpdate(UpdateOptions updateOptions)
{
    try
    {
        var libCalClient = new LibCalClient();
        await libCalClient.Authorize(config["LIBCAL_CLIENT_ID"], config["LIBCAL_CLIENT_SECRET"]);
        await using var db = new Database(config);
        // Keep one Snowflake session open for the whole run: the bulk path relies on TEMPORARY staging
        // tables surviving across calls, and it avoids re-paying connection/warehouse-resume cost per batch.
        if (db.IsSnowflake) { await db.Database.OpenConnectionAsync(); }

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

                if (db.IsSnowflake)
                {
                    await SaveEventBatchAsync(db, calendarId, events, registrations);
                }
                else
                {
                    // @ sign because event is a reserved keyword
                    foreach (var @event in events)
                    {
                        try
                        {
                            // This line would throw if the above call didn't return an entry for one of the ids;
                            // that's now caught below and reported per-event instead of aborting the whole run
                            @event.Registrants = registrations[@event.Id];
                            foreach (var registrant in @event.Registrants) { registrant.UserHash = Hash(registrant.Email); }
                            foreach (var category in @event.Category) { category.EventId = @event.Id; }
                            // just truncate strings longer than 2000 for oracle
                            @event.Description = Truncate(@event.Description, 2000);
                            @event.MoreInfo = Truncate(@event.MoreInfo, 2000);
                            db.Upsert(@event);
                            await db.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine(
                                $"[Event {@event.Id}] start={@event.Start:O} end={@event.End:O} - failed to save: {ex.Message}");
                        }
                        finally
                        {
                            // Detach everything staged for this event so a failure (or the save we just made)
                            // doesn't affect change tracking for the next event
                            db.ChangeTracker.Clear();
                        }
                    }
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

            if (db.IsSnowflake)
            {
                foreach (var batch in bookings.Chunk(SnowflakeBatchSize))
                {
                    await SaveAppointmentBatchAsync(db, libCalClient, batch, questionsSeen, usersSeen);
                }
            }
            else
            {
                foreach (var booking in bookings)
                {
                    // Track ids newly claimed by this booking so they can be released for retry if the save below fails
                    var newQuestionIds = new List<long>();
                    var claimedNewUser = false;
                    try
                    {
                        booking.UserHash = Hash(booking.Email);
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
                            claimedNewUser = true;
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

                        await db.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(
                            $"[Appointment booking {booking.Id}] from={booking.FromDate:O} to={booking.ToDate:O} - failed to save: {ex.Message}");
                        // This booking never actually persisted, so let a later booking retry any ids it claimed
                        foreach (var questionId in newQuestionIds) { questionsSeen.Remove(questionId); }
                        if (claimedNewUser) { usersSeen.Remove(booking.UserId); }
                    }
                    finally
                    {
                        db.ChangeTracker.Clear();
                    }
                }
            }
        }

        if (Updating(DataSources.Spaces))
        {
            var bookings = await libCalClient.GetSpaceBookings(updateOptions.FromDate, updateOptions.ToDate, updateOptions.LimitLocations);

            if (db.IsSnowflake)
            {
                foreach (var batch in bookings.Chunk(SnowflakeBatchSize))
                {
                    try
                    {
                        foreach (var booking in batch) { booking.UserHash = Hash(booking.Account); }
                        await SnowflakeBulkLoader.BulkUpsertAsync(db.Database, "LIBCAL_SPACE_BOOKINGS", ["ID"],
                            SpaceBookingColumns(), batch);
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
            else
            {
                foreach (var booking in bookings)
                {
                    try
                    {
                        booking.UserHash = Hash(booking.Account);
                        db.Upsert2(booking);
                        await db.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(
                            $"[Space booking {booking.Id}] from={booking.FromDate:O} to={booking.ToDate:O} - failed to save: {ex.Message}");
                    }
                    finally
                    {
                        db.ChangeTracker.Clear();
                    }
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
            // just truncate strings longer than 2000 for oracle
            @event.Description = Truncate(@event.Description, 2000);
            @event.MoreInfo = Truncate(@event.MoreInfo, 2000);
        }

        await SnowflakeBulkLoader.BulkUpsertAsync(db.Database, "LIBCAL_EVENTS", ["ID"], EventColumns(), events);
        await SnowflakeBulkLoader.BulkReplaceChildrenAsync(db.Database, "LIBCAL_CATEGORIES", "EVENT_ID",
            CategoryColumns(), events.SelectMany(e => e.Category).ToList());
        await SnowflakeBulkLoader.BulkReplaceChildrenAsync(db.Database, "LIBCAL_EVENT_REGISTRANTS", "EVENT_ID",
            RegistrantColumns(), events.SelectMany(e => e.Registrants.Select(r => (EventId: e.Id, Registrant: r))).ToList());
        await SnowflakeBulkLoader.BulkReplaceChildrenAsync(db.Database, "LIBCAL_FUTURE_DATES", "ORIGINAL_EVENT_ID",
            FutureDateColumns(), events.SelectMany(e => e.FutureDates.Select(fd => (OriginalEventId: e.Id, FutureDate: fd))).ToList());
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
                answer.Answer = Truncate(answer.Answer, 2000);
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
                user.Description = Truncate(user.Description, 2000);
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
        await SnowflakeBulkLoader.BulkUpsertAsync(db.Database, "LIBCAL_APPOINTMENT_QUESTIONS", ["ID"],
            AppointmentQuestionColumns(), questions);
        await SnowflakeBulkLoader.BulkReplaceChildrenAsync(db.Database, "LIBCAL_QUESTION_OPTIONS", "QUESTION_ID",
            QuestionOptionColumns(), questions.SelectMany(q => q.Options).ToList());
        await SnowflakeBulkLoader.BulkUpsertAsync(db.Database, "LIBCAL_APPOINTMENT_USERS", ["USER_ID"],
            AppointmentUserColumns(), users);
        await SnowflakeBulkLoader.BulkUpsertAsync(db.Database, "LIBCAL_APPOINTMENT_BOOKINGS", ["ID"],
            AppointmentBookingColumns(), batch);
        await SnowflakeBulkLoader.BulkReplaceChildrenAsync(db.Database, "LIBCAL_QUESTION_ANSWERS", "BOOKING_ID",
            QuestionAnswerColumns(), batch.SelectMany(b => b.Answers ?? []).ToList());
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

async Task RunBatch(BatchOptions batchOptions)
{
    await using var db = new Database(config);
    var rows = new List<ArchivedSpaceBooking>();

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
                    rows.Add(new ArchivedSpaceBooking
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
                rows.Add(new ArchivedSpaceBooking
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

    if (db.IsSnowflake)
    {
        // ID is Snowflake's auto-incrementing identity column here, so it's intentionally left out of the
        // column list/COPY INTO - Snowflake assigns it. This is a plain append (no MERGE): batch imports
        // were never upserted by the old code either, just always inserted.
        await SnowflakeBulkLoader.BulkInsertAsync(db.Database, "LIBCAL_ARCHIVED_SPACE_BOOKINGS", ArchivedSpaceBookingColumns(), rows);
    }
    else
    {
        foreach (var row in rows) { db.Add(row); }
        await db.SaveChangesAsync();
    }
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

// Column projections used by the Snowflake bulk-load path. Column names/order must match the schema in
// Migrations/*_SnowflakeInitial.cs exactly, since these bypass EF's own model mapping entirely.

(string, Func<Event, object?>)[] EventColumns() =>
[
    ("ID", e => e.Id),
    ("TITLE", e => e.Title),
    ("ALL_DAY", e => e.AllDay),
    ("START", e => e.Start),
    ("END", e => e.End),
    ("DESCRIPTION", e => e.Description),
    ("URL_PUBLIC", e => e.Url?.Public?.ToString()),
    ("URL_ADMIN", e => e.Url?.Admin?.ToString()),
    ("LOCATION_ID", e => e.Location?.Id),
    ("LOCATION_TYPE", e => e.Location?.Type),
    ("LOCATION_NAME", e => e.Location?.Name),
    ("CAMPUS_ID", e => e.Campus?.Id),
    ("CAMPUS_NAME", e => e.Campus?.Name),
    ("OWNER_ID", e => e.Owner?.Id),
    ("OWNER_NAME", e => e.Owner?.Name),
    ("PRESENTER", e => e.Presenter),
    ("CALENDAR_ID", e => e.Calendar?.Id),
    ("CALENDAR_NAME", e => e.Calendar?.Name),
    ("CALENDAR_PUBLIC", e => e.Calendar?.Public?.ToString()),
    ("CALENDAR_ADMIN", e => e.Calendar?.Admin?.ToString()),
    ("SEATS", e => e.Seats),
    ("REGISTRATION", e => e.Registration),
    ("HAS_REGISTRATION_OPENED", e => e.HasRegistrationOpened),
    ("HAS_REGISTRATION_CLOSED", e => e.HasRegistrationClosed),
    ("PHYSICAL_SEATS", e => e.PhysicalSeats),
    ("PHYSICAL_SEATS_TAKEN", e => e.PhysicalSeatsTaken),
    ("ONLINE_SEATS", e => e.OnlineSeats),
    ("ONLINE_SEATS_TAKEN", e => e.OnlineSeatsTaken),
    ("SEATS_TAKEN", e => e.SeatsTaken),
    ("WAIT_LIST", e => e.WaitList),
    ("COLOR", e => e.Color),
    ("FEATURED_IMAGE", e => e.FeaturedImage?.ToString()),
    ("MORE_INFO", e => e.MoreInfo),
    ("SETUP_TIME", e => e.SetupTime),
    ("TEARDOWN_TIME", e => e.TeardownTime),
    ("ONLINE_USER_ID", e => e.OnlineUserId),
    ("ZOOM_EMAIL", e => e.ZoomEmail),
    ("ONLINE_MEETING_ID", e => e.OnlineMeetingId),
    ("ONLINE_HOST_URL", e => e.OnlineHostUrl?.ToString()),
    ("ONLINE_JOIN_URL", e => e.OnlineJoinUrl?.ToString()),
    ("ONLINE_JOIN_PASSWORD", e => e.OnlineJoinPassword),
    ("ONLINE_PROVIDER", e => e.OnlineProvider),
];

(string, Func<Category, object?>)[] CategoryColumns() =>
[
    ("ID", c => c.Id),
    ("EVENT_ID", c => c.EventId),
    ("NAME", c => c.Name),
];

// Registrant's EVENT_ID foreign key is a pure EF shadow property (no matching C# member), so the parent
// Event's id has to travel alongside it explicitly rather than being read off the Registrant itself.
(string, Func<(long EventId, Registrant Registrant), object?>)[] RegistrantColumns() =>
[
    ("BOOKING_ID", x => x.Registrant.BookingId),
    ("USER_HASH", x => x.Registrant.UserHash),
    ("REGISTRATION_TYPE", x => x.Registrant.RegistrationType),
    ("BARCODE", x => x.Registrant.Barcode),
    ("REGISTERED_DATE", x => x.Registrant.RegisteredDate),
    ("ATTENDANCE", x => x.Registrant.Attendance),
    ("EVENT_ID", x => x.EventId),
];

// Same situation as Registrant above: ORIGINAL_EVENT_ID is a shadow property, so it's carried alongside
// the FutureDate rather than read off it.
(string, Func<(long OriginalEventId, FutureDate FutureDate), object?>)[] FutureDateColumns() =>
[
    ("FUTURE_EVENT_ID", x => x.FutureDate.FutureEventId),
    ("ORIGINAL_EVENT_ID", x => x.OriginalEventId),
    ("START", x => x.FutureDate.Start),
];

(string, Func<AppointmentBooking, object?>)[] AppointmentBookingColumns() =>
[
    ("ID", b => b.Id),
    ("USER_HASH", b => b.UserHash),
    ("FROM_DATE", b => b.FromDate),
    ("TO_DATE", b => b.ToDate),
    ("USER_ID", b => b.UserId),
    ("LOCATION", b => b.Location),
    ("LOCATION_ID", b => b.LocationId),
    ("GROUP", b => b.Group),
    ("GROUP_ID", b => b.GroupId),
    ("CATEGORY_ID", b => b.CategoryId),
    ("DIRECTIONS", b => b.Directions),
    ("CANCELLED", b => b.Cancelled),
];

(string, Func<QuestionAnswer, object?>)[] QuestionAnswerColumns() =>
[
    ("BOOKING_ID", a => a.BookingId),
    ("QUESTION_ID", a => a.QuestionId),
    ("ANSWER", a => a.Answer),
];

(string, Func<AppointmentQuestion, object?>)[] AppointmentQuestionColumns() =>
[
    ("ID", q => q.Id),
    ("LABEL", q => q.Label),
    ("TYPE", q => q.Type),
    ("REQUIRED", q => q.Required),
];

(string, Func<QuestionOption, object?>)[] QuestionOptionColumns() =>
[
    ("QUESTION_ID", o => o.QuestionId),
    ("OPTION", o => o.Option),
];

(string, Func<AppointmentUser, object?>)[] AppointmentUserColumns() =>
[
    ("USER_ID", u => u.UserId),
    ("FIRST_NAME", u => u.FirstName),
    ("LAST_NAME", u => u.LastName),
    ("NICKNAME", u => u.Nickname),
    ("EMAIL", u => u.Email),
    ("URL", u => u.Url?.ToString()),
    ("DESCRIPTION", u => u.Description),
];

(string, Func<SpaceBooking, object?>)[] SpaceBookingColumns() =>
[
    ("ID", b => b.Id),
    ("USER_HASH", b => b.UserHash),
    ("BOOK_ID", b => b.BookId),
    ("ITEM_ID", b => b.ItemId),
    ("CATEGORY_ID", b => b.CategoryId),
    ("LOCATION_ID", b => b.LocationId),
    ("FROM_DATE", b => b.FromDate),
    ("TO_DATE", b => b.ToDate),
    ("CREATED", b => b.Created),
    ("STATUS", b => b.Status),
    ("LOCATION_NAME", b => b.LocationName),
    ("CATEGORY_NAME", b => b.CategoryName),
    ("ITEM_NAME", b => b.ItemName),
    ("CANCELLED", b => b.Cancelled),
];

// ID is excluded: it's an auto-incrementing identity column in Snowflake, assigned on insert.
(string, Func<ArchivedSpaceBooking, object?>)[] ArchivedSpaceBookingColumns() =>
[
    ("BOOKING_ID", x => x.BookingId),
    ("SPACE_ID", x => x.SpaceId),
    ("SPACE_NAME", x => x.SpaceName),
    ("LOCATION", x => x.Location),
    ("ZONE", x => x.Zone),
    ("CATEGORY", x => x.Category),
    ("USER_HASH", x => x.UserHash),
    ("FROM_DATE", x => x.FromDate),
    ("TO_DATE", x => x.ToDate),
    ("CREATED_DATE", x => x.CreatedDate),
    ("EVENT_ID", x => x.EventId),
    ("EVENT_TITLE", x => x.EventTitle),
    ("EVENT_START", x => x.EventStart),
    ("EVENT_END", x => x.EventEnd),
    ("STATUS", x => x.Status),
    ("CANCELLED_BY_USER", x => x.CancelledByUser),
    ("CANCELLED_AT", x => x.CancelledAt),
    ("SHOWED_UP", x => x.ShowedUp),
    ("CHECKED_IN_DATE", x => x.CheckedInDate),
    ("CHECKED_OUT_DATE", x => x.CheckedOutDate),
    ("COST", x => x.Cost),
    ("BOOKING_FORM_ANSWERS", x => x.BookingFormAnswers),
];