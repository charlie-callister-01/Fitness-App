using Core.Model;

namespace LegacyConverter.Tests;

public sealed class PlanConversionTests
{
    [Theory]
    [InlineData("3 × 8–10", 3, 8, 10, null)]
    [InlineData("3 × 10", 3, 10, null, null)]
    [InlineData("2 × 8 per side", 2, 8, null, "per side")]
    [InlineData("3 × 12–15 per side", 3, 12, 15, "per side")]
    [InlineData("× 8 slow", 1, 8, null, "slow")]
    [InlineData("× 5 per side, 3 s hold", 1, 5, null, "per side, 3 s hold")]
    [InlineData("3 × 20–30 s per side", 3, null, null, "20–30 s per side")]
    [InlineData("5 min", 1, null, null, "5 min")]
    [InlineData("20–25 min", 1, null, null, "20–25 min")]
    [InlineData("3 rounds of 6/4/2", 3, null, null, "3 rounds of 6/4/2")]
    [InlineData("2 × 10 / 2 × 6 per side", 1, null, null, "2 × 10 / 2 × 6 per side")]
    [InlineData("6–10 × 30 s hard", 1, null, null, "6–10 × 30 s hard")]
    public void ParseSetsReps_ParsesKnownForms(
        string raw,
        int expectedSets,
        int? expectedReps,
        int? expectedRepsMax,
        string? expectedCue)
    {
        var parsed = PlanBuilder.ParseSetsReps(raw, existingCue: null);
        Assert.Equal(expectedSets, parsed.Sets);
        Assert.Equal(expectedReps, parsed.Reps);
        Assert.Equal(expectedRepsMax, parsed.RepsMax);
        Assert.Equal(expectedCue, parsed.Cue);
    }

    [Fact]
    public void ParseSetsReps_ThrowsOnUnreadableInput()
    {
        Assert.Throws<FormatException>(() => PlanBuilder.ParseSetsReps("do some stuff", null));
    }

    [Fact]
    public void Convert_BuildsPlanWithTracksPhasesSessionsAndDropsW()
    {
        var config = """
            ## Settings
            - start: 2026-10-19
            - race: 2027-04-24
            - race name: HYROX London (Doubles)
            - sessions per week: 5
            - streak min: 4
            - increment kg: 2
            - mobility done percent: 75

            ## Phases
            | Phase | From week | To week |
            | --- | --- | --- |
            | Foundation | 1 | 8 |
            | Build | 9 | 19 |
            | HYROX prep | 20 | 27 |

            ## Sessions
            | Code | Name | Day | Type |
            | --- | --- | --- | --- |
            | M | Morning mobility | Daily | mobility |
            | A | Lower + core | Mon | strength |
            | W | Weigh-in | Sun | body |
            | T | Benchmark test | Every 4 weeks | test |

            ## Exercises
            | Session | Exercise | Sets × reps | Log as | Cue |
            | --- | --- | --- | --- | --- |
            | M | Cat-cow | × 8 slow | - | Mobilises spine |
            | A | Reverse lunge | 2 × 8 per side | load | Start bodyweight |
            | W | Bodyweight | weekly | kg | Morning |
            | T | SkiErg 1000 m | 1 all-out | time | Log as m:ss |

            ## Badges
            | Badge | Exercise | Target |
            | --- | --- | --- |
            | First rep | sessions | 1 |
            """;

        var result = LegacyConverter.Convert(new LegacyFolder(
            config,
            new Dictionary<string, string>(),
            new Dictionary<string, string>()));

        var settings = result.Document.Settings!;
        Assert.Equal(4, settings.StreakMinWorkoutsPerWeek);
        Assert.False(settings.StreakCountsCustomWorkouts);
        Assert.Equal(2, settings.WeightIncrementKg);
        Assert.Null(settings.GoalWeightKg);

        var plan = Assert.Single(result.Document.Plans!);
        Assert.Equal("hyrox-2027", plan.Id);
        Assert.Equal("HYROX London", plan.Name);
        Assert.True(plan.IsActive);
        Assert.Equal(new DateOnly(2026, 10, 19), plan.StartDate);
        Assert.Equal("HYROX London Doubles", plan.Goal);
        Assert.Equal(new DateOnly(2027, 4, 24), plan.GoalDate);

        Assert.Equal(2, plan.Tracks.Count);
        var main = plan.Tracks[0];
        Assert.Equal("main", main.Id);
        Assert.Equal(new TrackRhythm.Weekly(5, 4, false), main.Rhythm);
        Assert.Equal(3, main.Phases.Count);
        Assert.Equal(["foundation", "build", "hyrox-prep"], main.Phases.Select(p => p.Id));

        var mobility = plan.Tracks[1];
        Assert.Equal("mobility", mobility.Id);
        Assert.Equal(new TrackRhythm.Daily(75), mobility.Rhythm);
        Assert.Empty(mobility.Phases);

        Assert.Equal(3, plan.Sessions.Count);
        Assert.DoesNotContain(plan.Sessions, s => s.Id.StartsWith("w", StringComparison.OrdinalIgnoreCase));

        var mobilitySession = plan.Sessions.Single(s => s.Id == "m-mobility");
        Assert.Equal("mobility", mobilitySession.TrackId);
        Assert.Equal(DefaultDay.Daily, mobilitySession.DefaultDay);
        Assert.Equal(SessionType.Mobility, mobilitySession.Type);

        var lower = plan.Sessions.Single(s => s.Id == "a-lower");
        Assert.Equal("main", lower.TrackId);
        Assert.Equal(DefaultDay.Monday, lower.DefaultDay);
        var lungeRow = Assert.Single(lower.Exercises);
        Assert.Equal(2, lungeRow.Sets);
        Assert.Equal(8, lungeRow.Reps);
        Assert.Contains("per side", lungeRow.Cue);

        var benchmark = plan.Sessions.Single(s => s.Id == "benchmark");
        Assert.Equal("main", benchmark.TrackId);
        Assert.Equal(DefaultDay.Friday, benchmark.DefaultDay);
        Assert.Equal(4, benchmark.BenchmarkIntervalWeeks);

        Assert.Contains(result.Notes, n => n.Kind == ConversionNoteKind.Dropped && n.Subject.Contains("W (Weigh-in)"));
        Assert.Contains(result.Notes, n => n.Kind == ConversionNoteKind.Guessed && n.Subject.Contains("Benchmark"));
    }
}
