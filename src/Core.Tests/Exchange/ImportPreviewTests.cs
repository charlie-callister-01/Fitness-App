using Core.Exchange;
using Core.Model;
using Core.Serialisation;
using Core.Storage;

namespace Core.Tests.Exchange;

public class ImportPreviewTests
{
    private static Exercise Ex(string id, string name) => new(id, name, LogKind.Load, null, false);

    private static Session Sess(string id, string exerciseId = "squat") =>
        new(id, $"Session {id}", DayOfWeek.Monday, SessionType.Strength, null, [new SessionExercise(exerciseId, 3, 5, null, null)]);

    private static Phase Ph(string id, params SessionOverride[] overrides) => new(id, "Base", 1, 4, [], overrides);

    private static Plan MakePlan(string id, string name, bool active, string sessionId, string phaseId, string exerciseId = "squat", params SessionOverride[] overrides) =>
        new(id, name, active, new DateOnly(2026, 12, 1), null, null, 3, 2, false, 2.5, [Ph(phaseId, overrides)], [Sess(sessionId, exerciseId)]);

    private static ExchangeDocument Doc(IReadOnlyList<Exercise>? exercises = null, IReadOnlyList<Plan>? plans = null, IReadOnlyList<Guide>? guides = null) =>
        new(ExchangeFormat.CurrentVersion, null, null, exercises, null, Guides: guides, Plans: plans);

    private static Inspected Incoming(ExchangeDocument document)
    {
        var result = ImportInput.Inspect(ExchangeFormat.Write(document));
        Assert.True(result.IsAccepted, string.Join("; ", result.Rejections));
        return result.Inspected!;
    }

    private static DataSnapshot Library(IReadOnlyList<Plan>? plans = null, IReadOnlyList<Guide>? guides = null, params Exercise[] exercises) =>
        DataSnapshot.Empty with { Exercises = exercises, Plans = plans ?? [], Guides = guides ?? [] };

    private static ImportPreview Preview(DataSnapshot library, ExchangeDocument document, ImportKind kind, ImportDecisions? decisions = null) =>
        ImportPreviewer.Preview(library, Incoming(document), kind, decisions ?? ImportDecisions.None);

    /// <summary>What the library would hold after the preview's Change list is committed.</summary>
    private static DataSnapshot Applied(ImportPreview preview) => preview.Changes.Aggregate(preview.Source, (s, c) => c.ApplyTo(s));

    private static readonly Exercise Squat = Ex("squat", "Back squat");

    [Fact]
    public void A_plan_is_added_as_a_new_inactive_plan_when_the_library_has_one()
    {
        var library = Library([MakePlan("current", "Current", true, "s1", "ph1")], null, Squat);

        var preview = Preview(library, Doc(plans: [MakePlan("hyrox", "Hyrox block", true, "s9", "ph9")]), ImportKind.Plan);

        Assert.True(preview.IsReady);
        var change = Assert.Single(preview.Changes);
        Assert.Equal((RecordKind.Plan, "hyrox"), (change.Kind, change.Key));
        var item = Assert.Single(preview.Items, i => i.Action == ImportItemAction.Add);
        Assert.Equal((RecordKind.Plan, "hyrox"), (item.Kind, item.Key));
        Assert.Contains(preview.Summary, l => l == "Creates a new Plan called \"Hyrox block\", inactive.");
        Assert.Same(library, preview.Source);
    }

    [Fact]
    public void Colliding_plan_session_and_phase_ids_get_fresh_ids_and_overrides_follow()
    {
        var library = Library([MakePlan("current", "Current", true, "s1", "ph1")], null, Squat);
        var incoming = MakePlan("current", "Other block", true, "s1", "ph1", "squat", new SessionOverride("s1", [new SessionExercise("squat", 5, 3, null, null)]));

        var preview = Preview(library, Doc(plans: [incoming]), ImportKind.Plan);

        Assert.True(preview.IsReady);
        var added = Assert.Single(Applied(preview).Plans, p => p.Name == "Other block");
        Assert.Equal("current-2", added.Id);
        Assert.False(added.IsActive);
        Assert.Equal("s1-2", Assert.Single(added.Sessions).Id);
        var phase = Assert.Single(added.Phases);
        Assert.Equal("ph1-2", phase.Id);
        Assert.Equal("s1-2", Assert.Single(phase.SessionOverrides).SessionId);
        Assert.Single(Applied(preview).Plans, p => p.Id == "current");
    }

