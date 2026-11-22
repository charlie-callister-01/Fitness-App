using Core.Model;
using Core.Serialisation;

namespace Core.Tests.Serialisation;

/// <summary>The standalone validator, called on documents built in memory (never serialised to JSON).</summary>
public class DocumentValidatorTests
{
    private static ExchangeDocument Valid() => WorkoutTestData.Rich();

    private static (string Path, string Code) Only(ExchangeDocument document)
    {
        var issue = Assert.Single(ExchangeFormat.Validate(document));
        return (issue.Path, issue.Code);
    }

    private static ExchangeDocument WithPlan(ExchangeDocument document, Func<Plan, Plan> change) =>
        document with { Plans = document.Plans!.Select((p, i) => i == 0 ? change(p) : p).ToList() };

    private static ExchangeDocument WithWorkout(ExchangeDocument document, Func<Workout, Workout> change) =>
        document with { Workouts = document.Workouts!.Select((w, i) => i == 0 ? change(w) : w).ToList() };

    [Fact]
    public void A_valid_document_has_no_issues() => Assert.Empty(ExchangeFormat.Validate(Valid()));

    [Fact]
    public void Duplicate_exercise_id_is_reported_at_the_second()
    {
        var document = Valid() with { Exercises = [PlanTestData.Exercise("rdl"), PlanTestData.Exercise("rdl")] };

        Assert.Contains(ExchangeFormat.Validate(document), i => i is { Path: "exercises[1].id", Code: "duplicate_id" });
    }

    [Fact]
    public void Unknown_exercise_in_a_session_row_is_reported()
    {
        var document = WithPlan(Valid(), p => p with
        {
            Sessions = [p.Sessions[0] with { Exercises = [new SessionExercise("ghost", 3, 8, null, null)] }, .. p.Sessions.Skip(1)],
        });

        Assert.Equal(("plans[0].sessions[0].exercises[0].exerciseId", "unknown_exercise"), Only(document));
    }

    [Fact]
    public void Exercise_references_are_not_checked_when_the_document_has_no_exercises()
    {
        var document = WithWorkout(Valid() with { Exercises = null }, w => w);

        Assert.Empty(ExchangeFormat.Validate(document));
    }

    [Fact]
    public void Workout_session_is_not_checked_when_the_document_has_no_plans()
    {
        var document = WithWorkout(Valid() with { Plans = null }, w => w with { SessionId = "ghost" });

        Assert.Empty(ExchangeFormat.Validate(document));
    }

    [Fact]
    public void Workout_session_unknown_to_every_plan_is_reported()
    {
        var document = WithWorkout(Valid(), w => w with { SessionId = "ghost" });

        Assert.Equal(("workouts[0].sessionId", "unknown_session"), Only(document));
    }

    [Fact]
    public void No_active_plan_is_reported_when_plans_are_present()
    {
        var document = WithPlan(Valid(), p => p with { IsActive = false });

        Assert.Equal(("plans", "no_active_plan"), Only(document));
    }

    [Fact]
    public void Second_active_plan_is_reported_at_that_plan()
    {
        var document = Valid() with { Plans = Valid().Plans!.Select(p => p with { IsActive = true }).ToList() };

        Assert.Equal(("plans[1].isActive", "multiple_active_plans"), Only(document));
    }

    private static ExchangeDocument WithMainPhases(ExchangeDocument document, Func<IReadOnlyList<Phase>, IReadOnlyList<Phase>> change) =>
        WithPlan(document, p => p with { Tracks = [p.Tracks[0] with { Phases = change(p.Tracks[0].Phases) }, .. p.Tracks.Skip(1)] });

    [Fact]
    public void Phase_gap_is_reported_at_the_phase_after_it()
    {
        var document = WithMainPhases(Valid(), ph => [ph[0], ph[1] with { StartWeek = 10 }, ph[2]]);

        Assert.Equal(("plans[0].phases[1].startWeek", "phase_gap"), Only(document));
    }

