using Core.Model;

namespace Core.Rules.Editing;

/// <summary>Ranks library Exercises by how many tags they share with a given Exercise (the picker's "Similar" order).</summary>
public static class SimilarExercises
{
    /// <summary>
    /// The library Exercises not already in <paramref name="workout"/>, most shared tags (muscle group, equipment, movement pattern,
    /// compared case-insensitively within each category) first, ties by name. Untagged Exercises come last; when
    /// <paramref name="current"/> has no tags the order is plain alphabetical.
    /// </summary>
    public static IReadOnlyList<Exercise> Rank(Exercise current, IEnumerable<Exercise> library, Workout workout)
    {
        var taken = workout.Planned.Select(p => p.ExerciseId)
            .Concat(workout.Entries.Select(e => e.ExerciseId))
            .Append(current.Id)
            .ToHashSet();

        return library
            .Where(e => !taken.Contains(e.Id))
            .OrderBy(e => current.Tags is not null && e.Tags is null ? 1 : 0)
            .ThenByDescending(e => Shared(current.Tags, e.Tags))
            .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>How many tags two Exercises share; 0 when either has none. Above 0 means "similar" in the picker.</summary>
    public static int SharedTagCount(Exercise a, Exercise b) => Shared(a.Tags, b.Tags);

    private static int Shared(ExerciseTags? a, ExerciseTags? b) =>
        a is null || b is null
            ? 0
            : Overlap(a.MuscleGroups, b.MuscleGroups) + Overlap(a.Equipment, b.Equipment) + Overlap(a.MovementPatterns, b.MovementPatterns);

    private static int Overlap(IReadOnlyList<string> a, IReadOnlyList<string> b) =>
        a.Intersect(b, StringComparer.OrdinalIgnoreCase).Count();
}