    [Fact]
    public void Importing_the_same_plan_file_again_has_nothing_to_change_even_after_fresh_ids()
    {
        var library = Library([MakePlan("current", "Current", true, "s1", "ph1")], null, Squat);
        var document = Doc(plans: [MakePlan("current", "Other block", true, "s1", "ph1")]);
        var afterFirst = Applied(Preview(library, document, ImportKind.Plan));

        var again = Preview(afterFirst, document, ImportKind.Plan);

        Assert.Empty(again.Changes);
        Assert.Equal(["Nothing to change."], again.Summary);
        Assert.True(again.IsReady);
        Assert.Contains(again.Items, i => i is { Action: ImportItemAction.Keep, Kind: RecordKind.Plan, Key: "current-2" });
    }

    [Fact]
    public void Importing_a_plan_that_is_already_there_unchanged_has_nothing_to_change_whichever_is_active()
    {
        var library = Library(exercises: Squat);
        var document = Doc(plans: [MakePlan("hyrox", "Hyrox block", true, "s9", "ph9")]);
        var afterFirst = Applied(Preview(library, document, ImportKind.Plan));

        var again = Preview(afterFirst, document, ImportKind.Plan);

        Assert.Empty(again.Changes);
        Assert.Equal(["Nothing to change."], again.Summary);
    }

    [Fact]
    public void Everything_a_plan_import_does_not_read_is_listed_as_ignored_and_its_problems_are_dropped()
    {
        Workout GhostWorkout(string id) => new(id, new DateOnly(2026, 11, 1), null, true, null, null, [],
            [new WorkoutEntry("ghost", EntryStatus.Done, null, null, null, [new WorkoutSet($"set-{id}", 60, 5, null, null, null, false)])], null);
        var document = Doc(plans: [MakePlan("hyrox", "Hyrox block", true, "s9", "ph9")]) with
        {
            Workouts = [GhostWorkout("w1"), GhostWorkout("w2")],
            WeighIns = [new WeighIn("wi1", new DateOnly(2026, 11, 1), 80)],
            Settings = Settings.Default,
        };

        var preview = Preview(Library(exercises: Squat), document, ImportKind.Plan);

        Assert.True(preview.IsReady);
        var ignored = preview.Items.Where(i => i.Action == ImportItemAction.Ignore).ToList();
        Assert.Equal([RecordKind.Settings, RecordKind.WeighIn, RecordKind.Workout], ignored.Select(i => i.Kind!.Value).Order());
        Assert.Contains(ignored, i => i.Kind == RecordKind.Workout && i.Text == "Ignored: 2 Workouts in the file. A Plan import does not use them.");
        Assert.DoesNotContain(preview.Items, i => i.Action == ImportItemAction.Blocker);
    }

    [Fact]
    public void A_plan_import_adds_new_exercises_and_changes_tags_in_the_same_commit()
    {
        var tagged = Squat with { IsBenchmark = true };
        var document = Doc(
            [tagged, Ex("sled", "Sled push"), Ex("wall-ball", "Wall ball")],
            [MakePlan("hyrox", "Hyrox block", true, "s9", "ph9", "sled")]);

        var preview = Preview(Library(exercises: Squat), document, ImportKind.Plan);

        Assert.True(preview.IsReady);
        Assert.Equal(["Adds 2 Exercises.", "Changes the tags or benchmark flag on 1 Exercise.", "Creates a new Plan called \"Hyrox block\" and makes it active, because you have no Plan yet.", "Touches no Workouts."], preview.Summary);
        var after = Applied(preview);
        Assert.Equal(["squat", "sled", "wall-ball"], after.Exercises.Select(e => e.Id));
        Assert.True(after.Exercises[0].IsBenchmark);
        Assert.Contains(preview.Items, i => i is { Action: ImportItemAction.Add, Kind: RecordKind.Exercise, Key: "sled" });
        Assert.Contains(preview.Items, i => i is { Action: ImportItemAction.Change, Kind: RecordKind.Exercise, Key: "squat" });
    }

    [Fact]
    public void A_plan_row_for_an_exercise_nowhere_is_a_blocker_at_its_path()
    {
        var preview = Preview(Library(), Doc(plans: [MakePlan("hyrox", "Hyrox block", true, "s9", "ph9", "ghost")]), ImportKind.Plan);

        Assert.False(preview.IsReady);
        var blocker = Assert.Single(preview.Blockers);
        Assert.Equal("plans[0].sessions[0].exercises[0].exerciseId", blocker.Key);
        Assert.Equal(RecordKind.Plan, blocker.Kind);
    }

