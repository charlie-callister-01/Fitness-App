using Core.Model;
using Core.Storage;

namespace Core.Rules.PlanEditing;

/// <summary>What one press of Save in the Plan editor commits.</summary>
public static class PlanSaveRules
{
    /// <summary>
    /// The changes for one commit: the composed Plan (left out when it is no different from <paramref name="saved"/>) and a put for each
    /// Exercise in <paramref name="library"/> whose benchmark flag actually changes under <paramref name="benchmarkOverrides"/> (Exercise id
    /// to the flag wanted). Overrides that change nothing, and ids not in the library, are ignored.
    /// </summary>
    public static IReadOnlyList<Change> Changes(
        Plan composed, Plan saved, IReadOnlyList<Exercise> library, IReadOnlyDictionary<string, bool> benchmarkOverrides)
    {
        var changes = new List<Change>();
        if (IsDifferent(composed, saved))
        {
            changes.Add(Change.Put(composed));
        }

        foreach (var exercise in library)
        {
            if (benchmarkOverrides.TryGetValue(exercise.Id, out var wanted) && wanted != exercise.IsBenchmark)
            {
                changes.Add(Change.Put(exercise with { IsBenchmark = wanted }));
            }
        }

        return changes;
    }

    /// <summary>Whether <paramref name="composed"/> differs from <paramref name="saved"/> (comparing Tracks, each Track's Phases, and Sessions by sequence).</summary>
    public static bool IsDifferent(Plan composed, Plan saved) =>
        composed with { Tracks = saved.Tracks, Sessions = saved.Sessions } != saved
        || TracksDiffer(composed.Tracks, saved.Tracks)
        || !composed.Sessions.SequenceEqual(saved.Sessions);

    private static bool TracksDiffer(IReadOnlyList<Track> a, IReadOnlyList<Track> b) =>
        a.Count != b.Count
        || a.Zip(b).Any(pair => pair.First with { Phases = pair.Second.Phases } != pair.Second || !pair.First.Phases.SequenceEqual(pair.Second.Phases));
}
