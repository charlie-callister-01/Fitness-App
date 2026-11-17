namespace Core.Rules.Schedule;

/// <summary>One Session placed on one date in a week's Week schedule. Break days are marked, not removed.</summary>
public sealed record ScheduledSession(string SessionId, DateOnly Date, bool IsMoved, bool IsBreakDay, bool IsBenchmark);

/// <summary>A week laid out day by day: the shared shape for rearrangement, Benchmarks and Nudges.</summary>
public sealed record WeekLayout(DateOnly Monday, IReadOnlyList<ScheduledSession> Sessions, IReadOnlyList<DateOnly> BreakDays);

/// <summary>A Session whose planned day passed with no Workout made from it that week.</summary>
public sealed record MissedSession(string SessionId, string SessionName, DateOnly Date);
