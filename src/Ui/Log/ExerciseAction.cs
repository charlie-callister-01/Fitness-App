namespace Ui.Log;

/// <summary>
/// What the Exercise menu can ask for, as one closed set. <see cref="LogActions.ApplyAsync"/> is the only place that turns one into a
/// write. Skip, Swap and Un-skip name the planned row they act on (<c>RowId</c>); the others name the Exercise shown.
/// </summary>
public abstract record ExerciseAction
{
    public sealed record Swap(string RowId, string NewExerciseId) : ExerciseAction;

    public sealed record UndoSwap(string RowId) : ExerciseAction;

    public sealed record Skip(string RowId, string? Reason) : ExerciseAction;

    public sealed record Unskip(string RowId) : ExerciseAction;

    /// <summary>Adds <c>NewExerciseId</c> after the shown Exercise (<c>AfterExerciseId</c>) and goes to it.</summary>
    public sealed record Add(string AfterExerciseId, string NewExerciseId) : ExerciseAction;

    public sealed record Remove(string ExerciseId) : ExerciseAction;

    public sealed record ToggleBenchmark(string ExerciseId, bool IsBenchmark) : ExerciseAction;
}
