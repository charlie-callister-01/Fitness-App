using Core.Model;
using Core.Rules.Finishing;
using Core.Rules.Outcomes;
using Core.Tests.Ids;

namespace Core.Tests.Rules.Finishing;

public class WorkoutFinishingTests
{
    private static readonly SequentialIdGenerator Ids = new("p");
    private static readonly DateTimeOffset End = new(2026, 11, 16, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 11, 16);

    private static WorkoutSet Set() => new("s1", 20, 10, null, null, null, false);

    private static WorkoutEntry Done(string id, params WorkoutSet[] sets) => new(id, EntryStatus.Done, null, null, null, sets);

    private static Workout Open(params WorkoutEntry[] entries) => new(
        "w1", Today, "s1", false, End.AddHours(-1), null,
        [new SessionExercise("squat", 3, 8, null, null), new SessionExercise("press", 3, 8, null, null), new SessionExercise("row", 3, 8, null, null)],
        entries, null);

    // ---- Pain check

    [Fact]
    public void None_answers_with_no_areas_or_severity()
    {
        var result = WorkoutFinishing.WithPainNone(Open(), Ids).PainCheck!;

        Assert.Equal(PainStatus.None, result.Status);
        Assert.Empty(result.Areas);
        Assert.Null(result.Severity);
    }

    [Fact]
    public void Reported_keeps_areas_severity_exercise_and_note()
    {
        var result = WorkoutFinishing.WithPainReported(Open(), [BodyArea.Knee, BodyArea.Hip], PainSeverity.Sharp, "squat", "Left knee twinge", Ids);

        var pain = result.Workout!.PainCheck!;
        Assert.Equal(PainStatus.Reported, pain.Status);
        Assert.Equal([BodyArea.Knee, BodyArea.Hip], pain.Areas);
        Assert.Equal(PainSeverity.Sharp, pain.Severity);
        Assert.Equal("squat", pain.ExerciseId);
        Assert.Equal("Left knee twinge", pain.Note);
    }

    [Fact]
    public void Reported_without_an_area_is_refused()
    {
        var result = WorkoutFinishing.WithPainReported(Open(), [], PainSeverity.Mild, null, null, Ids);

        Assert.True(result.IsRefused);
        Assert.Null(result.Workout);
    }

    [Fact]
    public void Reported_without_a_severity_is_refused()
    {
        Assert.True(WorkoutFinishing.WithPainReported(Open(), [BodyArea.Knee], null, null, null, Ids).IsRefused);
    }

    [Fact]
    public void Clearing_leaves_the_workout_unanswered_not_none()
    {
        var answered = WorkoutFinishing.WithPainNone(Open(), Ids);

        Assert.Null(WorkoutFinishing.WithPainCleared(answered).PainCheck);
    }

    [Fact]
    public void Changing_the_answer_keeps_the_pain_check_id()
    {
        var first = WorkoutFinishing.WithPainNone(Open(), Ids);
        var second = WorkoutFinishing.WithPainReported(first, [BodyArea.Knee], PainSeverity.Mild, null, null, Ids).Workout!;

        Assert.Equal(first.PainCheck!.Id, second.PainCheck!.Id);
    }

    // ---- Finish

    [Fact]
    public void Finish_stamps_the_end_time()
    {
        var result = WorkoutFinishing.Finish(Open(Done("squat", Set())), false, End);

        Assert.Equal(End, result.Workout!.EndedAt);
    }

    [Fact]
    public void Finish_with_no_confirmed_Set_is_refused()
    {
        var result = WorkoutFinishing.Finish(Open(Done("squat")), true, End);

        Assert.True(result.IsRefused);
        Assert.Null(result.Workout);
    }

    [Fact]
    public void Finish_of_an_already_finished_workout_keeps_the_original_end_time()
    {
        var finished = WorkoutFinishing.Finish(Open(Done("squat", Set())), false, End).Workout!;

        var again = WorkoutFinishing.Finish(finished, true, End.AddHours(5));

        Assert.Equal(End, again.Workout!.EndedAt);
        Assert.Equal(finished, again.Workout);
    }

