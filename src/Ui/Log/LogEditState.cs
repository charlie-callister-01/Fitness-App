using Core.Rules.LogKinds;
using Ui.Components;

namespace Ui.Log;

/// <summary>One Set row of one Exercise.</summary>
public readonly record struct RowKey(string ExerciseId, int Row);

/// <summary>What the owner did to a row that is not stored yet.</summary>
/// <param name="First">The first field's text; null still shows the prefill.</param>
/// <param name="Typed">The owner typed, stepped or reopened this row, as opposed to a stepper only carrying a value down to it.</param>
/// <param name="Max">Marked as a Max set before it is confirmed; a stored Set carries its own flag.</param>
public sealed record RowEdit(string? First = null, string? Second = null, bool Typed = false, bool Max = false);

/// <summary>The plain-language message under a row and the field it is about.</summary>
public sealed record RowError(RowKey Row, string Message, RowField Field);

/// <summary>Asks a row to move focus to a field; the sequence number makes each ask new.</summary>
public sealed record FocusRequest(RowKey Row, RowField Field, int Sequence);

/// <summary>
/// Everything the log screen holds that is not in the stored Workout: the Exercise showing, what was typed but not confirmed,
/// rows added with Add Set, the one reopened row, the failed row, the focus ask and the toast. Every transition is a method
/// here, so a row can never be left half-edited: a reopened row that is abandoned loses its edit (the stored Set shows again),
/// and a stepper only carries a value to rows the owner has not typed in.
/// </summary>
public sealed class LogEditState
{
    private readonly Dictionary<RowKey, RowEdit> _edits = [];
    private readonly Dictionary<string, int> _extraRows = [];
    private int _focusSequence;

    /// <summary>Index of the Exercise showing.</summary>
    public int Current { get; private set; }

    /// <summary>"Resumed where you left off" is shown.</summary>
    public bool Resumed { get; private set; }

    /// <summary>The confirmed row reopened for editing, if any.</summary>
    public RowKey? Editing { get; private set; }

    public RowError? Error { get; private set; }

    public FocusRequest? Focus { get; private set; }

    /// <summary>The "PR!" line for the Set just confirmed; cleared by any other action.</summary>
    public string? Toast { get; private set; }

    /// <summary>Bumped each time a Set's confirm sets <see cref="Toast"/>, so the line reads out a repeated "PR!" again.</summary>
    public int ToastKey { get; private set; }

    /// <summary>Shows the "PR!" line for the Set just confirmed, or none.</summary>
    public void ShowToast(string? toast)
    {
        Toast = toast;
        ToastKey++;
    }

    /// <summary>Starts over for a Workout: shows <paramref name="current"/>, drops every edit.</summary>
    public void Reset(int current, bool resumed)
    {
        _edits.Clear();
        _extraRows.Clear();
        Current = current;
        Resumed = resumed;
        Editing = null;
        Error = null;
        Focus = null;
        Toast = null;
    }

    /// <summary>Keeps the shown Exercise inside the Workout's list.</summary>
    public void ClampTo(int exerciseCount) => Current = Math.Clamp(Current, 0, Math.Max(0, exerciseCount - 1));

    public RowEdit? EditOf(RowKey key) => _edits.GetValueOrDefault(key);

    public int ExtraRows(string exerciseId) => _extraRows.GetValueOrDefault(exerciseId);

    /// <summary>Shows another Exercise. Confirmed Sets are saved and typed text stays; an abandoned reopen is dropped.</summary>
    public void Show(int index)
    {
        Current = index;
        Toast = null;
        StopEditing();
        Error = null;
    }

    /// <summary>The owner typed into a field. The other field keeps what the row shows now.</summary>
    public void Type(RowKey key, RowField field, string text, RowFace shown)
    {
        var edit = EditOf(key) ?? new RowEdit();
        _edits[key] = field == RowField.First
            ? edit with { First = text, Second = edit.Second ?? shown.Second, Typed = true }
            : edit with { First = edit.First ?? shown.First, Second = text, Typed = true };
    }

    /// <summary>Opens a confirmed row for editing, starting from what it shows. Any other reopened row is let go first.</summary>
    public void Reopen(RowKey key, RowFace shown)
    {
        StopEditing();
        Editing = key;
        _edits[key] = new RowEdit(shown.First, shown.Second, Typed: true);
        Toast = null;
        Error = null;
        RequestFocus(key, RowField.First);
    }

    /// <summary>Lets go of the reopened row without saving: its edit goes, so the stored Set shows again.</summary>
    public void StopEditing()
    {
        if (Editing is { } key)
        {
            _edits.Remove(key);
        }

        Editing = null;
    }

    /// <summary>The row was saved: its edit, the reopen and any error are done with.</summary>
    public void Confirmed(RowKey key)
    {
        _edits.Remove(key);
        Editing = null;
        Error = null;
    }

    public void Fail(RowKey key, string message, RowField field)
    {
        Error = new RowError(key, message, field);
        RequestFocus(key, field);
    }

    /// <summary>
    /// A stepper press on <paramref name="current"/>: its field takes the stepped value, and so do the later unconfirmed rows the
    /// owner has not typed in (they stay muted). A later row the owner typed in keeps its text.
    /// </summary>
    public void Step(string exerciseId, IReadOnlyList<RowView> rows, RowView current, StepSize step, int direction)
    {
        var stepped = step.Apply(step.Field == StepField.Second ? current.Face.Second : current.Face.First, direction);
        foreach (var row in rows.Where(r => r.Index >= current.Index && r.State != SetRowState.Done))
        {
            var key = new RowKey(exerciseId, row.Index);
            var edit = EditOf(key) ?? new RowEdit();
            var isCurrent = row.Index == current.Index;
            if (!isCurrent && edit.Typed)
            {
                continue;
            }

            _edits[key] = step.Field == StepField.Second
                ? edit with { First = edit.First ?? row.Face.First, Second = stepped, Typed = isCurrent }
                : edit with { First = stepped, Second = edit.Second ?? row.Face.Second, Typed = isCurrent };
        }

        Error = null;
    }

    public void AddSet(string exerciseId) => _extraRows[exerciseId] = ExtraRows(exerciseId) + 1;

    public void RemoveSet(string exerciseId, int row)
    {
        _extraRows[exerciseId] = Math.Max(0, ExtraRows(exerciseId) - 1);
        var key = new RowKey(exerciseId, row);
        _edits.Remove(key);
        if (Error?.Row == key)
        {
            Error = null;
        }
    }

    /// <summary>Marks or unmarks a row that is not stored yet as a Max set; it keeps its pending look unless already edited.</summary>
    public void MarkMax(RowKey key, bool isMax, bool pending) =>
        _edits[key] = (EditOf(key) ?? new RowEdit(Typed: !pending)) with { Max = isMax };

    private void RequestFocus(RowKey key, RowField field) => Focus = new FocusRequest(key, field, ++_focusSequence);
}
