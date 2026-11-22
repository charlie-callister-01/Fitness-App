using Core.Exchange;
using Core.Model;
using Core.Serialisation;
using Core.Storage;
using Core.Tests.Rules.TestData;

namespace Core.Tests.Exchange;

public class MergePreviewTests
{
    private static readonly DateOnly Day = new(2026, 11, 2);

    private static Exercise Ex(string id, string name) => new(id, name, LogKind.Load, null, false);

    private static readonly Exercise Squat = Ex("squat", "Back squat");

    private static Workout MakeWorkout(string id, DateOnly date, string? sessionId = null, double weightKg = 60) =>
        new(id, date, sessionId, true, null, null, [],
            [new WorkoutEntry("squat", EntryStatus.Done, null, null, null, [new WorkoutSet($"set-{id}", weightKg, 5, null, null, null, false)])], null);

    private static Plan MakePlan(string id, bool active, params string[] sessionIds) =>
        PlanFixture.Make(id: id, name: $"Plan {id}", isActive: active, start: new DateOnly(2026, 12, 1), incrementKg: 2.5,
            phases: [new Phase($"ph-{id}", "Base", 1, 4, [], [])],
            sessions: [.. sessionIds.Select(s => PlanFixture.Session(s, DefaultDay.Monday, SessionType.Strength, $"Session {s}", exercises: [new SessionExercise("squat", 3, 5, null, null)], trackId: $"{id}-main"))]);

    private static ExchangeDocument Doc() => new(ExchangeFormat.CurrentVersion, null, null, null, null);

    private static Inspected Incoming(ExchangeDocument document)
    {
        var result = ImportInput.Inspect(ExchangeFormat.Write(document));
        Assert.True(result.IsAccepted, string.Join("; ", result.Rejections));
        return result.Inspected!;
    }

    private static DataSnapshot Library() => DataSnapshot.Empty with { Exercises = [Squat] };

    private static ImportPreview Merge(DataSnapshot library, ExchangeDocument document, ImportDecisions? decisions = null) =>
        ImportPreviewer.Preview(library, Incoming(document), ImportKind.Merge, decisions ?? ImportDecisions.None);

    private static DataSnapshot Applied(ImportPreview preview) => preview.Changes.Aggregate(preview.Source, (s, c) => c.ApplyTo(s));

    [Fact]
    public void Records_the_library_does_not_have_are_added_by_key()
    {
        var document = Doc() with
        {
            WeighIns = [new WeighIn("wi1", Day, 80)],
            Breaks = [new Break("br1", Day, Day.AddDays(6), "Flu")],
            Badges = [new Badge("b1", "Ten Workouts", "workouts", 10)],
            Workouts = [MakeWorkout("w1", Day)],
            WeekSchedules = [new WeekSchedule(Day, [])],
        };

        var preview = Merge(Library(), document);

        Assert.True(preview.IsReady);
        Assert.Equal(5, preview.Changes.Count);
        Assert.Equal(5, preview.Items.Count(i => i.Action == ImportItemAction.Add));
        Assert.Equal(["Adds 1 Weigh-in.", "Adds 1 Workout.", "Adds 1 Break.", "Adds 1 Badge.", "Adds 1 Week schedule."], preview.Summary);
        var after = Applied(preview);
        Assert.Equal(["wi1"], after.WeighIns.Select(w => w.Id));
        Assert.Equal(["w1"], after.Workouts.Select(w => w.Id));
    }

    [Fact]
    public void Identical_records_have_nothing_to_change()
    {
        var document = Doc() with { WeighIns = [new WeighIn("wi1", Day, 80)], Workouts = [MakeWorkout("w1", Day)] };
        var library = Applied(Merge(Library(), document));

        var again = Merge(library, document);

        Assert.True(again.IsReady);
        Assert.Empty(again.Changes);
        Assert.Equal(["Nothing to change."], again.Summary);
    }

    [Fact]
    public void A_differing_record_replaces_the_one_with_that_key_and_nothing_is_deleted()
    {
        var library = Library() with
        {
            WeighIns = [new WeighIn("wi1", Day, 80), new WeighIn("wi2", Day.AddDays(1), 81)],
            Badges = [new Badge("b1", "Ten Workouts", "workouts", 10)],
        };
        var document = Doc() with { WeighIns = [new WeighIn("wi1", Day, 79.5)], Badges = [new Badge("b1", "Ten Workouts", "workouts", 10)] };

        var preview = Merge(library, document);

        Assert.True(preview.IsReady);
        var change = Assert.Single(preview.Items, i => i.Action == ImportItemAction.Change);
        Assert.Equal((RecordKind.WeighIn, "wi1"), (change.Kind, change.Key));
        Assert.Equal(["Changes 1 Weigh-in.", "Touches no Workouts."], preview.Summary);
        Assert.All(preview.Changes, c => Assert.IsNotType<StoreChange.Delete>(c.ToStoreChange()));
        var after = Applied(preview);
        Assert.Equal([79.5, 81], after.WeighIns.Select(w => w.WeightKg));
        Assert.Single(after.Badges);
    }

