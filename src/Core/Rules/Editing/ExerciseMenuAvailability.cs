namespace Core.Rules.Editing;

public enum MenuItemAvailability
{
    Hidden,
    Disabled,
    Enabled,
}

/// <summary>What the Exercise menu needs to know about an Exercise to decide its items.</summary>
/// <param name="HasSets">The Exercise has confirmed Sets: Undo swap and Remove are then refused (as <see cref="WorkoutEdits"/> does).</param>
/// <param name="CanWrite">False when this window cannot save: every item that writes is disabled.</param>
/// <param name="HasChoices">The library has Exercises to pick from, which Swap and Add need.</param>
public sealed record ExerciseMenuFacts(
    bool IsSkipped, bool IsSwapped, bool IsAdded, bool HasSets, bool IsBenchmark, bool CanWrite, bool HasChoices);

/// <summary>
/// Which items of the Exercise menu are shown and which of those can be used (design 8.4). View Guide is not here: it is always
/// available. One rule, so the menu cannot offer what the Core edits would refuse.
/// </summary>
public sealed record ExerciseMenuAvailability(
    MenuItemAvailability Swap,
    MenuItemAvailability UndoSwap,
    MenuItemAvailability Skip,
    MenuItemAvailability Unskip,
    MenuItemAvailability Add,
    MenuItemAvailability Remove,
    MenuItemAvailability Benchmark)
{
    public static ExerciseMenuAvailability Of(ExerciseMenuFacts f) => new(
        Swap: Shown(true, f.CanWrite && !f.IsAdded && f.HasChoices),
        UndoSwap: Shown(f.IsSwapped, f.CanWrite && !f.HasSets),
        Skip: Shown(!f.IsSkipped && !f.IsAdded, f.CanWrite),
        Unskip: Shown(f.IsSkipped, f.CanWrite),
        Add: Shown(true, f.CanWrite && f.HasChoices),
        Remove: Shown(f.IsAdded, f.CanWrite && !f.HasSets),
        Benchmark: Shown(true, f.CanWrite));

    private static MenuItemAvailability Shown(bool shown, bool enabled) =>
        !shown ? MenuItemAvailability.Hidden : enabled ? MenuItemAvailability.Enabled : MenuItemAvailability.Disabled;
}
