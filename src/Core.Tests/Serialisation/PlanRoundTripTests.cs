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
        var main = Assert.Single(plan.Tracks);
        Assert.Equal(3, main.Phases.Count);
        Assert.Equal(24, main.Phases[0].Goals[0].WeightKg);
        Assert.Equal("a-lower", main.Phases[1].SessionOverrides[0].SessionId);
        var squat = plan.Sessions[0].Exercises[0];
        Assert.Equal((8, 12, "Chest tall"), (squat.Reps, squat.RepsMax, squat.Cue));
        Assert.Equal(DefaultDay.Wednesday, plan.Sessions[1].DefaultDay);
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

    [Fact]
    public void Two_track_plan_with_daily_mobility_track_round_trips_to_identical_json_and_values()
    {
        var doc = new ExchangeDocument(
            FormatVersion: ExchangeFormat.CurrentVersion,
            ExportedAt: null,
            ExportedFrom: null,
            Exercises: [PlanTestData.Exercise("goblet-squat"), PlanTestData.Exercise("cat-cow", LogKind.TickOnly)],
            Settings: null,
            Plans:
            [
                new Plan(
                    Id: "hyrox-2027",
                    Name: "HYROX London",
                    IsActive: true,
                    StartDate: new DateOnly(2026, 10, 19),
                    Goal: "HYROX London Doubles",
                    GoalDate: new DateOnly(2027, 4, 24),
                    WeightIncrementKg: 2,
                    Tracks:
                    [
                        new Track("main", "Main", new TrackRhythm.Weekly(5, 4, false),
                        [
                            new Phase("foundation", "Foundation", 1, 8, [new PhaseGoal("goblet-squat", 24, 10)], []),
                        ]),
                        new Track("mobility", "Morning mobility", new TrackRhythm.Daily(75),
                        [
                            new Phase("mobility-habit", "Habit", 1, 4, [], []),
                        ]),
                    ],
                    Sessions:
                    [
                        new Session("a-lower", "A: Lower + core", "main", DefaultDay.Monday, SessionType.Strength, null,
                            [new SessionExercise("goblet-squat", 3, 8, 12, "Chest tall")]),
                        new Session("m-mobility", "Morning mobility", "mobility", DefaultDay.Daily, SessionType.Mobility, null,
                            [new SessionExercise("cat-cow", 1, null, null, "Slow")]),
                    ]),
            ]);

        var written = ExchangeFormat.Write(doc);
        var result = ExchangeFormat.Read(written);

        Assert.True(result.IsSuccess, string.Join("; ", result.Errors.Select(e => $"{e.Path}: {e.Message}")));
        Assert.Empty(result.Warnings);
        Assert.Equal(2, result.Document!.FormatVersion);
        Assert.Equal(written, ExchangeFormat.Write(result.Document));

        var plan = Assert.Single(result.Document.Plans!);
        Assert.Equal(2, plan.Tracks.Count);
        Assert.Equal(new TrackRhythm.Weekly(5, 4, false), plan.Tracks[0].Rhythm);
        Assert.Equal(new TrackRhythm.Daily(75), plan.Tracks[1].Rhythm);
        Assert.Single(plan.Tracks[1].Phases);
        Assert.Equal("mobility-habit", plan.Tracks[1].Phases[0].Id);
        Assert.Equal(("mobility", DefaultDay.Daily, SessionType.Mobility),
            (plan.Sessions[1].TrackId, plan.Sessions[1].DefaultDay, plan.Sessions[1].Type));
        Assert.Contains("\"cadence\":\"daily\"", written);
        Assert.Contains("\"dayDonePercent\":75", written);
        Assert.Contains("\"defaultDay\":\"daily\"", written);
        Assert.Contains("\"type\":\"mobility\"", written);
    }
}
