using System.Globalization;
using CommandLine;
using CsvHelper;
using dotenv.net;
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
await parser.ParseArguments<UpdateOptions, BatchOptions>(args).MapResult(async (UpdateOptions updateOptions) =>
{
    var libCalClient = new LibCalClient();
    await libCalClient.Authorize(config["LIBCAL_CLIENT_ID"], config["LIBCAL_CLIENT_SECRET"]);
    await using var db = new Database(dbOptions);
    var updater = new Updater(db, libCalClient);
    var fromDate = updateOptions.FromDate ?? DateTime.Today.AddMonths(-1);
    var toDate = updateOptions.ToDate ?? DateTime.Today;

    if (updateOptions.Sources.HasFlag(DataSources.Events)) { await updater.UpdateEvents(fromDate, toDate); }

    if (updateOptions.Sources.HasFlag(DataSources.Appointments)) { await updater.UpdateAppointments(fromDate, toDate); }

    if (updateOptions.Sources.HasFlag(DataSources.Spaces)) { await updater.UpdateSpaces(); }

    await db.SaveChangesAsync();
}, async (BatchOptions batchOptions) =>
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
        throw new NotImplementedException();
    }
    await db.SaveChangesAsync();
}, _ => Task.CompletedTask);