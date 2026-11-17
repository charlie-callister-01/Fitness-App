namespace Core.Model;

/// <summary>A one-week-only move of a Session to another day: the Session's ID and the date it is done on that week.</summary>
public sealed record SessionMove(string SessionId, DateOnly Date);

/// <summary>The Session moves for one week, keyed by that week's Monday. A Session with no move is on its default day.</summary>
public sealed record WeekSchedule(DateOnly WeekStart, IReadOnlyList<SessionMove> Moves);