    [Fact]
    public void Phase_overlap_is_reported_at_the_later_phase()
    {
        var document = WithMainPhases(Valid(), ph => [ph[0], ph[1] with { StartWeek = 8 }, ph[2]]);

        Assert.Equal(("plans[0].phases[1].startWeek", "phase_overlap"), Only(document));
    }

    [Fact]
    public void Phase_with_an_invalid_range_is_reported_once_with_no_gap_at_its_neighbour()
    {
        var document = WithMainPhases(Valid(), ph => [ph[0], ph[1] with { EndWeek = 3 }, ph[2]]);

        Assert.Equal(("plans[0].phases[1].endWeek", "out_of_range"), Only(document));
    }

    private static readonly Phase BadRangePhase = new("extra", "Extra", 30, 25, [], []);

    private static IEnumerable<(string Path, string Code)> Issues(ExchangeDocument document) =>
        ExchangeFormat.Validate(document).Select(i => (i.Path, i.Code)).OrderBy(i => i.Path, StringComparer.Ordinal);

    [Fact]
    public void A_gap_between_valid_phases_is_still_reported_when_another_phase_has_a_bad_range()
    {
        var document = WithMainPhases(Valid(), ph => [ph[0], ph[1], ph[2] with { StartWeek = 21 }, BadRangePhase]);

        Assert.Equal(
            [("plans[0].phases[2].startWeek", "phase_gap"), ("plans[0].phases[3].endWeek", "out_of_range")],
            Issues(document));
    }

    [Fact]
    public void An_overlap_between_valid_phases_is_still_reported_when_another_phase_has_a_bad_range()
    {
        var document = WithMainPhases(Valid(), ph => [ph[0], ph[1], ph[2] with { StartWeek = 18 }, BadRangePhase]);

        Assert.Equal(
            [("plans[0].phases[2].startWeek", "phase_overlap"), ("plans[0].phases[3].endWeek", "out_of_range")],
            Issues(document));
    }

    [Fact]
    public void Override_for_a_session_of_another_plan_is_reported()
    {
        var document = WithMainPhases(Valid(), ph => [ph[0], ph[1] with { SessionOverrides = [new SessionOverride("ghost", [])] }, ph[2]]);

        Assert.Equal(("plans[0].phases[1].sessionOverrides[0].sessionId", "unknown_session"), Only(document));
    }

    [Fact]
    public void Guide_for_an_unknown_exercise_is_reported()
    {
        var document = Valid() with { Guides = [new Guide("ghost", null, null, null, null, [], [], null, null, [], null, null)] };

        Assert.Equal(("guides[0].exerciseId", "unknown_exercise"), Only(document));
    }

    [Fact]
    public void Duplicate_set_ids_across_workouts_are_reported()
    {
        var first = Valid().Workouts![0].Entries[0].Sets[0];
        var document = WithWorkout(Valid(), w => w);
        var second = document.Workouts![1] with { Entries = [new WorkoutEntry("farmers-carry", EntryStatus.Done, null, null, null, [first])] };
        document = document with { Workouts = [document.Workouts[0], second, document.Workouts[2]] };

        Assert.Equal(("workouts[1].entries[0].sets[0].id", "duplicate_id"), Only(document));
    }

    [Fact]
    public void Skipped_entry_outside_the_planned_rows_is_reported_but_not_when_planned_is_empty()
    {
        var stray = new WorkoutEntry("5k-run", EntryStatus.Skipped, null, null, null, []);

        Assert.Equal(("workouts[0].entries[0].exerciseId", "not_in_planned"), Only(WithWorkout(Valid(), w => w with { Entries = [stray] })));
        Assert.Empty(ExchangeFormat.Validate(WithWorkout(Valid(), w => w with { Planned = [], Entries = [stray] })));
    }

    [Fact]
    public void Placeholder_blank_ids_left_by_the_parse_phase_are_not_reported()
    {
        var document = Valid() with { Exercises = [PlanTestData.Exercise(""), PlanTestData.Exercise("")] };

        Assert.DoesNotContain(ExchangeFormat.Validate(document), i => i.Code == "duplicate_id");
    }
}
