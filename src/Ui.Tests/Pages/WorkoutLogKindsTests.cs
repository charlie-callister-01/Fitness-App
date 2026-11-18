using AngleSharp.Dom;
using Bunit;
using Core.Model;
using Core.Rules.Starting;
using Core.Storage;
using Ui.Pages;
using static Ui.Tests.Pages.LogFixture;

namespace Ui.Tests.Pages;

/// <summary>One scenario per log kind: type into the right fields, Confirm, and check the Set stored in canonical units.</summary>
public sealed class WorkoutLogKindsTests : BunitContext
{
    private const string Id = "w-kind";

    /// <summary>A Workout in progress with one Exercise of the given kind, one planned Set.</summary>
    private async Task<AppData> SeedAsync(LogKind kind)
    {
        var plan = Plan([
            new("s", "Kinds", DayOfWeek.Monday, SessionType.Strength, null, [new SessionExercise("x", 1, null, null, null)]),
        ]);
        var workout = WorkoutStart.Create(plan, plan.Sessions[0], Today, new CountingIds(), new FixedClock(Now)) with { Id = Id };
        var data = await AppData.OpenAsync(new InMemoryDataStore());
        List<Change> changes = [Change.Put(new Exercise("x", "The exercise", kind, null, false)), Change.Put(plan), Change.Put(workout)];
        Assert.IsType<CommitResult.Committed>(await data.CommitAsync(changes));
        return data;
    }

    private async Task<(IRenderedComponent<WorkoutLog> Cut, AppData Data)> OpenAsync(LogKind kind)
    {
        var data = await SeedAsync(kind);
        Wire(this, data);
        return (Render<WorkoutLog>(p => p.Add(l => l.WorkoutId, Id)), data);
    }

    private static IElement Field(IRenderedComponent<WorkoutLog> cut, string label) => cut.Find($"input[aria-label='{label}']");

    private static void Type(IRenderedComponent<WorkoutLog> cut, string label, string text) => Field(cut, label).Input(text);

    private static void Confirm(IRenderedComponent<WorkoutLog> cut) => cut.Find(".fa-confirmbar .fa-btn--primary").Click();

    private static WorkoutSet OnlySet(AppData data) =>
        data.Snapshot.Workouts.Single(w => w.Id == Id).Entries.Single(e => e.ExerciseId == "x").Sets.Single();

    [Fact]
    public async Task Bodyweight_reps_default_to_the_bw_tag_and_store_no_added_weight()
    {
        var (cut, data) = await OpenAsync(LogKind.BodyweightLoad);
        Type(cut, "Added weight, Set 1", "bw");
        Type(cut, "Reps, Set 1", "12");
        Confirm(cut);

        Assert.Equal(new WorkoutSet("id-1", null, 12, null, null, null, false), OnlySet(data) with { Id = "id-1" });
    }

    [Fact]
    public async Task Bodyweight_reps_with_added_weight_store_the_weight_in_kg()
    {
        var (cut, data) = await OpenAsync(LogKind.BodyweightLoad);
        Type(cut, "Added weight, Set 1", "10");
        Type(cut, "Reps, Set 1", "8");
        Confirm(cut);

        var set = OnlySet(data);
        Assert.Equal(10, set.WeightKg);
        Assert.Equal(8, set.Reps);
    }

    [Fact]
    public async Task Carry_takes_weight_per_hand_and_metres()
    {
        var (cut, data) = await OpenAsync(LogKind.Carry);
        Assert.Equal("decimal", Field(cut, "Weight per hand, Set 1").GetAttribute("inputmode"));
        Assert.Equal("numeric", Field(cut, "Metres, Set 1").GetAttribute("inputmode"));
        Type(cut, "Weight per hand, Set 1", "24");
        Type(cut, "Metres, Set 1", "40");
        Confirm(cut);

        var set = OnlySet(data);
        Assert.Equal(24, set.WeightKg);
        Assert.Equal(40, set.DistanceM);
    }

    [Fact]
    public async Task Seconds_is_one_wide_numeric_input_with_steppers()
    {
        var (cut, data) = await OpenAsync(LogKind.Seconds);
        Assert.Equal("numeric", Field(cut, "Seconds, Set 1").GetAttribute("inputmode"));
        Assert.Empty(cut.FindAll("input[aria-label='Reps, Set 1']"));
        Assert.Equal("Increase by 5 s", cut.Find(".fa-confirmbar button[aria-label^='Increase']").GetAttribute("aria-label"));
        Type(cut, "Seconds, Set 1", "45");
        Confirm(cut);

        Assert.Equal(45, OnlySet(data).DurationSeconds);
    }

    [Fact]
    public async Task Minutes_store_seconds()
    {
        var (cut, data) = await OpenAsync(LogKind.Minutes);
        Assert.Empty(cut.FindAll(".fa-confirmbar button[aria-label^='Increase']"));
        Type(cut, "Minutes, Set 1", "12.5");
        Confirm(cut);

        Assert.Equal(750, OnlySet(data).DurationSeconds);
    }

