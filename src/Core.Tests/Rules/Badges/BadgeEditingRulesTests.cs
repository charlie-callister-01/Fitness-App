using Core.Exchange;
using Core.Ids;
using Core.Model;
using Core.Rules.Badges;
using Core.Rules.Units;
using Core.Storage;
using Core.Tests.Storage;
using Core.Tests.Exchange;
using Core.Tests.Rules.TestData;
using static Core.Tests.Rules.TestData.Dates;
using static Core.Tests.Rules.TestData.HistoryBuilder;

namespace Core.Tests.Rules.Badges;

public class BadgeEditingRulesTests
{
    private static readonly DateOnly Start = D(2026, 1, 5);
    private static readonly UnitConverter Kg = new(WeightUnit.Kg, DistanceUnit.Km, 2);
    private static readonly UnitConverter Lb = new(WeightUnit.Lb, DistanceUnit.Mi, 2);
    private static readonly Exercise Squat = new("squat", "Squat", LogKind.Load, null, false);
    private static readonly Exercise Plank = new("plank", "Plank", LogKind.Seconds, null, false);
    private static readonly Exercise Talk = new("talk", "Journal", LogKind.Text, null, false);
    private static readonly IReadOnlyList<Exercise> Library = [Squat, Plank, Talk];

    private sealed class Ids : IIdGenerator
    {
        public string NewId() => "new-id";
    }

    private static BadgeDraft Draft(BadgeMetricChoice metric = BadgeMetricChoice.Workouts, string name = "Ten", string target = "10",
        string? exercise = null, string reps = "", bool drop = false) =>
        new(name, metric, exercise, target, reps, drop);

    private static string Reason(BadgeDraft d, UnitConverter? units = null)
    {
        var result = BadgeEditingRules.Validate(d, "id", Library, units ?? Kg);
        Assert.False(result.IsValid);
        return result.Message!;
    }

    private static Badge Valid(BadgeDraft d, UnitConverter? units = null)
    {
        Assert.True(BadgeEditingRules.Validate(d, "id", Library, units ?? Kg).TryGet(out var badge, out var message), message);
        return badge;
    }

    [Theory]
    [InlineData(BadgeMetricChoice.Workouts, "sessions")]
    [InlineData(BadgeMetricChoice.Streak, "streak")]
    [InlineData(BadgeMetricChoice.BestWeek, "bestweek")]
    [InlineData(BadgeMetricChoice.PrCount, "prs")]
    [InlineData(BadgeMetricChoice.BodyWeight, "bodyweight")]
    public void Each_metric_choice_maps_to_the_supported_metric_name(BadgeMetricChoice choice, string metric)
    {
        var badge = Valid(Draft(choice, target: "5"));

        Assert.Equal(metric, badge.Metric);
        Assert.Equal(5, badge.Target);
        Assert.Equal("id", badge.Id);
        Assert.Equal("Ten", badge.Name);
    }

    [Fact]
    public void Exercise_best_uses_the_Exercise_id_as_the_metric()
    {
        var badge = Valid(Draft(BadgeMetricChoice.ExerciseBest, exercise: "plank", target: "120"));

        Assert.Equal("plank", badge.Metric);
        Assert.Equal(120, badge.Target);
    }

    [Fact]
    public void A_Load_Exercise_target_is_weight_times_reps_stored_as_the_estimated_one_rep_max()
    {
        var badge = Valid(Draft(BadgeMetricChoice.ExerciseBest, exercise: "squat", target: "24", reps: "8"));

        Assert.Equal(24 * (1 + 8 / 30.0), badge.Target, 6);
    }

    [Fact]
    public void A_Load_Exercise_weight_is_read_in_the_owners_units()
    {
        var badge = Valid(Draft(BadgeMetricChoice.ExerciseBest, exercise: "squat", target: "100", reps: "10"), Lb);

        Assert.Equal(100 * 0.45359237 * (1 + 10 / 30.0), badge.Target, 6);
    }

    [Fact]
    public void A_weight_target_is_read_in_the_owners_units_and_stored_in_kg()
    {
        Assert.Equal(45.359237, Valid(Draft(BadgeMetricChoice.BodyWeight, target: "100"), Lb).Target, 6);
    }

    [Fact]
    public void Drop_by_stores_a_negative_target_for_body_weight()
    {
        Assert.Equal(-5, Valid(Draft(BadgeMetricChoice.BodyWeight, target: "5", drop: true)).Target);
    }

