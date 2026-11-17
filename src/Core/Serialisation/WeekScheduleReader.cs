using Core.Model;

namespace Core.Serialisation;

/// <summary>
/// Reads the optional <c>weekSchedules</c> collection of a version 1 document (structure only; Mondays, week membership, Session references
/// and duplicates are cross-record rules owned by <see cref="DocumentValidator"/>). A week whose key can't be read, or a move whose date can't
/// be read, is dropped after being reported, so the validator never judges a placeholder.
/// </summary>
internal static class WeekScheduleReader
{
    public static List<WeekSchedule>? Read(FieldReader root) => root.ReadRecords("weekSchedules", ReadWeek);

    private static WeekSchedule? ReadWeek(FieldReader reader)
    {
        var weekStart = reader.RequiredDateOrNull("weekStart");
        var moves = reader.ReadRecords("moves", ReadMove) ?? [];
        return weekStart is { } start ? new WeekSchedule(start, moves) : null;
    }

    private static SessionMove? ReadMove(FieldReader reader)
    {
        var sessionId = reader.RequiredId("sessionId");
        var date = reader.RequiredDateOrNull("date");
        return date is { } day ? new SessionMove(sessionId, day) : null;
    }
}
