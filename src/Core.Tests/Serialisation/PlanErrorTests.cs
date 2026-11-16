using System.Text.Json.Nodes;
using Core.Serialisation;

namespace Core.Tests.Serialisation;

public class PlanErrorTests
{
    private static FormatIssue SingleError(string json)
    {
        var result = ExchangeFormat.Read(json);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Document);
        return Assert.Single(result.Errors);
    }

    private static JsonObject Phase(JsonObject root, int index) => PlanTestData.Plan0(root)["phases"]![index]!.AsObject();

    private static JsonObject Session(JsonObject root, int index) => PlanTestData.Plan0(root)["sessions"]![index]!.AsObject();

    private static JsonObject Row(JsonObject root, int session, int row) => Session(root, session)["exercises"]![row]!.AsObject();

    private static void Check(string json, string path, string code, string? messagePart = null)
    {
        var error = SingleError(json);
        Assert.Equal((path, code), (error.Path, error.Code));
        if (messagePart is not null)
        {
            Assert.Contains(messagePart, error.Message);
        }
    }

    [Fact]
    public void No_active_plan_is_rejected()
    {
        Check(PlanTestData.Broken(r => PlanTestData.Plan0(r)["isActive"] = false), "plans", "no_active_plan", "exactly one");
    }

    [Fact]
    public void Two_active_plans_name_the_second_one()
    {
        Check(PlanTestData.Broken(r => r["plans"]![1]!["isActive"] = true), "plans[1].isActive", "multiple_active_plans", "hyrox-2027");
    }

    [Fact]
    public void Empty_plans_list_means_none_and_is_accepted()
    {
        Assert.True(ExchangeFormat.Read(PlanTestData.Broken(r => r["plans"] = new JsonArray())).IsSuccess);
    }

    [Fact]
    public void Duplicate_plan_ids_are_rejected()
    {
        Check(PlanTestData.Broken(r => r["plans"]![1]!["id"] = "hyrox-2027"), "plans[1].id", "duplicate_id");
    }

    [Fact]
    public void Duplicate_session_ids_in_a_plan_are_rejected()
    {
        Check(PlanTestData.Broken(r => Session(r, 1)["id"] = "a-lower"), "plans[0].sessions[1].id", "duplicate_id");
    }

    [Fact]
    public void Phase_gap_names_the_phase_after_the_gap()
    {
        Check(PlanTestData.Broken(r => Phase(r, 1)["startWeek"] = 10), "plans[0].phases[1].startWeek", "phase_gap", "week 9");
    }

    [Fact]
    public void Phase_overlap_names_the_overlapping_phase()
    {
        Check(PlanTestData.Broken(r => Phase(r, 1)["startWeek"] = 8), "plans[0].phases[1].startWeek", "phase_overlap", "week 8");
    }

    [Fact]
    public void First_phase_must_start_at_week_one()
    {
        Check(PlanTestData.Broken(r => Phase(r, 0)["startWeek"] = 2), "plans[0].phases[0].startWeek", "phase_gap", "week 1");
    }

    [Fact]
    public void Phase_may_be_listed_out_of_order()
    {
        var json = PlanTestData.Broken(r =>
        {
            var phases = PlanTestData.Plan0(r)["phases"]!.AsArray();
            var first = phases[0]!.DeepClone();
            phases.RemoveAt(0);
            phases.Add(first);
        });

        Assert.True(ExchangeFormat.Read(json).IsSuccess);
    }

    [Fact]
    public void Phase_ending_before_it_starts_is_out_of_range()
    {
        Check(PlanTestData.Broken(r => Phase(r, 2)["endWeek"] = 19), "plans[0].phases[2].endWeek", "out_of_range", "20");
    }

    [Fact]
    public void Dangling_exercise_in_a_session_row_names_the_exact_field()
    {
        Check(PlanTestData.Broken(r => Row(r, 0, 1)["exerciseId"] = "nope"),
            "plans[0].sessions[0].exercises[1].exerciseId", "unknown_exercise", "nope");
    }

    [Fact]
    public void Dangling_exercise_in_a_phase_override_is_rejected()
    {
        Check(PlanTestData.Broken(r => Phase(r, 1)["sessionOverrides"]![0]!["exercises"]![0]!["exerciseId"] = "nope"),
            "plans[0].phases[1].sessionOverrides[0].exercises[0].exerciseId", "unknown_exercise");
    }

    [Fact]
    public void Dangling_exercise_in_a_phase_goal_is_rejected()
    {
        Check(PlanTestData.Broken(r => Phase(r, 0)["goals"]![0]!["exerciseId"] = "nope"),
            "plans[0].phases[0].goals[0].exerciseId", "unknown_exercise");
    }

    [Fact]
    public void Override_for_unknown_session_is_rejected()
    {
        Check(PlanTestData.Broken(r => Phase(r, 1)["sessionOverrides"]![0]!["sessionId"] = "ghost"),
            "plans[0].phases[1].sessionOverrides[0].sessionId", "unknown_session", "ghost");
    }

    [Theory]
    [InlineData("sets", 0, "out_of_range")]
    [InlineData("reps", 0, "out_of_range")]
    [InlineData("reps", "ten", "wrong_type")]
    public void Bad_row_numbers_name_the_field(string field, object value, string code)
    {
        var node = value is int i ? JsonValue.Create(i) : JsonValue.Create((string)value);
        Check(PlanTestData.Broken(r => Row(r, 0, 0)[field] = node), $"plans[0].sessions[0].exercises[0].{field}", code);
    }

    [Fact]
    public void Rep_range_high_end_below_low_end_is_rejected()
    {
        Check(PlanTestData.Broken(r => Row(r, 0, 0)["repsMax"] = 6), "plans[0].sessions[0].exercises[0].repsMax", "out_of_range", "8");
    }

    [Fact]
    public void Rep_range_without_a_low_end_is_rejected()
    {
        Check(PlanTestData.Broken(r => Row(r, 0, 1)["repsMax"] = 6), "plans[0].sessions[0].exercises[1].repsMax", "invalid_value", "reps");
    }

    [Theory]
    [InlineData("weeklyTarget", 0)]
    [InlineData("streakMinWorkoutsPerWeek", 0)]
    [InlineData("weightIncrementKg", 0)]
    [InlineData("weightIncrementKg", -2)]
    public void Out_of_range_plan_numbers_name_the_field(string field, int value)
    {
        Check(PlanTestData.Broken(r => PlanTestData.Plan0(r)[field] = value), $"plans[0].{field}", "out_of_range");
    }

    [Fact]
    public void Benchmark_interval_below_one_is_rejected()
    {
        Check(PlanTestData.Broken(r => Session(r, 2)["benchmarkIntervalWeeks"] = 0), "plans[0].sessions[2].benchmarkIntervalWeeks", "out_of_range");
    }

    [Fact]
    public void Unknown_day_lists_the_allowed_days()
    {
        Check(PlanTestData.Broken(r => Session(r, 0)["defaultDay"] = "funday"), "plans[0].sessions[0].defaultDay", "unknown_value", "monday");
    }

    [Fact]
    public void Unknown_session_type_is_rejected()
    {
        Check(PlanTestData.Broken(r => Session(r, 0)["type"] = "yoga"), "plans[0].sessions[0].type", "unknown_value", "strength");
    }

    [Fact]
    public void Bad_start_date_is_rejected()
    {
        Check(PlanTestData.Broken(r => PlanTestData.Plan0(r)["startDate"] = "19/10/2026"), "plans[0].startDate", "invalid_value", "yyyy-MM-dd");
    }

    [Fact]
    public void Goal_date_before_start_date_is_rejected()
    {
        Check(PlanTestData.Broken(r => PlanTestData.Plan0(r)["goalDate"] = "2026-01-01"), "plans[0].goalDate", "out_of_range", "2026-10-19");
    }

    [Fact]
    public void Missing_plan_name_is_rejected()
    {
        Check(PlanTestData.Broken(r => PlanTestData.Plan0(r).Remove("name")), "plans[0].name", "missing_field");
    }

    [Fact]
    public void Unknown_plan_field_is_a_warning_only()
    {
        var result = ExchangeFormat.Read(PlanTestData.Broken(r => PlanTestData.Plan0(r)["colour"] = "red"));

        Assert.True(result.IsSuccess);
        Assert.Equal("plans[0].colour", Assert.Single(result.Warnings).Path);
    }
}