    [Fact]
    public void A_differing_workout_is_kept_until_the_owner_chooses_to_replace_it()
    {
        var library = Library() with { Workouts = [MakeWorkout("w1", Day, weightKg: 60)] };
        var document = Doc() with { Workouts = [MakeWorkout("w1", Day, weightKg: 65)] };

        var kept = Merge(library, document);

        Assert.True(kept.IsReady);
        Assert.Empty(kept.Changes);
        Assert.Contains(kept.Items, i => i is { Action: ImportItemAction.Keep, Kind: RecordKind.Workout, Key: "w1", OffersReplace: true });
        Assert.Equal(["Nothing to change.", "Keeps 1 existing Workout. Choose to replace it to use the file's version."], kept.Summary);

        var replaced = Merge(library, document, ImportDecisions.None.WithWorkout("w1", KeepOrReplace.Replace));

        Assert.Contains(replaced.Items, i => i is { Action: ImportItemAction.Change, Kind: RecordKind.Workout, Key: "w1" });
        Assert.Equal(["Changes 1 Workout."], replaced.Summary);
        Assert.Equal(65, Assert.Single(Applied(replaced).Workouts).Entries[0].Sets[0].WeightKg);
    }

    [Fact]
    public void A_merged_plan_keeps_the_existing_active_flag_and_a_new_plan_is_added_inactive()
    {
        var library = Library() with { Plans = [MakePlan("a", true, "s-a"), MakePlan("c", false, "s-c")] };
        var document = Doc() with
        {
            Plans = [MakePlan("a", false, "s-a") with { Name = "Renamed" }, MakePlan("b", false, "s-b"), MakePlan("c", true, "s-c")],
        };

        var preview = Merge(library, document);

        Assert.True(preview.IsReady);
        Assert.Equal(["Adds 1 Plan.", "Changes 1 Plan.", "Touches no Workouts."], preview.Summary);
        var plans = Applied(preview).Plans.ToDictionary(p => p.Id);
        Assert.True(plans["a"].IsActive);
        Assert.Equal("Renamed", plans["a"].Name);
        Assert.False(plans["b"].IsActive);
        Assert.False(plans["c"].IsActive);
        Assert.DoesNotContain(preview.Items, i => i.Key == "c");
    }

    [Fact]
    public void A_workout_on_the_same_date_and_session_as_one_you_have_under_another_id_adds_a_warning()
    {
        var library = Library() with { Plans = [MakePlan("a", true, "s-a")], Workouts = [MakeWorkout("w1", Day, "s-a")] };
        var document = Doc() with
        {
            Workouts = [MakeWorkout("w2", Day, "s-a"), MakeWorkout("w3", Day, null), MakeWorkout("w4", Day.AddDays(1), "s-a")],
        };

        var preview = Merge(library, document);

        Assert.True(preview.IsReady);
        var warning = Assert.Single(preview.Items, i => i.Action == ImportItemAction.Warning);
        Assert.Equal((RecordKind.Workout, "w2"), (warning.Kind, warning.Key));
        Assert.Equal("The file's Workout w2 is on 2026-11-02 for the same Session as your Workout w1. Both will be kept.", warning.Text);
        Assert.Equal(4, Applied(preview).Workouts.Count);
    }

    [Fact]
    public void Settings_and_guides_in_the_file_are_ignored_and_listed()
    {
        var document = Doc() with
        {
            Settings = Settings.Default with { StreakMinWorkoutsPerWeek = 5 },
            Guides = [new Guide("squat", "Quads", null, null, null, [], [], null, null, [], null, null)],
            WeighIns = [new WeighIn("wi1", Day, 80)],
        };

        var preview = Merge(Library(), document);

        Assert.True(preview.IsReady);
        Assert.Equal([RecordKind.Settings, RecordKind.Guide], preview.Items.Where(i => i.Action == ImportItemAction.Ignore).Select(i => i.Kind!.Value).Order());
        Assert.Equal(Settings.Default, Applied(preview).Settings);
        Assert.Empty(Applied(preview).Guides);
    }

    [Fact]
    public void A_merged_plan_that_drops_a_session_a_workout_uses_is_a_blocker()
    {
        var library = Library() with { Plans = [MakePlan("a", true, "s-a", "s-b")], Workouts = [MakeWorkout("w1", Day, "s-b")] };
        var document = Doc() with { Plans = [MakePlan("a", true, "s-a")] };

        var preview = Merge(library, document);

        Assert.False(preview.IsReady);
        var blocker = Assert.Single(preview.Blockers);
        Assert.Equal((RecordKind.Workout, "workouts[0].sessionId"), (blocker.Kind, blocker.Key));
        Assert.Contains("is not a Session in any Plan", blocker.Text);
    }

    [Fact]
    public void A_merge_that_adds_no_workouts_says_it_touches_none()
    {
        var preview = Merge(Library(), Doc() with { WeighIns = [new WeighIn("wi1", Day, 80)] });

        Assert.Equal(["Adds 1 Weigh-in.", "Touches no Workouts."], preview.Summary);
    }
}
