using Core.Model;
using Core.Rules.Editing;
using Core.Rules.Library;
using Core.Rules.LogKinds;
using Core.Rules.Outcomes;
using Core.Rules.Performance;
using Core.Storage;
using Ui.Components;

namespace Ui.Log;

/// <summary>
/// Where each place in the log stands (<see cref="LogSlots"/>): complete, skipped, swapped with Sets or an optional added Exercise,
/// and so where the pager goes next. Reads the latest snapshot each time, so one instance lives as long as the page.
/// </summary>
public sealed class LogStanding(AppData data)
{
    public Exercise? ExerciseOf(string id) => data.Snapshot.Exercises.FirstOrDefault(e => e.Id == id);

    public string NameOf(SessionExercise planned) => ExerciseOf(planned.ExerciseId)?.Name ?? "Exercise";

    public bool IsLoggable(SessionExercise planned) => ExerciseOf(planned.ExerciseId) is { } e && RowSpec.IsLoggable(e.LogKind);

    public static int Confirmed(Workout workout, string exerciseId) => WorkoutEdits.ConfirmedSets(workout, exerciseId).Count;

    public bool IsComplete(Workout workout, SessionExercise planned) =>
        IsLoggable(planned) && Confirmed(workout, planned.ExerciseId) >= planned.Sets;

    /// <summary>The skip recorded for a planned Exercise, or null when it is not skipped.</summary>
    public static SkipInfo? SkipOf(Workout workout, SessionExercise planned) =>
        WorkoutOutcomes.Of(workout).Rows.FirstOrDefault(r => r.Kind == RowOutcomeKind.Skipped && r.Row.ExerciseId == planned.ExerciseId) is { } skipped
            ? new SkipInfo(skipped.SkipReason)
            : null;

    /// <summary>A swapped row whose replacement has Sets (the Core rule): an unlogged swap is not done, so Finish still prompts Partial.</summary>
    public static bool IsSwapLogged(Workout workout, LogSlot slot) =>
        slot.SwappedFromId is { } original
        && WorkoutOutcomes.Of(workout).Rows.FirstOrDefault(r => r.Kind == RowOutcomeKind.Swapped && r.Row.ExerciseId == original)?.Entry is { } entry
        && WorkoutOutcomes.HasSets(entry);

    /// <summary>Complete, skipped or swapped with Sets, or an optional added Exercise: nothing more is needed for this one.</summary>
    public bool IsHandled(Workout workout, LogSlot slot) =>
        slot.Added || IsSwapLogged(workout, slot) || IsComplete(workout, slot.Row) || SkipOf(workout, slot.Row) is not null;

    /// <summary>The first Exercise still to log, else the last one: where a reopened Workout lands.</summary>
    public int FirstIncomplete(Workout workout)
    {
        var slots = LogSlots.Of(workout);
        var index = slots.ToList().FindIndex(s => !s.Added && !IsComplete(workout, s.Row) && SkipOf(workout, s.Row) is null);
        return index < 0 ? Math.Max(0, slots.Count - 1) : index;
    }

    /// <summary>
    /// Where the log goes after an Exercise is skipped: the next Exercise (wrapping round) that is not complete or skipped, else the last
    /// one, whose primary button is then Finish.
    /// </summary>
    public int AfterSkip(Workout workout, int from)
    {
        var planned = LogSlots.Of(workout);
        for (var step = 1; step < planned.Count; step++)
        {
            var index = (from + step) % planned.Count;
            if (!IsHandled(workout, planned[index]))
            {
                return index;
            }
        }

        return Math.Max(0, planned.Count - 1);
    }

    /// <summary>Where the Exercise with this ID is in the pager, or null when it is not in the Workout.</summary>
    public int? IndexOf(Workout workout, string exerciseId) =>
        LogSlots.Of(workout).ToList().FindIndex(s => s.Row.ExerciseId == exerciseId) is var index and >= 0 ? index : null;

    public static int SlotCount(Workout workout) => LogSlots.Of(workout).Count;

    /// <summary>An added Exercise only counts once logged; a swapped one counts once it has Sets, so neither holds the count or Finish back.</summary>
    public LogProgress ProgressOf(Workout workout, IReadOnlyList<LogSlot> slots, int prCount) =>
        new(
            slots.Count(s => !s.Added || IsComplete(workout, s.Row)),
            slots.Count(s => IsComplete(workout, s.Row)),
            prCount,
            slots.Count(s => SkipOf(workout, s.Row) is not null),
            slots.Count(s => IsSwapLogged(workout, s) && !IsComplete(workout, s.Row)));

    /// <summary>The pager's chips: name, done, skipped, swapped.</summary>
    public IReadOnlyList<ExercisePager.Chip> ChipsOf(Workout workout, IReadOnlyList<LogSlot> slots) =>
        [.. slots.Select(s => new ExercisePager.Chip(NameOf(s.Row), IsComplete(workout, s.Row), SkipOf(workout, s.Row) is not null, s.SwappedFromId is not null))];

    /// <summary>What is known of an Exercise standing in a slot: the swap it replaces (if any) and whether it was added.</summary>
    public (SwapInfo? Swap, bool Added) MarksOf(LogSlot slot) =>
        (slot.SwappedFromId is { } original ? new SwapInfo(original, ExerciseOf(original)?.Name ?? "Exercise") : null, slot.Added);

    /// <summary>What the picker is asked for an Exercise in hand (Swap, and Add after it); the Core picker rule ranks and filters.</summary>
    public PickerRequest PickerFor(Workout workout, Exercise current) =>
        PickerRequest.ForSwap(current, workout, data.Snapshot.Exercises);
}
