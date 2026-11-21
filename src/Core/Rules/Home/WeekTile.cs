using Core.Model;

namespace Core.Rules.Home;

public enum TileState
{
    Done,
    Partial,
    Missed,
    BreakDay,
    Upcoming,
    Rest,
}

public sealed record WeekTile(
    DateOnly Date,
    string Weekday,
    bool IsToday,
    Session? Session,
    string? Code,
    SessionType? SessionType,
    TileState State)
{
    public TileState TileState => State;
    public DayOfWeek DayOfWeek => Date.DayOfWeek;
}

public sealed record WeekLine(
    int Done,
    int Scheduled,
    int Target,
    IReadOnlyList<string> MissedCodes);
