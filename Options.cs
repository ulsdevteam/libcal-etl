using CommandLine;

class UpdateOptions
{
    [Option('f', "from", Required = false, HelpText = "From date, defaults to one month before today")]
    public DateTime? FromDate { get; set; }

    [Option('t', "to", Required = false, HelpText = "To date, defaults to today")]
    public DateTime? ToDate { get; set; }

    [Option('s', Required = true, HelpText =
            "Which data sources to update, separated by commas. " +
            "Accepted values are: events, appointments, spaces, all."
    )]
    public DataSources Sources { get; set; }
}

[Flags]
enum DataSources
{
    Events = 1,
    Appointments = 2,
    Spaces = 4,
    All = 7
}