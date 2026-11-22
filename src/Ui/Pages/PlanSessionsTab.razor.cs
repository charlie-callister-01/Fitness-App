using Core.Ids;
using Core.Model;
using Core.Rules.Library;
using Core.Rules.PlanEditing;
using Core.Storage;
using Microsoft.AspNetCore.Components;

namespace Ui.Pages;

/// <summary>The Session sheet's fields while it is open: the name, default day and type, and which Session it edits (null when adding).</summary>
public sealed class SessionSheetDraft
{
    public string? EditingSessionId { get; init; }

    public string Name { get; set; } = "";

    public DefaultDay Day { get; set; } = DefaultDay.Monday;

    public SessionType Type { get; set; } = SessionType.Strength;

    /// <summary>The plain refusal to show in the sheet, or null.</summary>
    public string? Message { get; set; }
}

public sealed partial class PlanSessionsTab
{
    private static readonly DefaultDay[] Days =
        [DefaultDay.Monday, DefaultDay.Tuesday, DefaultDay.Wednesday, DefaultDay.Thursday, DefaultDay.Friday, DefaultDay.Saturday, DefaultDay.Sunday];

    private IReadOnlyList<Exercise>? _libraryList;
    private Dictionary<string, Exercise> _library = [];
    private string? _expandedKey;
    private string? _pickerSessionId;
    private bool _sheetOpen;
    private SessionSheetDraft _sheet = new();

    [Inject] private AppData Data { get; set; } = default!;

    [Inject] private IIdGenerator Ids { get; set; } = default!;

    [Parameter, EditorRequired]
    public PlanEditState State { get; set; } = default!;

    /// <summary>The page's shared context: whether this window can write, and the plain reason when it cannot.</summary>
    [Parameter, EditorRequired]
    public PageContext Page { get; set; } = default!;

    /// <summary>The page's save is running.</summary>
    [Parameter]
    public bool Saving { get; set; }

    /// <summary>Something in the tab changed, so the page can re-check Save.</summary>
    [Parameter]
    public EventCallback OnEdited { get; set; }

    /// <summary>The whole editor cannot be changed (read-only window or a save running).</summary>
    private bool Disabled => !Page.CanWrite || Saving;

    private PickerRequest PickerRequestForOpenSession =>
        PickerRequest.ForPlanEditor(
            Data.Snapshot.Exercises,
            State.Draft.Sessions.FirstOrDefault(s => s.Id == _pickerSessionId)?.Exercises.Select(r => r.ExerciseId).ToList() ?? []);

    /// <summary>What the row shows about its library Exercise: the name (the id when it is not in the library) and the Benchmark tick.</summary>
    private RowExercise Lookup(string exerciseId)
    {
        if (!ReferenceEquals(_libraryList, Data.Snapshot.Exercises))
        {
            _libraryList = Data.Snapshot.Exercises;
            _library = _libraryList.ToDictionary(e => e.Id);
        }

        return _library.TryGetValue(exerciseId, out var exercise)
            ? new RowExercise(exercise.Name, true, exercise.IsBenchmark, State.IsBenchmark(exerciseId, exercise.IsBenchmark))
            : new RowExercise(exerciseId, false, false, false);
    }

    /// <summary>A row's Exercise as the row shows it. The Benchmark tick can be changed only for an Exercise in the library.</summary>
    private sealed record RowExercise(string Name, bool InLibrary, bool LibraryBenchmark, bool IsBenchmark);

    private void Toggle(string key) => _expandedKey = _expandedKey == key ? null : key;

    private Task ChangeRows(string sessionId, Func<IReadOnlyList<SessionExercise>, IReadOnlyList<SessionExercise>> change)
    {
        State.Change(plan => SessionRowRules.WithRows(plan, sessionId, change(plan.Sessions.First(s => s.Id == sessionId).Exercises)));
        return OnEdited.InvokeAsync();
    }

    private Task ChangeRow(string sessionId, int index, Func<SessionExercise, SessionExercise> change) =>
        ChangeRows(sessionId, rows => [.. rows.Select((r, i) => i == index ? change(r) : r)]);

    // Text that is not a whole number is kept as typed (State.Inputs) and holds Save back; the row keeps its last good number.
    private Task SetNumber(string sessionId, string exerciseId, int index, RowNumber field, ChangeEventArgs e)
    {
        if (!State.Inputs.TryRead(sessionId, exerciseId, field, e.Value?.ToString(), out var number))
        {
            return OnEdited.InvokeAsync();
        }

        return ChangeRow(sessionId, index, row => field switch
        {
            RowNumber.Sets => row with { Sets = number ?? 0 },
            RowNumber.Reps => row with { Reps = number },
            _ => row with { RepsMax = number },
        });
    }

    private string NumberText(string sessionId, SessionExercise row, RowNumber field) =>
        State.Inputs.UnreadableText(sessionId, row.ExerciseId, field)
        ?? (field switch { RowNumber.Sets => row.Sets, RowNumber.Reps => row.Reps, _ => row.RepsMax })?.ToString() ?? "";

    private Task SetCue(string sessionId, int index, ChangeEventArgs e)
    {
        var cue = e.Value?.ToString();
        return ChangeRow(sessionId, index, r => r with { Cue = string.IsNullOrWhiteSpace(cue) ? null : cue });
    }

    private Task Move(string sessionId, int index, bool up) =>
        ChangeRows(sessionId, rows => up ? SessionRowRules.MoveUp(rows, index) : SessionRowRules.MoveDown(rows, index));

    private Task Remove(string sessionId, string exerciseId, int index)
    {
        _expandedKey = null;
        State.Inputs.Forget(sessionId, exerciseId);
        return ChangeRows(sessionId, rows => SessionRowRules.Remove(rows, index));
    }

    // The Benchmark flag belongs to the Exercise in the library; the tick waits in the draft and is saved with the Plan.
    private Task SetBenchmark(string exerciseId, bool libraryValue, ChangeEventArgs e)
    {
        State.SetBenchmark(exerciseId, e.Value is true, libraryValue);
        return OnEdited.InvokeAsync();
    }

    private async Task AddFromPickerAsync(string exerciseId)
    {
        if (_pickerSessionId is not { } sessionId)
        {
            return;
        }

        _pickerSessionId = null;
        await ChangeRows(sessionId, rows => SessionRowRules.AddFromLibrary(rows, exerciseId));
    }

    private void ClosePicker() => _pickerSessionId = null;

    private void OpenAddSessionSheet()
    {
        _sheet = new SessionSheetDraft();
        _sheetOpen = true;
    }

    private void OpenSessionSheet(string sessionId)
    {
        var session = State.Draft.Sessions.First(s => s.Id == sessionId);
        _sheet = new SessionSheetDraft { EditingSessionId = sessionId, Name = session.Name, Day = session.DefaultDay, Type = session.Type };
        _sheetOpen = true;
    }

    private void CloseSessionSheet() => _sheetOpen = false;

    private async Task ApplySessionSheet()
    {
        var result = _sheet.EditingSessionId is { } id
            ? SessionRowRules.EditSession(State.Draft, id, _sheet.Name, _sheet.Day, _sheet.Type)
            : SessionRowRules.AddSession(State.Draft, _sheet.Name, _sheet.Day, _sheet.Type, Ids);
        if (!result.TryGet(out var changed, out var reason))
        {
            _sheet.Message = reason;
            return;
        }

        State.Change(_ => changed);
        _sheetOpen = false;
        await OnEdited.InvokeAsync();
    }
}
