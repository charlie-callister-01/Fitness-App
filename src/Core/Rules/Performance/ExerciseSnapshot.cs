namespace Core.Rules.Performance;

/// <summary>
/// Last, Best and Suggested target for one Exercise, returned together for the log screen.
/// <see cref="Target"/> is null until ticket 04 fills it.
/// </summary>
public sealed record ExerciseSnapshot(string ExerciseId, LastResult? Last, BestResult? Best, SuggestedTarget? Target);