    [Fact]
    public void Drop_by_is_ignored_for_other_metrics()
    {
        Assert.Equal(5, Valid(Draft(BadgeMetricChoice.Workouts, target: "5", drop: true)).Target);
    }

    [Fact]
    public void The_name_is_trimmed()
    {
        Assert.Equal("Ten", Valid(Draft(name: "  Ten ")).Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_name_is_refused(string name) =>
        Assert.Equal("Give the Badge a name.", Reason(Draft(name: name)));

    [Fact]
    public void An_undefined_metric_is_refused() =>
        Assert.Equal("Choose what the Badge counts.", Reason(Draft((BadgeMetricChoice)99)));

    [Fact]
    public void Exercise_best_needs_an_Exercise() =>
        Assert.Equal("Choose an Exercise for this Badge.", Reason(Draft(BadgeMetricChoice.ExerciseBest, exercise: null)));

    [Fact]
    public void Exercise_best_refuses_an_Exercise_that_is_not_in_the_library() =>
        Assert.Equal("That Exercise is not in your library.", Reason(Draft(BadgeMetricChoice.ExerciseBest, exercise: "ghost", target: "5")));

    [Fact]
    public void Exercise_best_refuses_an_Exercise_that_cannot_be_measured() =>
        Assert.Equal("Journal cannot be measured, so it cannot have a Badge.", Reason(Draft(BadgeMetricChoice.ExerciseBest, exercise: "talk", target: "5")));

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-3")]
    public void A_target_must_be_a_number_above_zero(string target) =>
        Assert.Equal("Enter a target above zero.", Reason(Draft(target: target)));

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("2.5")]
    [InlineData("x")]
    public void A_Load_Exercise_needs_whole_reps_above_zero(string reps) =>
        Assert.Equal("Enter the reps as a whole number above zero.",
            Reason(Draft(BadgeMetricChoice.ExerciseBest, exercise: "squat", target: "24", reps: reps)));

    [Fact]
    public void A_decimal_comma_is_understood()
    {
        Assert.Equal(2.5, Valid(Draft(target: "2,5")).Target);
    }

    [Fact]
    public void Add_gives_the_Badge_a_new_permanent_id()
    {
        var added = BadgeEditingRules.Add(Draft(), Library, Kg, new Ids());

        Assert.True(added.TryGet(out var badge, out _));
        Assert.Equal(new Badge("new-id", "Ten", "sessions", 10), badge);
    }

    [Fact]
    public void Add_passes_a_refusal_through()
    {
        Assert.Equal("Give the Badge a name.", BadgeEditingRules.Add(Draft(name: ""), Library, Kg, new Ids()).Message);
    }

    [Fact]
    public void Replace_keeps_the_id_and_changes_the_rest()
    {
        var existing = new[] { new Badge("b1", "Ten", "sessions", 10), new Badge("b2", "Other", "prs", 3) };

        var result = BadgeEditingRules.Replace(existing, "b2", Draft(BadgeMetricChoice.Streak, "Long", "6"), Library, Kg);

        Assert.True(result.TryGet(out var badge, out _));
        Assert.Equal(new Badge("b2", "Long", "streak", 6), badge);
    }

    [Fact]
    public void Replace_says_so_when_the_Badge_is_gone() =>
        Assert.Equal("That Badge is no longer there.", BadgeEditingRules.Replace([], "b1", Draft(), Library, Kg).Message);

    [Fact]
    public void Replace_refuses_an_invalid_draft_and_changes_nothing()
    {
        var existing = new[] { new Badge("b1", "Ten", "sessions", 10) };

        Assert.Equal("Give the Badge a name.", BadgeEditingRules.Replace(existing, "b1", Draft(name: ""), Library, Kg).Message);
    }

    [Fact]
    public void Delete_returns_the_Badge_to_remove_or_says_it_is_gone()
    {
        var existing = new[] { new Badge("b1", "Ten", "sessions", 10) };

        Assert.True(BadgeEditingRules.Delete(existing, "b1").TryGet(out var gone, out _));
        Assert.Equal("b1", gone.Id);
        Assert.Equal("That Badge is no longer there.", BadgeEditingRules.Delete(existing, "zzz").Message);
    }

    [Theory]
    [InlineData("sessions", 10, BadgeMetricChoice.Workouts, "10", false)]
    [InlineData("streak", 4, BadgeMetricChoice.Streak, "4", false)]
    [InlineData("bestweek", 4, BadgeMetricChoice.BestWeek, "4", false)]
    [InlineData("best week", 4, BadgeMetricChoice.BestWeek, "4", false)]
    [InlineData("prs", 25, BadgeMetricChoice.PrCount, "25", false)]
    [InlineData("bodyweight", 80, BadgeMetricChoice.BodyWeight, "80", false)]
    [InlineData("bodyweight", -5, BadgeMetricChoice.BodyWeight, "5", true)]
    public void A_stored_Badge_opens_as_the_draft_that_made_it(string metric, double target, BadgeMetricChoice choice, string text, bool drop)
    {
        var draft = BadgeEditingRules.DraftFor(new Badge("b", "Name", metric, target), Library, Kg);

        Assert.Equal(new BadgeDraft("Name", choice, null, text, "", drop), draft);
    }

    [Fact]
    public void A_stored_Exercise_Badge_opens_on_its_Exercise()
    {
        var draft = BadgeEditingRules.DraftFor(new Badge("b", "Plank", "plank", 120), Library, Kg);

        Assert.Equal(new BadgeDraft("Plank", BadgeMetricChoice.ExerciseBest, "plank", "120", "", false), draft);
    }

    [Fact]
    public void A_stored_Load_Badge_opens_as_one_rep_and_keeps_its_target_when_saved_unchanged()
    {
        var stored = new Badge("b", "Squat", "squat", 24 * (1 + 8 / 30.0));

        var draft = BadgeEditingRules.DraftFor(stored, Library, Kg);
        var saved = Valid(draft);

        Assert.Equal("1", draft.Reps);
        Assert.Equal(stored.Target, saved.Target, 2);
    }

    [Theory]
    [InlineData(24 * (1 + 8 / 30.0), false)]
    [InlineData(1 + 7 / 30.0, false)]
    [InlineData(137.123456789, true)]
    public void Replacing_a_Badge_with_its_own_unchanged_draft_keeps_the_target_exactly(double target, bool inLb)
    {
        var units = inLb ? Lb : Kg;
        var stored = new Badge("b", "Squat", "squat", target);
        var draft = BadgeEditingRules.DraftFor(stored, Library, units);

        Assert.True(BadgeEditingRules.Replace([stored], "b", draft with { Name = "Renamed" }, Library, units).TryGet(out var saved, out _));

        Assert.Equal(target, saved.Target);
        Assert.Equal("Renamed", saved.Name);
    }

    [Fact]
    public void Changing_the_reps_of_an_opened_Load_Badge_changes_the_target()
    {
        var stored = new Badge("b", "Squat", "squat", 24 * (1 + 8 / 30.0));
        var draft = BadgeEditingRules.DraftFor(stored, Library, Kg) with { Reps = "5" };

        Assert.True(BadgeEditingRules.Replace([stored], "b", draft, Library, Kg).TryGet(out var saved, out _));

        Assert.NotEqual(stored.Target, saved.Target);
    }

    [Fact]
    public void The_estimated_1RM_line_says_the_target_is_an_estimated_1RM_with_its_one_rep_weight()
    {
        var draft = Draft(BadgeMetricChoice.ExerciseBest, "Squat", "24", "squat", "8");

        var line = BadgeEditingText.OneRepMaxLine(draft, Library, Kg);

        Assert.Equal("This target is stored as an estimated 1RM of 30.4 kg (24 kg for 8 reps).", line);
    }

    [Fact]
    public void The_estimated_1RM_line_is_only_for_a_Load_Exercise_with_a_complete_draft()
    {
        Assert.Null(BadgeEditingText.OneRepMaxLine(Draft(BadgeMetricChoice.ExerciseBest, "Plank", "60", "plank"), Library, Kg));
        Assert.Null(BadgeEditingText.OneRepMaxLine(Draft(BadgeMetricChoice.Workouts), Library, Kg));
        Assert.Null(BadgeEditingText.OneRepMaxLine(Draft(BadgeMetricChoice.ExerciseBest, "Squat", "", "squat", "8"), Library, Kg));
    }

    [Fact]
    public void A_stored_Badge_with_an_unknown_metric_opens_with_no_Exercise_chosen()
    {
        var draft = BadgeEditingRules.DraftFor(new Badge("b", "Odd", "nonsense", 3), Library, Kg);

        Assert.Equal(BadgeMetricChoice.ExerciseBest, draft.Metric);
        Assert.Null(draft.ExerciseId);
    }

    // ---- live line

    private static BadgeHistory History(IReadOnlyList<Workout>? workouts = null, IReadOnlyList<WeighIn>? weighIns = null) =>
        new(PlanFixture.Make(start: Start, sessions: [PlanFixture.Session("lower", DayOfWeek.Monday, name: "Lower")]),
            Library, workouts ?? [], [], weighIns ?? []);

    private static string Line(BadgeDraft d, BadgeHistory h, UnitConverter? units = null) =>
        BadgeEditingText.LiveLine(d, h, D(2026, 2, 1), units ?? Kg);

    [Fact]
    public void The_live_line_for_a_count_says_where_it_stands_and_what_unlocks_it()
    {
        var h = History([Lift("w1", Start, "lower", "squat", Set(kg: 50, reps: 5)), Lift("w2", Start.AddDays(7), "lower", "squat", Set(kg: 50, reps: 5))]);

        Assert.Equal("Now: 2. Unlocks when: complete 10 Workouts.", Line(Draft(), h));
    }

    [Fact]
    public void The_live_line_for_a_Load_Exercise_shows_the_best_set_and_the_target_set()
    {
        var h = History([Lift("w1", Start, "lower", "squat", Set(kg: 22, reps: 9))]);

        var line = Line(Draft(BadgeMetricChoice.ExerciseBest, exercise: "squat", target: "24", reps: "8"), h);

        Assert.Equal("Now: 22 × 9. Unlocks when: you lift 24 × 8 or better.", line);
    }

    [Fact]
    public void The_live_line_with_no_result_yet_says_so()
    {
        var line = Line(Draft(BadgeMetricChoice.ExerciseBest, exercise: "squat", target: "24", reps: "8"), History());

        Assert.Equal("Now: nothing logged yet. Unlocks when: you lift 24 × 8 or better.", line);
    }

    [Fact]
    public void The_live_line_for_body_weight_uses_the_owners_units()
    {
        var weighIns = new[] { new WeighIn("x", Start, 100) };

        var line = Line(Draft(BadgeMetricChoice.BodyWeight, target: "90"), History(weighIns: weighIns));

        Assert.Equal("Now: 100. Unlocks when: reach 90 kg.", line);
    }

    [Fact]
    public void The_live_line_for_a_drop_says_drop()
    {
        var weighIns = new[] { new WeighIn("x", Start, 100), new WeighIn("y", Start.AddDays(7), 98) };

        var line = Line(Draft(BadgeMetricChoice.BodyWeight, target: "5", drop: true), History(weighIns: weighIns));

        Assert.Equal("Now: 2. Unlocks when: drop 5 kg from your first Weigh-in.", line);
    }

    [Fact]
    public void The_live_line_waits_for_a_complete_draft()
    {
        Assert.Equal("Fill in the target to see where you stand.", Line(Draft(target: ""), History()));
        Assert.Equal("Fill in the target to see where you stand.", Line(Draft(BadgeMetricChoice.ExerciseBest, exercise: null), History()));
    }

    [Fact]
    public void An_Exercise_without_a_chart_value_is_not_measurable_in_the_live_line()
    {
        Assert.Equal("Journal cannot be measured, so it cannot have a Badge.",
            Line(Draft(BadgeMetricChoice.ExerciseBest, exercise: "talk", target: "5"), History()));
    }

    // ---- export round trip

    [Fact]
    public async Task An_edited_Badge_survives_export_and_import()
    {
        var edited = Valid(Draft(BadgeMetricChoice.PrCount, "Fifty PRs", "50"));
        var snapshot = ExchangeTestData.Snapshot() with { Badges = [edited] };

        var export = Exporter.Export(snapshot, ExportRequest.Backup, "fitness-app 1.2", ExchangeTestData.Now);
        var document = ExchangeTestData.ReadBack(export.Text);

        Assert.Equal([edited], document.Badges);

        // The import half: restoring that Backup into an empty app gives the same Badge back.
        var app = await AppData.OpenAsync(new InMemoryDataStore());
        var inspected = ImportInput.Inspect(export.Text);
        Assert.True(inspected.IsAccepted);
        var preview = ImportPreviewer.Preview(app, inspected.Inspected!, ImportKind.Restore, ImportDecisions.None, ExchangeTestData.Now);
        Assert.IsType<ApplyOutcome.Applied>(await ImportApplier.ApplyAsync(app, preview, new RecordingBackupSink(), ExchangeTestData.Now));
        Assert.Equal([edited], app.Snapshot.Badges);
    }
}
