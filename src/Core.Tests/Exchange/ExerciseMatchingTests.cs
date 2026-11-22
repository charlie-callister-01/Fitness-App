using Core.Exchange;
using Core.Model;
using Core.Serialisation;
using Core.Storage;
using Core.Tests.Serialisation;

namespace Core.Tests.Exchange;

public class ExerciseMatchingTests
{
    private static Exercise Ex(string id, string name, LogKind kind = LogKind.Load, bool benchmark = false, ExerciseTags? tags = null) =>
        new(id, name, kind, tags, benchmark);

    private static DataSnapshot Library(params Exercise[] exercises) => DataSnapshot.Empty with { Exercises = exercises };

    private static Inspected Incoming(ExchangeDocument document)
    {
        var result = ImportInput.Inspect(ExchangeFormat.Write(document));
        Assert.True(result.IsAccepted, string.Join("; ", result.Rejections));
        return result.Inspected!;
    }

    private static ExchangeDocument Doc(params Exercise[] exercises) =>
        new(ExchangeFormat.CurrentVersion, null, null, exercises, null);

    private static ExerciseMatching Match(DataSnapshot library, ExchangeDocument document, ImportDecisions? decisions = null) =>
        ExerciseMatcher.Match(library, Incoming(document), decisions ?? ImportDecisions.None);

    private static Guide GuideFor(string id) => new(id, "Quads", null, null, null, [], [], null, null, [], null, null);

    [Fact]
    public void Same_id_matches_whatever_the_names_say()
    {
        var m = Match(Library(Ex("squat", "Back squat")), Doc(Ex("squat", "Totally different")));

        var match = Assert.Single(m.Matches);
        Assert.Equal(ExerciseOutcome.MatchedById, match.Outcome);
        Assert.Equal("squat", match.Chosen);
        Assert.Contains(m.Findings, f => f.Code == "name_differs" && f.Kind == MatchFindingKind.Note && f.Message.Contains("Totally different"));
        Assert.Empty(m.ToAdd);
    }

    [Fact]
    public void Same_name_after_normalising_maps_the_file_id_to_the_library_id()
    {
        var m = Match(Library(Ex("goblet-squat", "Goblet squat")), Doc(Ex("gs", "  GOBLET   squat ")));

        Assert.Equal(ExerciseOutcome.MatchedByName, Assert.Single(m.Matches).Outcome);
        Assert.Equal("goblet-squat", m.IdMap["gs"]);
        Assert.Empty(m.OpenDecisions);
    }

    [Fact]
    public void Nothing_matching_is_new_and_keeps_its_file_id()
    {
        var m = Match(Library(Ex("squat", "Back squat")), Doc(Ex("sled-push", "Sled push", LogKind.Carry)));

        var match = Assert.Single(m.Matches);
        Assert.Equal(ExerciseOutcome.New, match.Outcome);
        Assert.Empty(match.Candidates);
        Assert.Equal("sled-push", m.IdMap["sled-push"]);
        Assert.Equal("Sled push", Assert.Single(m.ToAdd).Name);
    }

    [Fact]
    public void A_near_match_blocks_until_decided()
    {
        var m = Match(Library(Ex("goblet-squat", "Goblet squat")), Doc(Ex("gs", "Goblet squats")));

        var match = Assert.Single(m.Matches);
        Assert.Equal(ExerciseOutcome.NeedsDecision, match.Outcome);
        Assert.Equal("goblet-squat", Assert.Single(match.Candidates).Id);
        Assert.Null(match.Chosen);
        Assert.Single(m.OpenDecisions);
        Assert.Empty(m.ToAdd);
        Assert.False(m.IdMap.ContainsKey("gs"));
    }

    [Fact]
    public void Choosing_a_suggestion_uses_the_existing_exercise()
    {
        var m = Match(Library(Ex("goblet-squat", "Goblet squat")), Doc(Ex("gs", "Goblet squats")),
            ImportDecisions.None.WithExercise("gs", ExerciseChoice.Use("goblet-squat")));

        Assert.Equal(ExerciseOutcome.UsesExisting, Assert.Single(m.Matches).Outcome);
        Assert.Equal("goblet-squat", m.IdMap["gs"]);
        Assert.Empty(m.OpenDecisions);
        Assert.Empty(m.ToAdd);
    }

