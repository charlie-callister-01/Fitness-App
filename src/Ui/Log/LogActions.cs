using Core.Ids;
using Core.Model;
using Core.Rules.Editing;
using Core.Rules.Parsing;
using Core.Rules.Units;
using Core.Storage;
using Ui.Saving;

namespace Ui.Log;

/// <summary>
/// Every write the log screen makes, in one place: confirming a Set, Max, RPE and the Exercise menu's actions
/// (<see cref="ApplyAsync"/>). Every save goes through one <see cref="GuardedSave"/>, so two taps save once and a window that cannot
/// write does nothing. A failed save changes nothing on screen (the guard reports it to the save-failure notice). Where the pager goes
/// after a write is decided here.
/// </summary>
public sealed class LogActions
{
    private readonly AppData _data;
    private readonly IIdGenerator _ids;
    private readonly LogEditState _state;
    private readonly LogViewBuilder _builder;
    private readonly LogSession _session;
    private readonly GuardedSave _save;
    private readonly WorkoutWriter _writer;
    private readonly ExerciseWriter _exerciseWriter;

    public LogActions(
        AppData data, SaveFailureNotice notice, IIdGenerator ids, LogEditState state, LogViewBuilder builder, LogSession session)
    {
        _data = data;
        _ids = ids;
        _state = state;
        _builder = builder;
        _session = session;
        _save = new GuardedSave(data, notice);
        _writer = new WorkoutWriter(data, _save);
        _exerciseWriter = new ExerciseWriter(data, _save);
    }

    /// <summary>The Workout the log is open on; the page sets it from its route.</summary>
    public string WorkoutId { get; set; } = "";

    /// <summary>Whether a tap should save: nothing is saving and this window can write.</summary>
    public bool CanAct => _save.CanAct;

    private Workout? Open =>
        _data.Snapshot.Workouts.FirstOrDefault(w => w.Id == WorkoutId) is { EndedAt: null } open ? open : null;

    /// <summary>Does what the Exercise menu asked for.</summary>
    public Task ApplyAsync(ExerciseAction action) => action switch
    {
        ExerciseAction.Swap swap => ChangeAsync(w => WorkoutEdits.Swap(w, swap.RowId, swap.NewExerciseId)),
        ExerciseAction.UndoSwap undo => ChangeAsync(w => WorkoutEdits.UndoSwap(w, undo.RowId)),
        ExerciseAction.Skip skip => SkipAsync(skip),
        ExerciseAction.Unskip unskip => ChangeAsync(w => WorkoutEdits.Unskip(w, unskip.RowId)),
        ExerciseAction.Add add => AddAsync(add),
        ExerciseAction.Remove remove => RemoveAsync(remove),
        ExerciseAction.ToggleBenchmark mark => _exerciseWriter.ChangeAsync(mark.ExerciseId, e => ExerciseEdits.WithBenchmark(e, mark.IsBenchmark)),
        _ => throw new ArgumentOutOfRangeException(nameof(action)),
    };

    /// <summary>Sets or clears the Exercise's RPE, creating its entry when no Set has been confirmed yet. Never blocks Confirm.</summary>
    public Task SetRpeAsync(string exerciseId, int? rpe) => ChangeAsync(w => WorkoutEdits.WithRpe(w, exerciseId, rpe));

    /// <summary>A Set that exists is flagged in the stored Workout at once; a row still to be confirmed keeps the flag until it is.</summary>
    public async Task ToggleMaxAsync(ExerciseView exercise)
    {
        if (_save.IsBusy || exercise.Rows is not { Tools.MaxTarget: { } index } rows)
        {
            return;
        }

        var id = exercise.Info.ExerciseId;
        var marks = rows.Items[index].Marks;
        if (marks.Stored)
        {
            await ChangeAsync(w => WorkoutEdits.WithMax(w, id, index, !marks.IsMax));
        }
        else
        {
            _state.MarkMax(new RowKey(id, index), !marks.IsMax, marks.Pending);
        }
    }

    /// <summary>Confirms the shown Exercise's current row: a typed value that does not parse stays on screen with its error.</summary>
    public async Task ConfirmSetAsync(ExerciseView exercise)
    {
        if (!CanAct || exercise.Rows?.CurrentRow is not { } row)
        {
            return;
        }

        var info = exercise.Info;
        var key = new RowKey(info.ExerciseId, row.Index);
        var parser = new InputParser(new UnitConverter(_data.Snapshot.Settings));
        switch (RowInput.Check(info.Kind, row.Face.First, row.Face.Second, parser))
        {
            case RowCheck.Invalid invalid:
                _state.Fail(key, invalid.Error, invalid.Field);
                break;
            case RowCheck.Valid valid:
                var isMax = _state.EditOf(key)?.Max ?? false;
                var result = await _writer.ChangeAsync(
                    WorkoutId, w => WorkoutEdits.WithSet(w, info.ExerciseId, row.Index, valid.Set, isMax, _ids));
                if (result.IsSaved)
                {
                    _state.Confirmed(key);
                    _session.StartRest(Open is { LoggedAfterwards: true });
                    _state.ShowToast(Open is { } saved ? _builder.ToastFor(saved, info.ExerciseId, row.Index) : null);
                }

                break;
        }
    }

    /// <summary>Skips the Exercise (dropping any Sets) and moves on to the next one not yet handled.</summary>
    private async Task SkipAsync(ExerciseAction.Skip skip)
    {
        var result = await _writer.ChangeAsync(WorkoutId, w => WorkoutEdits.Skip(w, skip.RowId, skip.Reason));
        if (result.IsSaved && Open is { } saved)
        {
            _state.Show(_builder.Standing.AfterSkip(saved, _state.Current));
        }
    }

    /// <summary>Adds an Exercise after the shown one and goes to it.</summary>
    private async Task AddAsync(ExerciseAction.Add add)
    {
        var result = await _writer.ChangeAsync(WorkoutId, w => WorkoutEdits.AddExercise(w, add.AfterExerciseId, add.NewExerciseId));
        if (result.IsSaved && Open is { } saved && _builder.Standing.IndexOf(saved, add.NewExerciseId) is { } index)
        {
            _state.Show(index);
        }
    }

    private async Task RemoveAsync(ExerciseAction.Remove remove)
    {
        var result = await _writer.ChangeAsync(WorkoutId, w => WorkoutEdits.RemoveAdded(w, remove.ExerciseId));
        if (result.IsSaved)
        {
            _state.Show(Math.Max(0, _state.Current - 1));
        }
    }

    private Task ChangeAsync(Func<Workout, Workout> change) => _writer.ChangeAsync(WorkoutId, change);
}
