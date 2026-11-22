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

    private static JsonObject Track(JsonObject root, int index = 0) => PlanTestData.Plan0(root)["tracks"]![index]!.AsObject();

    private static JsonObject Phase(JsonObject root, int index) => Track(root)["phases"]![index]!.AsObject();

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
        Check(PlanTestData.Broken(r => Phase(r, 1)["startWeek"] = 10), "plans[0].tracks[0].phases[1].startWeek", "phase_gap", "week 9");
    }

    [Fact]
    public void Phase_overlap_names_the_overlapping_phase()
    {
        Check(PlanTestData.Broken(r => Phase(r, 1)["startWeek"] = 8), "plans[0].tracks[0].phases[1].startWeek", "phase_overlap", "week 8");
    }

    [Fact]
    public void First_phase_must_start_at_week_one()
    {
        Check(PlanTestData.Broken(r => Phase(r, 0)["startWeek"] = 2), "plans[0].tracks[0].phases[0].startWeek", "phase_gap", "week 1");
    }

    [Fact]
    public void Phase_may_be_listed_out_of_order()
    {
        var json = PlanTestData.Broken(r =>
        {
            var phases = Track(r)["phases"]!.AsArray();
            var first = phases[0]!.DeepClone();
            phases.RemoveAt(0);
            phases.Add(first);
        });

        Assert.True(ExchangeFormat.Read(json).IsSuccess);
    }

    [Fact]
    public void Phase_ending_before_it_starts_is_out_of_range()
    {
        Check(PlanTestData.Broken(r => Phase(r, 2)["endWeek"] = 19), "plans[0].tracks[0].phases[2].endWeek", "out_of_range", "20");
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
            "plans[0].tracks[0].phases[1].sessionOverrides[0].exercises[0].exerciseId", "unknown_exercise");
    }

    [Fact]
    public void Dangling_exercise_in_a_phase_goal_is_rejected()
    {
        Check(PlanTestData.Broken(r => Phase(r, 0)["goals"]![0]!["exerciseId"] = "nope"),
            "plans[0].tracks[0].phases[0].goals[0].exerciseId", "unknown_exercise");
    }

    [Fact]
    public void Override_for_unknown_session_is_rejected()
    {
        Check(PlanTestData.Broken(r => Phase(r, 1)["sessionOverrides"]![0]!["sessionId"] = "ghost"),
            "plans[0].tracks[0].phases[1].sessionOverrides[0].sessionId", "unknown_session", "ghost");
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
    [InlineData("weightIncrementKg", 0)]
    [InlineData("weightIncrementKg", -2)]
    public void Out_of_range_plan_numbers_name_the_field(string field, int value)
    {
        Check(PlanTestData.Broken(r => PlanTestData.Plan0(r)[field] = value), $"plans[0].{field}", "out_of_range");
    }

    [Theory]
    [InlineData("weeklyTarget", 0)]
    [InlineData("streakMinWorkoutsPerWeek", 0)]
    public void Out_of_range_weekly_track_numbers_name_the_field(string field, int value)
    {
        Check(PlanTestData.Broken(r => Track(r)[field] = value), $"plans[0].tracks[0].{field}", "out_of_range");
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

    private static string TwoTrackJson(Action<JsonObject>? edit = null)
    {
        var root = JsonNode.Parse(PlanTestData.RichJson())!.AsObject();
        var plan0 = PlanTestData.Plan0(root);
        plan0["tracks"]!.AsArray().Add(new JsonObject
        {
            ["id"] = "mobility",
            ["name"] = "Morning mobility",
            ["cadence"] = "daily",
            ["dayDonePercent"] = 75,
            ["phases"] = new JsonArray
            {
                new JsonObject { ["id"] = "mob-1", ["focus"] = "Habit", ["startWeek"] = 1, ["endWeek"] = 4 },
                new JsonObject { ["id"] = "mob-2", ["focus"] = "Keep", ["startWeek"] = 5, ["endWeek"] = 8 },
            },
        });
        plan0["sessions"]!.AsArray().Add(new JsonObject
        {
            ["id"] = "m-mobility",
            ["name"] = "Morning mobility",
            ["trackId"] = "mobility",
            ["defaultDay"] = "daily",
            ["type"] = "mobility",
            ["exercises"] = new JsonArray { new JsonObject { ["exerciseId"] = "goblet-squat", ["sets"] = 1 } },
        });
        edit?.Invoke(root);
        return root.ToJsonString();
    }

    [Fact]
    public void Missing_tracks_on_v2_plan_is_reported()
    {
        Check(PlanTestData.Broken(r => PlanTestData.Plan0(r).Remove("tracks")), "plans[0].tracks", "missing_field");
    }

    [Fact]
    public void Empty_tracks_on_v2_plan_is_reported()
    {
        Check(PlanTestData.Broken(r => PlanTestData.Plan0(r)["tracks"] = new JsonArray()), "plans[0].tracks", "missing_field");
    }

    [Fact]
    public void Main_track_must_be_weekly()
    {
        var json = PlanTestData.Broken(r =>
        {
            var plan = PlanTestData.Plan0(r);
            plan["tracks"] = new JsonArray
            {
                new JsonObject
                {
                    ["id"] = "mobility",
                    ["name"] = "Morning mobility",
                    ["cadence"] = "daily",
                    ["dayDonePercent"] = 75,
                    ["phases"] = new JsonArray(),
                },
            };
            plan["sessions"] = new JsonArray
            {
                new JsonObject
                {
                    ["id"] = "m-mobility",
                    ["name"] = "Morning mobility",
                    ["trackId"] = "mobility",
                    ["defaultDay"] = "daily",
                    ["type"] = "mobility",
                    ["exercises"] = new JsonArray(),
                },
            };
        });

        Check(json, "plans[0].tracks[0].cadence", "invalid_value", "weekly");
    }

    [Fact]
    public void Unknown_track_cadence_is_rejected_without_cascading_errors()
    {
        Check(PlanTestData.Broken(r => Track(r)["cadence"] = "monthly"), "plans[0].tracks[0].cadence", "unknown_value", "weekly");
    }

    [Fact]
    public void Duplicate_track_ids_within_a_plan_are_rejected()
    {
        var json = PlanTestData.Broken(r => PlanTestData.Plan0(r)["tracks"]!.AsArray().Add(new JsonObject
        {
            ["id"] = "hyrox-2027-main",
            ["name"] = "Duplicate",
            ["cadence"] = "daily",
            ["dayDonePercent"] = 75,
            ["phases"] = new JsonArray(),
        }));

        Check(json, "plans[0].tracks[1].id", "duplicate_id");
    }

    [Fact]
    public void Duplicate_track_ids_across_plans_are_rejected()
    {
        Check(PlanTestData.Broken(r => r["plans"]![1]!["tracks"]![0]!["id"] = "hyrox-2027-main"), "plans[1].tracks[0].id", "duplicate_id");
    }

    [Theory]
    [InlineData("weeklyTarget", 3)]
    [InlineData("streakMinWorkoutsPerWeek", 2)]
    [InlineData("streakCountsCustomWorkouts", false)]
    public void Weekly_fields_on_a_daily_track_are_rejected(string field, object value)
    {
        var node = value is int i ? JsonValue.Create(i) : JsonValue.Create((bool)value);
        Check(TwoTrackJson(r => Track(r, 1)[field] = node), $"plans[0].tracks[1].{field}", "invalid_value");
    }

    [Fact]
    public void Day_done_percent_on_a_weekly_track_is_rejected()
    {
        Check(PlanTestData.Broken(r => Track(r)["dayDonePercent"] = 75), "plans[0].tracks[0].dayDonePercent", "invalid_value");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Day_done_percent_outside_one_to_hundred_is_out_of_range(int value)
    {
        Check(TwoTrackJson(r => Track(r, 1)["dayDonePercent"] = value), "plans[0].tracks[1].dayDonePercent", "out_of_range");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void Day_done_percent_boundaries_are_accepted(int value)
    {
        Assert.True(ExchangeFormat.Read(TwoTrackJson(r => Track(r, 1)["dayDonePercent"] = value)).IsSuccess);
    }

    [Fact]
    public void Missing_session_track_id_is_rejected()
    {
        Check(PlanTestData.Broken(r => Session(r, 1).Remove("trackId")), "plans[0].sessions[1].trackId", "missing_field");
    }

    [Fact]
    public void Unknown_session_track_id_is_rejected_at_track_id()
    {
        Check(PlanTestData.Broken(r => Session(r, 1)["trackId"] = "ghost"), "plans[0].sessions[1].trackId", "unknown_track", "ghost");
    }

    [Fact]
    public void Daily_default_day_on_a_weekly_track_session_is_rejected()
    {
        Check(PlanTestData.Broken(r => Session(r, 0)["defaultDay"] = "daily"), "plans[0].sessions[0].defaultDay", "invalid_value");
    }

    [Fact]
    public void Non_daily_default_day_on_a_daily_track_session_is_rejected()
    {
        Check(TwoTrackJson(r => Session(r, 3)["defaultDay"] = "monday"), "plans[0].sessions[3].defaultDay", "invalid_value");
    }

    [Fact]
    public void Benchmark_session_off_the_main_track_is_rejected()
    {
        var json = PlanTestData.Broken(r =>
        {
            PlanTestData.Plan0(r)["tracks"]!.AsArray().Add(new JsonObject
            {
                ["id"] = "cardio",
                ["name"] = "Cardio",
                ["cadence"] = "weekly",
                ["weeklyTarget"] = 2,
                ["streakMinWorkoutsPerWeek"] = 1,
                ["streakCountsCustomWorkouts"] = false,
                ["phases"] = new JsonArray(),
            });
            Session(r, 2)["trackId"] = "cardio";
        });

        Check(json, "plans[0].sessions[2].trackId", "invalid_value", "main Track");
    }

    [Fact]
    public void Duplicate_phase_ids_across_tracks_are_rejected()
    {
        Check(TwoTrackJson(r => Track(r, 1)["phases"]![0]!["id"] = "foundation"), "plans[0].tracks[1].phases[0].id", "duplicate_id");
    }

    [Fact]
    public void Phase_gap_and_overlap_are_checked_per_track()
    {
        Check(TwoTrackJson(r => Track(r, 1)["phases"]![1]!["startWeek"] = 6), "plans[0].tracks[1].phases[1].startWeek", "phase_gap", "week 5");
        Check(TwoTrackJson(r => Track(r, 1)["phases"]![1]!["startWeek"] = 4), "plans[0].tracks[1].phases[1].startWeek", "phase_overlap", "week 4");
    }

    [Fact]
    public void Phase_override_naming_a_session_of_another_track_is_rejected()
    {
        Check(TwoTrackJson(r => Phase(r, 1)["sessionOverrides"]![0]!["sessionId"] = "m-mobility"),
            "plans[0].tracks[0].phases[1].sessionOverrides[0].sessionId", "unknown_session", "m-mobility");
    }

    [Theory]
    [InlineData("weeklyTarget", 5)]
    [InlineData("streakMinWorkoutsPerWeek", 4)]
    [InlineData("streakCountsCustomWorkouts", false)]
    [InlineData("phases", "[]")]
    public void V1_plan_level_fields_on_a_v2_plan_are_unknown_field_warnings(string field, object value)
    {
        var json = PlanTestData.Broken(r =>
        {
            JsonNode node = value switch
            {
                int i => JsonValue.Create(i)!,
                bool b => JsonValue.Create(b)!,
                _ => new JsonArray(),
            };
            PlanTestData.Plan0(r)[field] = node;
        });

        var result = ExchangeFormat.Read(json);

        Assert.True(result.IsSuccess);
        var warning = Assert.Single(result.Warnings);
        Assert.Equal(($"plans[0].{field}", "unknown_field"), (warning.Path, warning.Code));
    }
}