    [Fact]
    public async Task Reps_only_is_one_wide_numeric_input()
    {
        var (cut, data) = await OpenAsync(LogKind.Reps);
        Assert.Equal("numeric", Field(cut, "Reps, Set 1").GetAttribute("inputmode"));
        Type(cut, "Reps, Set 1", "15");
        Confirm(cut);

        Assert.Equal(15, OnlySet(data).Reps);
    }

    [Theory]
    [InlineData("1:48", 108)]
    [InlineData("108", 108)]
    [InlineData("1:04:10", 3850)]
    public async Task Time_accepts_clock_text_or_plain_seconds_and_has_no_steppers(string typed, int seconds)
    {
        var (cut, data) = await OpenAsync(LogKind.Time);
        Assert.Equal("text", Field(cut, "Time, Set 1").GetAttribute("inputmode"));
        Assert.Empty(cut.FindAll(".fa-confirmbar button[aria-label^='Increase']"));
        Type(cut, "Time, Set 1", typed);
        Confirm(cut);

        Assert.Equal(seconds, OnlySet(data).DurationSeconds);
    }

    [Fact]
    public async Task Distance_in_km_is_a_decimal_input_stored_in_km()
    {
        var (cut, data) = await OpenAsync(LogKind.DistanceKm);
        Assert.Equal("decimal", Field(cut, "Distance, Set 1").GetAttribute("inputmode"));
        Type(cut, "Distance, Set 1", "5,5");
        Confirm(cut);

        Assert.Equal(5.5, OnlySet(data).DistanceKm);
    }

    [Fact]
    public async Task Distance_in_metres_is_stored_in_metres()
    {
        var (cut, data) = await OpenAsync(LogKind.DistanceM);
        Type(cut, "Distance, Set 1", "500");
        Confirm(cut);

        Assert.Equal(500, OnlySet(data).DistanceM);
    }

    [Fact]
    public async Task Tick_only_is_one_big_toggle_that_stores_an_empty_set()
    {
        var (cut, data) = await OpenAsync(LogKind.TickOnly);
        Assert.Empty(cut.FindAll(".fa-setrow input"));
        cut.Find(".fa-setrow button[aria-label='Mark Set 1 done']").Click();

        Assert.Equal(new WorkoutSet("", null, null, null, null, null, false), OnlySet(data) with { Id = "" });
        Assert.Single(cut.FindAll(".fa-setrow__state--done"));
    }

    [Fact]
    public async Task Body_weight_is_one_wide_decimal_input_stored_in_kg()
    {
        var (cut, data) = await OpenAsync(LogKind.BodyMeasurementKg);
        Assert.Equal("decimal", Field(cut, "Body weight, Set 1").GetAttribute("inputmode"));
        Type(cut, "Body weight, Set 1", "82.5");
        Confirm(cut);

        Assert.Equal(82.5, OnlySet(data).WeightKg);
    }

    [Fact]
    public async Task A_body_measurement_in_cm_is_stored_in_cm()
    {
        var (cut, data) = await OpenAsync(LogKind.BodyMeasurementCm);
        Type(cut, "Measurement, Set 1", "92");
        Confirm(cut);

        Assert.Equal(92, OnlySet(data).MeasurementCm);
    }

    [Fact]
    public async Task A_bad_value_in_a_wide_row_says_what_was_expected_and_marks_the_field()
    {
        var (cut, data) = await OpenAsync(LogKind.Seconds);
        Type(cut, "Seconds, Set 1", "abc");
        Confirm(cut);

        Assert.Equal("true", Field(cut, "Seconds, Set 1").GetAttribute("aria-invalid"));
        Assert.NotEmpty(cut.Find(".fa-setrow__error").TextContent);
        Assert.Empty(data.Snapshot.Workouts.Single(w => w.Id == Id).Entries);
    }

    [Fact]
    public async Task A_text_Exercise_says_it_cannot_be_logged_yet_and_has_no_inputs()
    {
        var (cut, _) = await OpenAsync(LogKind.Text);

        Assert.Contains("cannot be logged", cut.Find(".log__notice").TextContent);
        Assert.Empty(cut.FindAll("input"));
    }

    [Fact]
    public async Task An_Exercise_used_by_a_Workout_cannot_be_deleted_so_the_log_never_loses_one()
    {
        // The page still has a "no longer in your library" note for a missing Exercise (damaged data), but the
        // data layer refuses the deletion that would cause it, so it cannot be reached through the app.
        var data = await SeedAsync(LogKind.Reps);

        var deleted = await data.CommitAsync(Change.Delete(RecordKind.Exercise, "x"));

        Assert.IsType<CommitResult.Rejected>(deleted);
        Wire(this, data);
        Assert.Empty(Render<WorkoutLog>(p => p.Add(l => l.WorkoutId, Id)).FindAll(".log__notice"));
    }
}