    [Fact]
    public void Choosing_add_as_new_adds_it_under_the_file_id()
    {
        var m = Match(Library(Ex("goblet-squat", "Goblet squat")), Doc(Ex("gs", "Goblet squats")),
            ImportDecisions.None.WithExercise("gs", ExerciseChoice.AddAsNew));

        Assert.Equal(ExerciseOutcome.New, Assert.Single(m.Matches).Outcome);
        Assert.Equal("gs", m.IdMap["gs"]);
        Assert.Equal("gs", Assert.Single(m.ToAdd).Id);
        Assert.Empty(m.OpenDecisions);
    }

    [Fact]
    public void A_choice_that_is_not_a_suggestion_leaves_the_decision_open()
    {
        var m = Match(Library(Ex("goblet-squat", "Goblet squat"), Ex("deadlift", "Deadlift")), Doc(Ex("gs", "Goblet squats")),
            ImportDecisions.None.WithExercise("gs", ExerciseChoice.Use("deadlift")));

        Assert.Single(m.OpenDecisions);
    }

    [Fact]
    public void Same_word_set_is_a_near_match_and_at_most_three_best_first_are_offered()
    {
        var library = Library(
            Ex("a", "Single arm row"), Ex("b", "Single arm rows"), Ex("c", "Arm single row"), Ex("d", "Row arm single"));
        var m = Match(library, Doc(Ex("x", "Row single arms")));

        var candidates = Assert.Single(m.Matches).Candidates;
        Assert.Equal(3, candidates.Count);
        Assert.All(candidates, c => Assert.Equal(1.0, c.Score));
    }

    [Fact]
    public void Dissimilar_names_are_not_near_matches()
    {
        var m = Match(Library(Ex("squat", "Back squat")), Doc(Ex("press", "Overhead press")));

        Assert.Equal(ExerciseOutcome.New, Assert.Single(m.Matches).Outcome);
    }

    [Fact]
    public void A_matched_exercise_keeps_library_log_kind_and_name_with_a_warning()
    {
        var m = Match(Library(Ex("run", "Run", LogKind.Time)), Doc(Ex("run", "Run", LogKind.DistanceKm)));

        Assert.Contains(m.Findings, f => f.Code == "log_kind_ignored" && f.Kind == MatchFindingKind.Warning && f.Path == "exercises[0].logKind");
        Assert.Empty(m.ToUpdate);
    }

    [Fact]
    public void Changed_tags_and_benchmark_on_a_matched_exercise_are_applied_as_an_update()
    {
        var tags = new ExerciseTags(["quads"], [], []);
        var m = Match(Library(Ex("squat", "Back squat")), Doc(Ex("squat", "Back squat", benchmark: true, tags: tags)));

        var updated = Assert.Single(m.ToUpdate);
        Assert.True(updated.IsBenchmark);
        Assert.Equal(["quads"], updated.Tags!.MuscleGroups);
        Assert.Equal("Back squat", updated.Name);
    }

    [Fact]
    public void An_identical_exercise_is_no_update()
    {
        var m = Match(Library(Ex("squat", "Back squat")), Doc(Ex("squat", "Back squat")));

        Assert.Empty(m.ToUpdate);
        Assert.Empty(m.ToAdd);
        Assert.Empty(m.Findings);
    }

