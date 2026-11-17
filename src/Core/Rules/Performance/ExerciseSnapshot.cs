namespace Core.Rules.Performance;

/// <summary>
/// Last, Best and Suggested target for one Exercise, returned together for the log screen.
/// <see cref="Target"/> is null when there is no history, no Plan, or the Exercise's kind has no target.
/// </summary>
public sealed record ExerciseSnapshot(string ExerciseId, LastResult? Last, BestResult? Best, SuggestedTarget? Target);
