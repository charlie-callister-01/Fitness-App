namespace Core.Exchange;

/// <summary>An inclusive date range: a date on <see cref="From"/> or <see cref="To"/> is inside it.</summary>
public sealed record DateRange(DateOnly From, DateOnly To)
{
    public bool Contains(DateOnly date) => date >= From && date <= To;

    /// <summary>True when any day of [<paramref name="first"/>, <paramref name="last"/>] is inside the range.</summary>
    public bool Overlaps(DateOnly first, DateOnly last) => first <= To && last >= From;
}

public enum ExportFormat
{
    Json,
    Markdown,
}

/// <summary>
/// What to export: a date range (null means all time), the six kinds of data the owner can tick, and the format.
/// Plans are never date-filtered: the Plan tick means the active Plan, whole.
/// </summary>
public sealed record ExportRequest(
    DateRange? Range,
    bool Workouts,
    bool Plan,
    bool WeighIns,
    bool PainChecks,
    bool Guides,
    bool Exercises,
    ExportFormat Format)
{
    /// <summary>The number of days in the default Export (twelve weeks).</summary>
    public const int DefaultDays = 84;

    /// <summary>"Export everything": all time, every kind of data, JSON. The only request that is a Backup.</summary>
    public static ExportRequest Backup { get; } = new(null, true, true, true, true, true, true, ExportFormat.Json);

    /// <summary>The last 12 weeks ending today; Guides off, the other five kinds on; JSON.</summary>
    public static ExportRequest Default(DateOnly today) => PastDays(today, DefaultDays);

    /// <summary>The last <paramref name="days"/> days ending today (the Weekly review's "past week" is 7); same ticks as <see cref="Default"/>.</summary>
    public static ExportRequest PastDays(DateOnly today, int days) =>
        new(new DateRange(today.AddDays(1 - days), today), true, true, true, true, false, true, ExportFormat.Json);

    /// <summary>
    /// True when this request is a Backup: all time, every kind ticked, JSON. Only a Backup holds the Settings, Breaks, Badges, every Plan,
    /// the Week schedules and the personal notes, and only a Backup counts for the Backup Nudge.
    /// </summary>
    public bool IsBackup =>
        Range is null && Workouts && Plan && WeighIns && PainChecks && Guides && Exercises && Format == ExportFormat.Json;
}
