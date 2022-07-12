using dotenv.net;
using LibCalTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Defines the SQL table schema and encapsulates the connection to the database.
/// </summary>
class Database : DbContext
{
    IConfiguration Config { get; }
    
    public Database(IConfiguration config)
    {
        Config = config;
    }

    /// <summary>
    /// Add an entity to the context, setting its state to Added if it does not already exist, or Modified if it does.
    /// </summary>
    /// <param name="item">The new or updated entity</param>
    /// <typeparam name="T">An entity type that is managed by this context</typeparam>
    public void Upsert<T>(T item) where T : class
    {
        var entry = Entry(item);
        if (entry.GetDatabaseValues() is null)
        {
            Add(item);
        }
        else
        {
            Update(item);
            // Update sets the state of all child objects to modified by default, but some may be newly added so we need to check
            // If we had deeper nesting, this function would probably need to be recursive, or another strategy could be used
            foreach (var collection in entry.Collections)
            {
                if (collection.CurrentValue is null) { continue; }

                foreach (var childItem in collection.CurrentValue)
                {
                    var childEntry = collection.FindEntry(childItem);
                    if (childEntry is not null && childEntry.GetDatabaseValues() is null)
                    {
                        childEntry.State = EntityState.Added;
                    }
                }
            }
        }
    }

    // Called by EF to configure the schema
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Event>(events =>
        {
            events.ToTable("LIBCAL_EVENTS");
            events.HasKey(e => e.Id);
            // The id comes from the api, we don't want EF to autogenerate it
            events.Property(e => e.Id).ValueGeneratedNever();

            // These tell it to store the columns of nested objects alongside
            events.OwnsOne(e => e.Url);
            events.OwnsOne(e => e.Location);
            events.OwnsOne(e => e.Campus);
            events.OwnsOne(e => e.Owner);
            events.OwnsOne(e => e.Calendar);

            // These will create new tables linked with a foreign key
            events.OwnsMany(e => e.Category, categories =>
            {
                categories.ToTable("LIBCAL_CATEGORIES");
                categories.WithOwner().HasForeignKey(c => c.EventId);
                categories.HasKey(c => new { c.EventId, c.Id });
                categories.Property(c => c.Id).ValueGeneratedNever();
            });
            events.OwnsMany(e => e.Registrants, registrants =>
            {
                registrants.ToTable("LIBCAL_EVENT_REGISTRANTS");
                registrants.WithOwner();
                registrants.HasKey(r => r.BookingId);
                registrants.Property(r => r.BookingId).ValueGeneratedNever();
            });
            events.OwnsMany(e => e.FutureDates, futureDates =>
            {
                futureDates.ToTable("LIBCAL_FUTURE_DATES");
                // This sets up OriginalEventId as a "shadow property" that exists in the db but not in the C# class
                const string originalEventId = "OriginalEventId";
                futureDates.Property<long>(originalEventId);
                futureDates.WithOwner().HasForeignKey(originalEventId);
                futureDates.HasKey(originalEventId, nameof(FutureDate.FutureEventId));
                futureDates.Property(fd => fd.FutureEventId).ValueGeneratedNever();
            });

            // Unused
            events.Ignore(e => e.Geolocation);
        });

        builder.Entity<AppointmentBooking>(bookings =>
        {
            bookings.ToTable("LIBCAL_APPOINTMENT_BOOKINGS");
            bookings.HasKey(b => b.Id);
            bookings.Property(b => b.Id).ValueGeneratedNever();

            bookings.OwnsMany(b => b.Answers, answers =>
            {
                answers.ToTable("LIBCAL_QUESTION_ANSWERS");
                answers.WithOwner().HasForeignKey(q => q.BookingId);
                answers.HasKey(q => new { q.BookingId, q.QuestionId });
                answers.HasOne<AppointmentQuestion>().WithMany().HasForeignKey(q => q.QuestionId);
            });
        });

        builder.Entity<AppointmentUser>(users =>
        {
            users.ToTable("LIBCAL_APPOINTMENT_USERS");
            users.HasKey(u => u.UserId);
            users.Property(u => u.UserId).ValueGeneratedNever();
        });

        builder.Entity<AppointmentQuestion>(questions =>
        {
            questions.ToTable("LIBCAL_APPOINTMENT_QUESTIONS");
            questions.Property(q => q.Id).ValueGeneratedNever();
            questions.OwnsMany(q => q.Options, options =>
            {
                options.ToTable("LIBCAL_QUESTION_OPTIONS");
                options.WithOwner().HasForeignKey(o => o.QuestionId);
                options.HasKey(o => new { o.QuestionId, o.Option });
            });
        });

        builder.Entity<SpaceBooking>(bookings =>
        {
            bookings.ToTable("LIBCAL_SPACE_BOOKINGS");
            bookings.Property(b => b.Id).ValueGeneratedNever();
        });

        builder.Entity<ArchivedSpaceBooking>(archivedBookings =>
        {
            archivedBookings.ToTable("LIBCAL_ARCHIVED_SPACE_BOOKINGS");
            // I would like to use .HasNoKey(), but in EF no-key tables are read-only without custom sql
            // So this configures it to use an auto-incrementing id
            archivedBookings.HasKey(a => a.Id);
            archivedBookings.Property(a => a.Id).ValueGeneratedOnAdd();
        });
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        var connectionString = Config["CONNECTION_STRING"];
        if (connectionString.StartsWith("Filename="))
        {
            options.UseSqlite(connectionString);
        }
        else
        {
            options.UseOracle(connectionString, oracleOptions =>
            {
                oracleOptions.MigrationsHistoryTable("LIBCAL_EF_MIGRATIONS");
            });
        }
        options.UseUpperSnakeCaseNamingConvention();
    }

    class MigrationFactory : IDesignTimeDbContextFactory<Database>
    {
        // Called to configure the connection when using the cli migration tools
        public Database CreateDbContext(string[] args)
        {
            DotEnv.Load();
            var config = new ConfigurationBuilder().AddEnvironmentVariables().Build();
            return new Database(config);
        }
    }
}