    [Fact]
    public void Every_reference_follows_the_remap()
    {
        var source = WorkoutTestData.Rich() with
        {
            Badges = [new Badge("b1", "Squat club", "goblet-squat", 100), new Badge("b2", "Regular", "total-workouts", 50)],
            Guides = [GuideFor("goblet-squat")],
        };
        // The library calls the file's goblet-squat "gs" and its 5k-run "run"; everything else is new.
        var m = Match(Library(Ex("gs", "goblet-squat"), Ex("run", "5k-run", LogKind.Time)), source);

        Assert.Equal("gs", m.IdMap["goblet-squat"]);
        var d = m.Document;
        var plan = d.Plans![0];
        var mainTrack = plan.Tracks[0];
        Assert.Equal("gs", plan.Sessions[0].Exercises[0].ExerciseId);
        Assert.Equal("farmers-carry", plan.Sessions[0].Exercises[1].ExerciseId);
        Assert.Equal("gs", mainTrack.Phases[0].Goals[0].ExerciseId);
        Assert.Equal("run", plan.Sessions[1].Exercises[0].ExerciseId);
        Assert.Equal("rdl", mainTrack.Phases[1].SessionOverrides[0].Exercises[0].ExerciseId);
        var workout = d.Workouts![0];
        Assert.Equal("gs", workout.Planned[0].ExerciseId);
        Assert.Equal("gs", workout.Entries[0].ExerciseId);
        Assert.Equal("run", workout.Entries[2].ExerciseId);
        Assert.Equal("gs", workout.PainCheck!.ExerciseId);
        Assert.Equal("gs", d.Guides![0].ExerciseId);
        Assert.Equal("gs", d.Badges![0].Metric);
        Assert.Equal("total-workouts", d.Badges[1].Metric);
    }

    [Fact]
    public void Swapped_from_is_remapped()
    {
        var m = Match(Library(Ex("romanian-deadlift", "rdl")), WorkoutTestData.Rich());

        Assert.Equal("romanian-deadlift", m.Document.Workouts![0].Entries[2].SwappedFromExerciseId);
    }

    [Fact]
    public void The_remapped_document_lists_resolved_exercises_only()
    {
        var m = Match(Library(Ex("gs", "Goblet squat", LogKind.Reps)), Doc(Ex("goblet-squat", "goblet squat"), Ex("new-one", "Sled push")));

        Assert.Equal(["gs", "new-one"], m.Document.Exercises!.Select(e => e.Id));
        Assert.Equal(LogKind.Reps, m.Document.Exercises![0].LogKind);
    }

    [Fact]
    public void An_exercise_reference_that_is_nowhere_is_unknown_at_its_exact_path()
    {
        var source = new ExchangeDocument(ExchangeFormat.CurrentVersion, null, null, null, null,
            Guides: [GuideFor("nope")],
            Workouts: [new Workout("w", new DateOnly(2026, 1, 1), null, true, null, null, [],
                [new WorkoutEntry("squat", EntryStatus.Done, null, null, null, []), new WorkoutEntry("ghost", EntryStatus.Done, null, null, null, [])],
                new PainCheck("p", PainStatus.Reported, [BodyArea.Knee], PainSeverity.Mild, null, "phantom"))]);
        var m = Match(Library(Ex("squat", "Back squat")), source);

        var paths = m.Findings.Where(f => f.Code == "unknown_exercise" && f.Kind == MatchFindingKind.Blocker).Select(f => f.Path).Order();
        Assert.Equal(["guides[0].exerciseId", "workouts[0].entries[1].exerciseId", "workouts[0].painCheck.exerciseId"], paths);
    }

    [Fact]
    public void A_session_that_is_nowhere_is_unknown_at_its_exact_path()
    {
        var library = DataSnapshot.Empty with { Plans = [PlanTestData.Rich().Plans![0]] };
        var source = new ExchangeDocument(ExchangeFormat.CurrentVersion, null, null, null, null,
            Workouts:
            [
                new Workout("w1", new DateOnly(2026, 1, 1), "a-lower", false, null, null, [], [], null),
                new Workout("w2", new DateOnly(2026, 1, 2), "ghost-session", false, null, null, [], [], null),
            ],
            WeekSchedules: [new WeekSchedule(new DateOnly(2026, 1, 5), [new SessionMove("missing", new DateOnly(2026, 1, 6))])]);
        var m = Match(library, source);

        var paths = m.Findings.Where(f => f.Code == "unknown_session" && f.Kind == MatchFindingKind.Blocker).Select(f => f.Path).Order();
        Assert.Equal(["weekSchedules[0].moves[0].sessionId", "workouts[1].sessionId"], paths);
    }
}
