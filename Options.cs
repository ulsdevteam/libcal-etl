﻿using CommandLine;

[Verb("update")]
class UpdateOptions
{
    DateTime? _fromDate;
    DateTime? _toDate;

    [Option('f', "from", Required = false, HelpText = "From date, defaults to the first day of the previous July")]
    public DateTime FromDate
    {
        get => _fromDate ?? StartOfFiscalYear;
        set => _fromDate = value;
    }

    [Option('t', "to", Required = false, HelpText = "To date, defaults to today")]
    public DateTime ToDate
    {
        get => _toDate ?? DateTime.Today;
        set => _toDate = value;
    }
    

    [Value(0, Required = true, MetaName = "sources", HelpText =
            "Which data sources to update, separated by commas. " +
            "Accepted values are: events, appointments, spaces, all."
    )]
    public DataSources Sources { get; set; }
    
    static DateTime StartOfFiscalYear => new DateTime(DateTime.Today.Month > 7 ? DateTime.Today.Year : DateTime.Today.Year - 1, 7, 1);
}

[Flags]
enum DataSources
{
    Events = 1,
    Appointments = 2,
    Spaces = 4,
    All = 7
}

[Verb("batch")]
class BatchOptions
{
    [Value(0, MetaName = "files")]
    public IEnumerable<string> Files { get; set; }
}

[Verb("print-schema")]
class PrintSchemaOptions { }