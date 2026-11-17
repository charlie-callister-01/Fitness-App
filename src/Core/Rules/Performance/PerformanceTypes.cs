using Core.Model;

namespace Core.Rules.Performance;

/// <summary>An Exercise's best Set and where it came from.</summary>
public sealed record BestResult(WorkoutSet Set, string WorkoutId, DateOnly Date, bool IsMax);

/// <summary>A Workout whose best Set for an Exercise strictly beat everything before it.</summary>
public sealed record PersonalRecord(string WorkoutId, DateOnly Date, WorkoutSet Set);

/// <summary>What was done the last time an Exercise was actually done (done or swapped, never skipped).</summary>
public sealed record LastResult(string WorkoutId, DateOnly Date, IReadOnlyList<WorkoutSet> Sets, int? Rpe, WorkoutSet? BestSet);

/// <summary>One chart point: the day's best Set and its value in the Exercise's measure (for example estimated 1RM, reps, seconds, km).</summary>
public sealed record ProgressPoint(DateOnly Date, double Value, WorkoutSet Set);

/// <summary>A body measurement's first and latest values (kg or cm) and the change between them.</summary>
public sealed record MeasurementChange(double Start, double Latest, double Change);
