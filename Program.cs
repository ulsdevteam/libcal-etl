using CommandLine;
using dotenv.net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

DotEnv.Load();
var config = new ConfigurationBuilder().AddEnvironmentVariables().Build();
var parser = new Parser(options => { options.CaseInsensitiveEnumValues = true; });
await parser.ParseArguments<UpdateOptions>(args).WithParsedAsync(async updateOptions =>
{
    var libCalClient = new LibCalClient();
    await libCalClient.Authorize(config["LIBCAL_CLIENT_ID"], config["LIBCAL_CLIENT_SECRET"]);
    var dbOptions = new DbContextOptionsBuilder()
        .UseSqlite("Filename=test.db")
        .Options;
    await using var db = new Database(dbOptions);
    db.Database.EnsureCreated();
    
    var updater = new Updater(db, libCalClient);
    var fromDate = updateOptions.FromDate ?? DateTime.Today.AddMonths(-1);
    var toDate = updateOptions.ToDate ?? DateTime.Today;

    if (updateOptions.UpdateChoices.HasFlag(UpdateChoices.Events))
    {
        await updater.UpdateEvents(fromDate, toDate);
    }

    if (updateOptions.UpdateChoices.HasFlag(UpdateChoices.Appointments))
    {
        await updater.UpdateAppointments(fromDate, toDate);
    }

    if (updateOptions.UpdateChoices.HasFlag(UpdateChoices.Spaces))
    {
        await updater.UpdateSpaces();
    }

    await db.SaveChangesAsync();
});