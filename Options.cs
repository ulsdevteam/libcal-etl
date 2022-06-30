using CommandLine;

class UpdateOptions
{
    [Option('f', "from", Required = false, HelpText = "From date, defaults to one month before today")]
    public DateTime? FromDate { get; set; }
    [Option('t', "to", Required = false, HelpText = "To date, defaults to today")]
    public DateTime? ToDate { get; set; }
    [Value(0)]
    public UpdateChoices UpdateChoices { get; set; }
}

[Flags]
enum UpdateChoices
{
    Events = 1,
    Appointments = 2,
    Spaces = 4,
    All = 7,
}