    [Fact]
    public void An_unanswered_near_match_keeps_the_preview_not_ready_until_chosen()
    {
        var library = Library(exercises: Squat);
        var document = Doc([Ex("bsquat", "Back squats")], [MakePlan("hyrox", "Hyrox block", true, "s9", "ph9", "bsquat")]);

        var open = Preview(library, document, ImportKind.Plan);
        var chosen = Preview(library, document, ImportKind.Plan, ImportDecisions.None.WithExercise("bsquat", ExerciseChoice.Use("squat")));

        Assert.False(open.IsReady);
        Assert.Equal("bsquat", Assert.Single(open.OpenChoices).IncomingId);
        Assert.True(chosen.IsReady);
        Assert.Equal("squat", Applied(chosen).Plans.Single().Sessions.Single().Exercises.Single().ExerciseId);
    }

    [Fact]
    public void What_the_validator_would_reject_after_the_changes_is_a_plain_blocker_with_its_path()
    {
        var orphan = new Workout("w1", new DateOnly(2026, 11, 1), "gone", false, null, null, [], [], null);
        var library = Library(exercises: Squat) with { Workouts = [orphan] };

        var preview = Preview(library, Doc(plans: [MakePlan("hyrox", "Hyrox block", true, "s9", "ph9")]), ImportKind.Plan);

        Assert.False(preview.IsReady);
        var blocker = Assert.Single(preview.Blockers);
        Assert.Equal((RecordKind.Workout, "workouts[0].sessionId"), (blocker.Kind, blocker.Key));
        Assert.StartsWith("workouts[0].sessionId: ", blocker.Text);
    }

    [Theory]
    [InlineData(ImportKind.Merge)]
    public void A_kind_that_is_not_built_yet_says_so_and_is_never_ready(ImportKind kind)
    {
        var preview = Preview(Library(exercises: Squat), Doc(plans: [MakePlan("hyrox", "Hyrox block", true, "s9", "ph9")]), kind);

        Assert.False(preview.IsReady);
        Assert.Empty(preview.Changes);
        var blocker = Assert.Single(preview.Blockers);
        Assert.Null(blocker.Kind);
        Assert.Contains("not supported yet", blocker.Text);
    }

    private static Guide Gd(string exerciseId, string? works, string? notes = null, params string[] cues) =>
        new(exerciseId, works, null, null, null, cues, [], null, null, [], null, notes);

    [Fact]
    public void A_guide_for_an_exercise_with_none_is_added_with_the_files_personal_notes()
    {
        var preview = Preview(Library(exercises: Squat), Doc(guides: [Gd("squat", "Quads", "from the file")]), ImportKind.Guides);

        Assert.True(preview.IsReady);
        Assert.Equal(["Adds 1 Guide.", "Touches no Workouts."], preview.Summary);
        Assert.Contains(preview.Items, i => i is { Action: ImportItemAction.Add, Kind: RecordKind.Guide, Key: "squat" });
        var guide = Assert.Single(Applied(preview).Guides);
        Assert.Equal(("Quads", "from the file"), (guide.Works, guide.PersonalNotes));
    }

    [Fact]
    public void An_existing_guide_with_content_is_kept_by_default_and_offers_replace()
    {
        var library = Library(null, [Gd("squat", "Mine", "my notes")], Squat);

        var preview = Preview(library, Doc(guides: [Gd("squat", "Theirs")]), ImportKind.Guides);

        Assert.True(preview.IsReady);
        Assert.Empty(preview.Changes);
        Assert.Equal(["Nothing to change.", "Keeps 1 existing Guide. Choose to replace it to use the file's version."], preview.Summary);
        Assert.Contains(preview.Items, i => i is { Action: ImportItemAction.Keep, Kind: RecordKind.Guide, Key: "squat", OffersReplace: true });
    }

    [Theory]
    [InlineData("my notes", "my notes")]
    [InlineData(null, "file notes")]
    [InlineData("  ", "file notes")]
    public void Replacing_a_guide_swaps_the_content_and_never_overwrites_personal_notes(string? mine, string expectedNotes)
    {
        var library = Library(null, [Gd("squat", "Mine", mine, "old cue")], Squat);
        var decisions = ImportDecisions.None.WithGuide("squat", KeepOrReplace.Replace);

        var preview = Preview(library, Doc(guides: [Gd("squat", "Theirs", "file notes", "new cue")]), ImportKind.Guides, decisions);

        Assert.Equal(["Replaces 1 Guide.", "Touches no Workouts."], preview.Summary);
        Assert.Contains(preview.Items, i => i is { Action: ImportItemAction.Change, Kind: RecordKind.Guide, Key: "squat" });
        var guide = Assert.Single(Applied(preview).Guides);
        Assert.Equal(("Theirs", expectedNotes), (guide.Works, guide.PersonalNotes));
        Assert.Equal(["new cue"], guide.Cues);
    }

