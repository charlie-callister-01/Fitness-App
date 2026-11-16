using System.Text.Json.Nodes;
using Core.Serialisation;

namespace Core.Tests.Serialisation;

/// <summary>Reading JSON is parse then validate: an error in one record must not cause cross-record errors in another.</summary>
public class ParseThenValidateTests
{
    private static IReadOnlyList<FormatIssue> Errors(Action<JsonObject> edit) =>
        ExchangeFormat.Read(WorkoutTestData.Broken(edit)).Errors;

    private static (string Path, string Code) Only(IReadOnlyList<FormatIssue> errors)
    {
        var error = Assert.Single(errors);
        return (error.Path, error.Code);
    }

    [Fact]
    public void Plan_with_an_unrelated_error_does_not_make_its_sessions_unknown()
    {
        var errors = Errors(r => PlanTestData.Plan0(r)["name"] = "");

        Assert.Equal(("plans[0].name", "missing_field"), Only(errors));
    }

    [Fact]
    public void Session_with_a_bad_field_is_still_a_known_session()
    {
        var errors = Errors(r => PlanTestData.Plan0(r)["sessions"]![0]!["name"] = 5);

        Assert.Equal(("plans[0].sessions[0].name", "wrong_type"), Only(errors));
    }

    [Fact]
    public void Phase_with_a_bad_range_is_reported_once_with_no_neighbour_gap()
    {
        var errors = Errors(r => PlanTestData.Plan0(r)["phases"]![1]!["endWeek"] = 5);

        Assert.Equal(("plans[0].phases[1].endWeek", "out_of_range"), Only(errors));
    }

    [Fact]
    public void Phase_with_an_unreadable_start_week_is_reported_once_with_no_neighbour_gap()
    {
        var errors = Errors(r => PlanTestData.Plan0(r)["phases"]![1]!["startWeek"] = "nine");

        Assert.Equal(("plans[0].phases[1].startWeek", "wrong_type"), Only(errors));
    }

    [Fact]
    public void Wrong_type_for_is_active_does_not_cause_no_active_plan()
    {
        var errors = Errors(r => PlanTestData.Plan0(r)["isActive"] = "yes");

        Assert.Equal(("plans[0].isActive", "wrong_type"), Only(errors));
    }

    [Fact]
    public void Skipped_entry_not_in_planned_is_rejected()
    {
        var errors = Errors(r => WorkoutTestData.Entry(r, 0, 1)["exerciseId"] = "5k-run");

        Assert.Equal(("workouts[0].entries[1].exerciseId", "not_in_planned"), Only(errors));
    }

    [Fact]
    public void Swapped_entry_replacing_something_not_in_planned_is_rejected()
    {
        var errors = Errors(r => WorkoutTestData.Entry(r, 0, 2)["swappedFromExerciseId"] = "5k-run");

        Assert.Equal(("workouts[0].entries[2].swappedFromExerciseId", "not_in_planned"), Only(errors));
    }

    [Fact]
    public void Skipped_entry_is_accepted_when_planned_is_empty()
    {
        var json = WorkoutTestData.Broken(r =>
        {
            WorkoutTestData.Workout(r, 0)["planned"] = new JsonArray();
            WorkoutTestData.Entry(r, 0, 1)["exerciseId"] = "5k-run";
        });

        Assert.True(ExchangeFormat.Read(json).IsSuccess);
    }

    [Fact]
    public void Unknown_swapped_from_is_reported_once_not_also_as_not_in_planned()
    {
        var errors = Errors(r => WorkoutTestData.Entry(r, 0, 2)["swappedFromExerciseId"] = "nope");

        Assert.Equal(("workouts[0].entries[2].swappedFromExerciseId", "unknown_exercise"), Only(errors));
    }
}
