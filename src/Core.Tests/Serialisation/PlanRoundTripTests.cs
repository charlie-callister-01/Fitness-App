using Core.Model;
using Core.Serialisation;

namespace Core.Tests.Serialisation;

public class PlanRoundTripTests
{
    [Fact]
    public void Rich_plan_round_trips_to_identical_json()
    {
        var written = PlanTestData.RichJson();

        var result = ExchangeFormat.Read(written);

        Assert.True(result.IsSuccess, string.Join("; ", result.Errors.Select(e => e.Path + " " + e.Message)));
        Assert.Empty(result.Warnings);
        Assert.Equal(written, ExchangeFormat.Write(result.Document!));
    }

    [Fact]
    public void Round_trip_preserves_plan_values()
    {
        var plan = ExchangeFormat.Read(PlanTestData.RichJson()).Document!.Plans![0];

        Assert.Equal(new DateOnly(2027, 4, 24), plan.GoalDate);
        Assert.Equal(3, plan.Phases.Count);
        Assert.Equal(24, plan.Phases[0].Goals[0].WeightKg);
        Assert.Equal("a-lower", plan.Phases[1].SessionOverrides[0].SessionId);
        var squat = plan.Sessions[0].Exercises[0];
        Assert.Equal((8, 12, "Chest tall"), (squat.Reps, squat.RepsMax, squat.Cue));
        Assert.Equal(DayOfWeek.Wednesday, plan.Sessions[1].DefaultDay);
        Assert.Equal(4, plan.Sessions[2].BenchmarkIntervalWeeks);
    }

    [Fact]
    public void Output_uses_iso_dates_and_camelCase_enums()
    {
        var json = PlanTestData.RichJson();

        Assert.Contains("\"startDate\":\"2026-10-19\"", json);
        Assert.Contains("\"defaultDay\":\"monday\"", json);
        Assert.Contains("\"type\":\"test\"", json);
    }

    [Fact]
    public void Plan_only_document_is_accepted_without_the_exercise_library()
    {
        var doc = PlanTestData.Rich() with { Exercises = null };

        var result = ExchangeFormat.Read(ExchangeFormat.Write(doc));

        Assert.True(result.IsSuccess, string.Join("; ", result.Errors.Select(e => e.Path + " " + e.Message)));
        Assert.Null(result.Document!.Exercises);
        Assert.Equal(2, result.Document.Plans!.Count);
    }

    [Fact]
    public void Document_without_plans_has_none()
    {
        var result = ExchangeFormat.Read(TestData.MinimalJson());

        Assert.Null(result.Document!.Plans);
    }
}