    [Fact]
    public void Partial_finish_records_unrecorded_rows_as_skipped_with_no_reason()
    {
        var workout = WorkoutFinishing.Finish(Open(Done("squat", Set())), true, End).Workout!;

        var skipped = workout.Entries.Where(e => e.Status == EntryStatus.Skipped).ToList();
        Assert.Equal(["press", "row"], skipped.Select(e => e.ExerciseId));
        Assert.All(skipped, e => Assert.Null(e.SkipReason));
        Assert.True(WorkoutOutcomes.Of(workout).IsPartial);
    }

    [Fact]
    public void Partial_finish_turns_an_empty_Done_entry_into_skipped_and_drops_its_RPE()
    {
        var rpeOnly = new WorkoutEntry("press", EntryStatus.Done, null, null, 8, []);

        var workout = WorkoutFinishing.Finish(Open(Done("squat", Set()), rpeOnly), true, End).Workout!;

        var press = Assert.Single(workout.Entries, e => e.ExerciseId == "press");
        Assert.Equal(EntryStatus.Skipped, press.Status);
        Assert.Null(press.Rpe);
    }

    [Fact]
    public void Partial_finish_leaves_swapped_and_fully_done_rows_alone()
    {
        var swapped = new WorkoutEntry("goblet", EntryStatus.Swapped, null, "press", null, [Set()]);
        var open = Open(Done("squat", Set()), swapped, Done("row", Set()));

        var workout = WorkoutFinishing.Finish(open, true, End).Workout!;

        Assert.Equal(open.Entries, workout.Entries);
        Assert.False(WorkoutOutcomes.Of(workout).IsPartial);
    }

    [Fact]
    public void Partial_finish_drops_an_added_Exercise_with_no_Sets_and_keeps_one_with_Sets()
    {
        var open = Open(Done("squat", Set()), Done("curl"), Done("dip", Set()));

        var workout = WorkoutFinishing.Finish(open, true, End).Workout!;

        Assert.DoesNotContain(workout.Entries, e => e.ExerciseId == "curl");
        Assert.Contains(workout.Entries, e => e.ExerciseId == "dip");
    }

    [Fact]
    public void Partial_finish_keeps_an_existing_skip_reason()
    {
        var skipped = new WorkoutEntry("press", EntryStatus.Skipped, "Shoulder", null, null, []);

        var workout = WorkoutFinishing.Finish(Open(Done("squat", Set()), skipped), true, End).Workout!;

        Assert.Equal("Shoulder", Assert.Single(workout.Entries, e => e.ExerciseId == "press").SkipReason);
    }

    // ---- Re-date

    [Fact]
    public void Logged_afterwards_sets_the_flag_and_date_and_clears_the_start_time()
    {
        var past = Today.AddDays(-3);

        var workout = WorkoutFinishing.LoggedAfterwards(Open(), past, Today).Workout!;

        Assert.True(workout.LoggedAfterwards);
        Assert.Equal(past, workout.Date);
        Assert.Null(workout.StartedAt);
    }

    [Fact]
    public void Logged_afterwards_allows_today_and_any_distance_back()
    {
        Assert.False(WorkoutFinishing.LoggedAfterwards(Open(), Today, Today).IsRefused);
        Assert.False(WorkoutFinishing.LoggedAfterwards(Open(), Today.AddYears(-3), Today).IsRefused);
    }

    [Fact]
    public void Logged_afterwards_refuses_a_future_date()
    {
        var result = WorkoutFinishing.LoggedAfterwards(Open(), Today.AddDays(1), Today);

        Assert.True(result.IsRefused);
        Assert.Null(result.Workout);
    }

    [Fact]
    public void Undo_restores_today_and_clears_the_flag()
    {
        var redated = WorkoutFinishing.LoggedAfterwards(Open(), Today.AddDays(-3), Today).Workout!;

        var undone = WorkoutFinishing.UndoLoggedAfterwards(redated, Today);

        Assert.False(undone.LoggedAfterwards);
        Assert.Equal(Today, undone.Date);
    }
}
