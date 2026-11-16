using System.Text.Json.Nodes;
using Core.Serialisation;

namespace Core.Tests.Serialisation;

public class WorkoutErrorTests
{
    private static void Check(Action<JsonObject> edit, string path, string code, string? messagePart = null)
    {
        var result = ExchangeFormat.Read(WorkoutTestData.Broken(edit));
        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors);
        Assert.Equal((path, code), (error.Path, error.Code));
        if (messagePart is not null)
        {
            Assert.Contains(messagePart, error.Message);
        }
    }

    private static JsonObject W(JsonObject r, int i = 0) => WorkoutTestData.Workout(r, i);

    private static JsonObject E(JsonObject r, int e) => WorkoutTestData.Entry(r, 0, e);

    private static JsonObject S(JsonObject r, int e = 0, int s = 0) => WorkoutTestData.Set(r, 0, e, s);

    [Fact]
    public void Session_from_any_plan_in_the_document_resolves() =>
        Assert.True(ExchangeFormat.Read(WorkoutTestData.Broken(r => W(r)["sessionId"] = "c-engine")).IsSuccess);

    [Fact]
    public void Dangling_session_is_rejected() =>
        Check(r => W(r)["sessionId"] = "ghost", "workouts[0].sessionId", "unknown_session", "ghost");

    [Fact]
    public void Dangling_entry_exercise_is_rejected() =>
        Check(r => E(r, 0)["exerciseId"] = "nope", "workouts[0].entries[0].exerciseId", "unknown_exercise", "nope");

    [Fact]
    public void Dangling_planned_exercise_is_rejected() =>
        Check(r => W(r)["planned"]![0]!["exerciseId"] = "nope", "workouts[0].planned[0].exerciseId", "unknown_exercise");

    [Fact]
    public void Dangling_swapped_from_is_rejected() =>
        Check(r => E(r, 2)["swappedFromExerciseId"] = "nope", "workouts[0].entries[2].swappedFromExerciseId", "unknown_exercise");

    [Fact]
    public void Dangling_pain_exercise_is_rejected() =>
        Check(r => W(r)["painCheck"]!["exerciseId"] = "nope", "workouts[0].painCheck.exerciseId", "unknown_exercise");

    [Fact]
    public void Negative_weight_is_rejected() =>
        Check(r => S(r)["weightKg"] = -1, "workouts[0].entries[0].sets[0].weightKg", "out_of_range");

    [Fact]
    public void Zero_reps_are_rejected() =>
        Check(r => S(r)["reps"] = 0, "workouts[0].entries[0].sets[0].reps", "out_of_range");

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void Rpe_outside_1_to_10_is_rejected(int rpe) =>
        Check(r => E(r, 0)["rpe"] = rpe, "workouts[0].entries[0].rpe", "out_of_range", "10");

    [Fact]
    public void Unknown_status_lists_allowed_values() =>
        Check(r => E(r, 0)["status"] = "maybe", "workouts[0].entries[0].status", "unknown_value", "swapped");

    [Fact]
    public void Unknown_body_area_is_rejected() =>
        Check(r => W(r)["painCheck"]!["areas"]![0] = "toe", "workouts[0].painCheck.areas[0]", "unknown_value", "knee");

    [Fact]
    public void Unknown_severity_is_rejected() =>
        Check(r => W(r)["painCheck"]!["severity"] = "ouch", "workouts[0].painCheck.severity", "unknown_value", "mild");

    [Fact]
    public void Reported_pain_needs_an_area() =>
        Check(r => W(r)["painCheck"]!["areas"] = new JsonArray(), "workouts[0].painCheck.areas", "missing_field");

    [Fact]
    public void Reported_pain_needs_a_severity() =>
        Check(r => W(r)["painCheck"]!.AsObject().Remove("severity"), "workouts[0].painCheck.severity", "missing_field");

    [Fact]
    public void No_pain_cannot_carry_areas() =>
        Check(r => W(r, 1)["painCheck"]!["areas"] = new JsonArray("knee"), "workouts[1].painCheck.areas", "invalid_value");

    [Fact]
    public void Swapped_entry_needs_its_original() =>
        Check(r => E(r, 2).Remove("swappedFromExerciseId"), "workouts[0].entries[2].swappedFromExerciseId", "missing_field");

    [Fact]
    public void Skip_reason_only_on_skipped_entries() =>
        Check(r => E(r, 0)["skipReason"] = "tired", "workouts[0].entries[0].skipReason", "invalid_value");

    [Fact]
    public void Skipped_entry_cannot_have_sets() =>
        Check(r => E(r, 1)["sets"] = new JsonArray(new JsonObject { ["id"] = "sx", ["reps"] = 3 }), "workouts[0].entries[1].sets", "invalid_value");

    [Fact]
    public void Duplicate_workout_id_is_rejected() =>
        Check(r => W(r, 1)["id"] = "w-1", "workouts[1].id", "duplicate_id");

    [Fact]
    public void Duplicate_set_id_is_rejected_across_workouts() =>
        Check(r => WorkoutTestData.Set(r, 1, 0, 0)["id"] = "s-1", "workouts[1].entries[0].sets[0].id", "duplicate_id");

    [Fact]
    public void End_before_start_is_rejected() =>
        Check(r => W(r)["endedAt"] = "2026-11-16T16:00:00+00:00", "workouts[0].endedAt", "end_before_start");

    [Fact]
    public void Bad_date_is_rejected() =>
        Check(r => W(r)["date"] = "16/11/2026", "workouts[0].date", "invalid_value", "yyyy-MM-dd");

    [Fact]
    public void Both_distance_units_on_one_set_are_rejected() =>
        Check(r => S(r, 2)["distanceM"] = 100, "workouts[0].entries[2].sets[0].distanceM", "invalid_value");

    [Fact]
    public void Unknown_workout_field_is_a_warning()
    {
        var result = ExchangeFormat.Read(WorkoutTestData.Broken(r => W(r)["mood"] = "good"));

        Assert.True(result.IsSuccess);
        Assert.Equal("workouts[0].mood", Assert.Single(result.Warnings).Path);
    }
}
