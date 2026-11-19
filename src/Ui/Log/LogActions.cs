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
/// (<see cref="ApplyAsync"/>). One busy guard stops two taps saving twice; a window that cannot write does nothing. A failed save
/// changes nothing on screen (the writer reports it to the save-failure notice). Where the pager goes after a write is decided here.
/// </summary>
public sealed class LogActions(
    AppData data, SaveFailureNotice notice, IIdGenerator ids, LogEditState state, LogViewBuilder builder, LogSession session)
{
    private readonly WorkoutWriter _writer = new(data, notice);
    private readonly ExerciseWriter _exerciseWriter = new(data, notice);
    private bool _busy;

    /// <summary>The Workout the log is open on; the page sets it from its route.</summary>
    public string WorkoutId { get; set; } = "";

    /// <summary>Whether a tap should save: nothing is saving and this window can write.</summary>
    public bool CanAct => !_busy && data.Health.CanWrite;

    private Workout? Open =>
        data.Snapshot.Workouts.FirstOrDefault(w => w.Id == WorkoutId) is { EndedAt: null } open ? open : null;

    /// <summary>Does what the Exercise menu asked for.</summary>
    public Task ApplyAsync(ExerciseAction action) => action switch
    {
        ExerciseAction.Swap swap => ChangeAsync(w => WorkoutEdits.Swap(w, swap.RowId, swap.NewExerciseId)),
        ExerciseAction.UndoSwap undo => ChangeAsync(w => WorkoutEdits.UndoSwap(w, undo.RowId)),
        ExerciseAction.Skip skip => SkipAsync(skip),
        ExerciseAction.Unskip unskip => ChangeAsync(w => WorkoutEdits.Unskip(w, unskip.RowId)),
        ExerciseAction.Add add => AddAsync(add),
        ExerciseAction.Remove remove => RemoveAsync(remove),
        ExerciseAction.ToggleBenchmark mark => RunExclusive(
            () => _exerciseWriter.ChangeAsync(mark.ExerciseId, e => ExerciseEdits.WithBenchmark(e, mark.IsBenchmark))),
        _ => throw new ArgumentOutOfRangeException(nameof(action)),
    };

    /// <summary>Sets or clears the Exercise's RPE, creating its entry when no Set has been confirmed yet. Never blocks Confirm.</summary>
    public Task SetRpeAsync(string exerciseId, int? rpe) => ChangeAsync(w => WorkoutEdits.WithRpe(w, exerciseId, rpe));

    /// <summary>A Set that exists is flagged in the stored Workout at once; a row still to be confirmed keeps the flag until it is.</summary>
    public async Task ToggleMaxAsync(ExerciseView exercise)
    {
        if (_busy || exercise.Rows is not { Tools.MaxTarget: { } index } rows)
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
            state.MarkMax(new RowKey(id, index), !marks.IsMax, marks.Pending);
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
        var parser = new InputParser(new UnitConverter(data.Snapshot.Settings));
        switch (RowInput.Check(info.Kind, row.Face.First, row.Face.Second, parser))
        {
            case RowCheck.Invalid invalid:
                state.Fail(key, invalid.Error, invalid.Field);
                break;
            case RowCheck.Valid valid:
                var isMax = state.EditOf(key)?.Max ?? false;
                await RunExclusive(async () =>
                {
                    var result = await _writer.ChangeAsync(
                        WorkoutId, w => WorkoutEdits.WithSet(w, info.ExerciseId, row.Index, valid.Set, isMax, ids));
                    if (result is CommitResult.Committed)
                    {
                        state.Confirmed(key);
                        session.StartRest(Open is { LoggedAfterwards: true });
                        state.Toast = Open is { } saved ? builder.ToastFor(saved, info.ExerciseId, row.Index) : null;
                    }
                });
                break;
        }
    }

    /// <summary>Skips the Exercise (dropping any Sets) and moves on to the next one not yet handled.</summary>
    private Task SkipAsync(ExerciseAction.Skip skip) =>
        RunExclusive(async () =>
        {
            var result = await _writer.ChangeAsync(WorkoutId, w => WorkoutEdits.Skip(w, skip.RowId, skip.Reason));
            if (result is CommitResult.Committed && Open is { } saved)
            {
                state.Show(builder.Standing.AfterSkip(saved, state.Current));
            }
        });

    /// <summary>Adds an Exercise after the shown one and goes to it.</summary>
    private Task AddAsync(ExerciseAction.Add add) =>
        RunExclusive(async () =>
        {
            var result = await _writer.ChangeAsync(WorkoutId, w => WorkoutEdits.AddExercise(w, add.AfterExerciseId, add.NewExerciseId));
            if (result is CommitResult.Committed && Open is { } saved && builder.Standing.IndexOf(saved, add.NewExerciseId) is { } index)
            {
                state.Show(index);
            }
        });

    private Task RemoveAsync(ExerciseAction.Remove remove) =>
        RunExclusive(async () =>
        {
            var result = await _writer.ChangeAsync(WorkoutId, w => WorkoutEdits.RemoveAdded(w, remove.ExerciseId));
            if (result is CommitResult.Committed)
            {
                state.Show(Math.Max(0, state.Current - 1));
            }
        });

    private Task ChangeAsync(Func<Workout, Workout> change) => RunExclusive(() => _writer.ChangeAsync(WorkoutId, change));

    /// <summary>Runs a save unless another is in flight or this window cannot write.</summary>
    private async Task RunExclusive(Func<Task> save)
    {
        if (!CanAct)
        {
            return;
        }

        _busy = true;
        try
        {
            await save();
        }
        finally
        {
            _busy = false;
        }
    }
}
