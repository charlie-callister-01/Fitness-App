using Core.Model;
using Core.Rules.LogKinds;
using Ui.Components;

namespace Ui.Log;

/// <summary>The text in a row's two fields and what an empty field hints at.</summary>
public sealed record RowFace(string First, string Second, string? FirstHint, string? SecondHint);

/// <summary>What a row's state cell and label say about it.</summary>
/// <param name="Pending">The values are only the prefill (or carried by a stepper): shown muted until confirmed or edited.</param>
/// <param name="Stored">The row is a stored Set (confirmed or being reopened).</param>
public sealed record RowMarks(bool Pending, bool Stored, bool IsMax, bool IsPr);

public sealed record RowView(int Index, SetRowState State, RowFace Face, RowMarks Marks, bool HasError);

/// <summary>Last, Best and the Suggested target as text in the owner's units; a part is null when there is none.</summary>
public sealed record HeaderText(string? Last, string? Best, string? Target, string? Reason);

public sealed record ExerciseInfo(string ExerciseId, string Name, LogKind Kind, string? PlannedText, string? Cue, bool IsBenchmark);

/// <summary>The links and steppers around the rows.</summary>
/// <param name="Step">The steppers' step while a row is waiting to be confirmed; null hides them.</param>
/// <param name="MaxTarget">The row the Max set link acts on: the current row, else the last confirmed one.</param>
/// <param name="RemovableRow">The last row when it is an extra, unconfirmed one that Remove would take away.</param>
public sealed record RowTools(StepSize? Step, int? MaxTarget, int? RemovableRow)
{
    public string? StepAmount => Step?.Amount.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
}

/// <summary>The rows of a loggable Exercise and how each is laid out.</summary>
public sealed record ExerciseRows(RowSpec Spec, IReadOnlyList<RowView> Items, RowTools Tools)
{
    public RowView? CurrentRow => Items.FirstOrDefault(r => r.State == SetRowState.Current);
}

/// <summary>An Exercise as shown: its rows, or the reason it has none (<see cref="Unavailable"/>).</summary>
public sealed record ExerciseView(ExerciseInfo Info, HeaderText? Header, ExerciseRows? Rows, string? Unavailable, int? Rpe, SkipInfo? Skip = null);

/// <summary>An Exercise left undone on purpose: its Sets are hidden and the optional reason is shown.</summary>
public sealed record SkipInfo(string? Reason);

public enum PrimaryAction
{
    ConfirmSet,
    Next,
    Finish,
}

/// <summary>The one primary button: its words, what it does and, for Next, which Exercise it leads to.</summary>
public sealed record PrimaryButton(string Label, PrimaryAction Action, int? JumpTo = null)
{
    public bool NeedsWrite => Action != PrimaryAction.Next;
}

public sealed record LogProgress(int ExerciseCount, int DoneCount, int PrCount, int SkippedCount = 0)
{
    public bool AllDone => ExerciseCount > 0 && DoneCount + SkippedCount == ExerciseCount;
}

public sealed record LogView(
    string SessionName, LogProgress Progress, ExerciseView? Exercise, PrimaryButton Primary,
    IReadOnlyList<ExercisePager.Chip> Chips, string? UpNext);
