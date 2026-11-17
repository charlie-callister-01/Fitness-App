using Core.Model;

namespace Core.Tests.Rules.Outcomes;

internal sealed class WorkoutBuilder
{
    private readonly List<SessionExercise> _planned = [];
    private readonly List<WorkoutEntry> _entries = [];

    public WorkoutBuilder Plan(params string[] exerciseIds)
    {
        foreach (var id in exerciseIds) _planned.Add(new SessionExercise(id, 3, 10, null, null));
        return this;
    }

    public WorkoutBuilder Done(string exerciseId) => Add(new WorkoutEntry(exerciseId, EntryStatus.Done, null, null, null, []));

    public WorkoutBuilder Skipped(string exerciseId, string? reason) =>
        Add(new WorkoutEntry(exerciseId, EntryStatus.Skipped, reason, null, null, []));

    public WorkoutBuilder Swapped(string plannedId, string doneId) =>
        Add(new WorkoutEntry(doneId, EntryStatus.Swapped, null, plannedId, null, []));

    private WorkoutBuilder Add(WorkoutEntry entry)
    {
        _entries.Add(entry);
        return this;
    }

    public Workout Build() => new(
        "w1", new DateOnly(2026, 11, 2), _planned.Count == 0 ? null : "s1", false, null, null, _planned, _entries, null);
}