    [Fact]
    public void An_existing_guide_with_no_content_is_replaced_without_asking_and_keeps_its_notes()
    {
        var library = Library(null, [Gd("squat", null, "my notes")], Squat);

        var preview = Preview(library, Doc(guides: [Gd("squat", "Theirs", "file notes")]), ImportKind.Guides);

        Assert.Equal(["Replaces 1 Guide.", "Touches no Workouts."], preview.Summary);
        var guide = Assert.Single(Applied(preview).Guides);
        Assert.Equal(("Theirs", "my notes"), (guide.Works, guide.PersonalNotes));
        Assert.DoesNotContain(preview.Items, i => i.OffersReplace);
    }

    [Fact]
    public void Importing_the_same_guides_file_again_has_nothing_to_change_and_nothing_to_keep()
    {
        var document = Doc(guides: [Gd("squat", "Theirs", "file notes")]);
        var afterFirst = Applied(Preview(Library(null, [Gd("squat", "Mine")], Squat), document, ImportKind.Guides, ImportDecisions.None.WithGuide("squat", KeepOrReplace.Replace)));

        var again = Preview(afterFirst, document, ImportKind.Guides);

        Assert.Empty(again.Changes);
        Assert.Equal(["Nothing to change."], again.Summary);
        Assert.DoesNotContain(again.Items, i => i.Kind == RecordKind.Guide);
    }

    [Fact]
    public void Guides_follow_the_matched_exercise_new_exercises_come_along_and_plans_are_ignored()
    {
        var library = Library(null, [Gd("goblet-squat", "Mine")], Ex("goblet-squat", "Goblet squat"));
        var document = Doc(
            [Ex("gs", "goblet squat"), Ex("sled", "Sled push")],
            [MakePlan("hyrox", "Hyrox block", true, "s9", "ph9", "sled")],
            [Gd("gs", "Theirs"), Gd("sled", "Push it")]);

        var kept = Preview(library, document, ImportKind.Guides);
        var replaced = Preview(library, document, ImportKind.Guides, ImportDecisions.None.WithGuide("goblet-squat", KeepOrReplace.Replace));

        Assert.True(kept.IsReady);
        Assert.Contains(kept.Items, i => i is { Action: ImportItemAction.Keep, Key: "goblet-squat", OffersReplace: true });
        Assert.Contains(kept.Items, i => i is { Action: ImportItemAction.Ignore, Kind: RecordKind.Plan });
        Assert.Equal(["Adds 1 Exercise.", "Adds 1 Guide.", "Keeps 1 existing Guide. Choose to replace it to use the file's version.", "Touches no Workouts."], kept.Summary);
        Assert.Equal(["goblet-squat", "sled"], Applied(kept).Exercises.Select(e => e.Id));
        Assert.Equal(["Mine", "Push it"], Applied(kept).Guides.Select(g => g.Works));
        Assert.Equal(["Theirs", "Push it"], Applied(replaced).Guides.Select(g => g.Works));
        Assert.Empty(Applied(kept).Plans);
    }

    [Fact]
    public void A_guide_for_an_exercise_nowhere_is_a_blocker()
    {
        var preview = Preview(Library(exercises: Squat), Doc(guides: [Gd("ghost", "Boo")]), ImportKind.Guides);

        Assert.False(preview.IsReady);
        Assert.Equal(("guides[0].exerciseId", RecordKind.Guide), (Assert.Single(preview.Blockers).Key, Assert.Single(preview.Blockers).Kind!.Value));
    }

    [Fact]
    public void The_first_plan_is_active_and_only_the_first_of_several()
    {
        var plans = new[] { MakePlan("a", "Plan A", true, "sa", "pa"), MakePlan("b", "Plan B", false, "sb", "pb") };

        var preview = Preview(Library(exercises: Squat), Doc(plans: plans), ImportKind.Plan);

        Assert.True(preview.IsReady);
        Assert.Equal([("a", true), ("b", false)], Applied(preview).Plans.Select(p => (p.Id, p.IsActive)));
        Assert.Contains("Creates a new Plan called \"Plan A\" and makes it active, because you have no Plan yet.", preview.Summary);
        Assert.Contains("Creates a new Plan called \"Plan B\", inactive.", preview.Summary);
        Assert.Contains(preview.Items, i => i.Key == "a" && i.Text.Contains("active"));
    }
}
