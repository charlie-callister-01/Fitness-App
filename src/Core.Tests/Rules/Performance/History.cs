using Core.Model;

namespace Core.Tests.Rules.Performance;

/// <summary>Tiny fluent builder for short dated histories used by the performance tests.</summary>
internal sealed class History
{
    private readonly List<Workout> _workouts = [];

    public IReadOnlyList<Workout> Workouts => _workouts;

    public static WorkoutSet Set(double? kg = null, int? reps = null, double? km = null, int? seconds = null, double? m = null, bool max = false) =>
        new($"s{Guid.NewGuid():N}", kg, reps, km, seconds, m, max);

    public History Done(string id, string date, string exerciseId, params WorkoutSet[] sets) =>
        Add(id, date, new WorkoutEntry(exerciseId, EntryStatus.Done, null, null, null, sets));

    public History DoneWithRpe(string id, string date, string exerciseId, int rpe, params WorkoutSet[] sets) =>
        Add(id, date, new WorkoutEntry(exerciseId, EntryStatus.Done, null, null, rpe, sets));

    public History Skipped(string id, string date, string exerciseId) =>
        Add(id, date, new WorkoutEntry(exerciseId, EntryStatus.Skipped, "time", null, null, []));

    /// <summary>A swapped entry: <paramref name="actualId"/> was done instead of <paramref name="plannedId"/>.</summary>
    public History Swapped(string id, string date, string plannedId, string actualId, params WorkoutSet[] sets) =>
        Add(id, date, new WorkoutEntry(actualId, EntryStatus.Swapped, null, plannedId, null, sets));

    private History Add(string id, string date, WorkoutEntry entry)
    {
        _workouts.Add(new Workout(id, DateOnly.Parse(date), null, false, null, null, [], [entry], null));
        return this;
    }
}
