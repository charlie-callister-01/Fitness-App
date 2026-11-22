using Core.Model;

namespace LegacyConverter.Tests;

public sealed class ExerciseConversionTests
{
    [Theory]
    [InlineData("Goblet squat", "goblet-squat")]
    [InlineData("KB or trap-bar deadlift", "kb-or-trap-bar-deadlift")]
    [InlineData("SkiErg 1000 m", "skierg-1000-m")]
    [InlineData("Glute bridge + bird dog", "glute-bridge-bird-dog")]
    [InlineData("glute_bridge_bird_dog", "glute-bridge-bird-dog")]
    [InlineData("Run/walk intervals", "run-walk-intervals")]
    [InlineData("Waist \u22123 cm", "waist-3-cm")]
    [InlineData("Ski sub-4:30", "ski-sub-4-30")]
    public void ToId_MatchesGoldenList(string input, string expectedId)
    {
        Assert.Equal(expectedId, LegacyIds.ToId(input));
    }

    [Theory]
    [InlineData("load", LogKind.Load)]
    [InlineData("carry", LogKind.Carry)]
    [InlineData("reps", LogKind.Reps)]
    [InlineData("secs", LogKind.Seconds)]
    [InlineData("mins", LogKind.Minutes)]
    [InlineData("km", LogKind.DistanceKm)]
    [InlineData("m", LogKind.DistanceM)]
    [InlineData("time", LogKind.Time)]
    [InlineData("kg", LogKind.BodyMeasurementKg)]
    [InlineData("cm", LogKind.BodyMeasurementCm)]
    [InlineData("text", LogKind.Text)]
    [InlineData("-", LogKind.TickOnly)]
    public void MapLogKind_MapsEverySupportedKind(string raw, LogKind expected)
    {
        Assert.Equal(expected, ExerciseBuilder.MapLogKind(raw, "Test exercise"));
    }

    [Fact]
    public void MapLogKind_ThrowsOnUnknownKind()
    {
        Assert.Throws<FormatException>(() => ExerciseBuilder.MapLogKind("watts", "Bike"));
    }

    [Fact]
    public void Convert_BuildsExercisesWithBenchmarksRetiredAndBodyweightDropped()
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

            ## Sessions
            | Code | Name | Day | Type |
            | --- | --- | --- | --- |
            | A | Lower + core | Mon | strength |
            | T | Benchmark test | Every 4 weeks | test |

            ## Exercises
            | Session | Exercise | Sets × reps | Log as | Cue |
            | --- | --- | --- | --- | --- |
            | A | Goblet squat | 3 × 8–10 | load | Chest up |
            | C, E, T | Longest continuous run | in one go | mins | Unbroken |
            | W | Bodyweight | weekly | kg | Morning |
            | T | SkiErg 1000 m | 1 all-out | time | Log as m:ss |

            ## Badges
            | Badge | Exercise | Target |
            | --- | --- | --- |
            | First rep | sessions | 1 |
            """;

        var folder = new LegacyFolder(
            config,
            new Dictionary<string, string>(),
            new Dictionary<string, string>());

        var result = LegacyConverter.Convert(folder);

        // 3 active (Bodyweight dropped) + 3 retired = 6
        Assert.Equal(6, result.Document.Exercises!.Count);
        Assert.DoesNotContain(result.Document.Exercises, e => e.Id == "bodyweight");

        var squat = result.Document.Exercises.Single(e => e.Id == "goblet-squat");
        Assert.Equal(LogKind.Load, squat.LogKind);
        Assert.False(squat.IsBenchmark);

        var run = result.Document.Exercises.Single(e => e.Id == "longest-continuous-run");
        Assert.Equal(LogKind.Minutes, run.LogKind);
        Assert.True(run.IsBenchmark);

        var ski = result.Document.Exercises.Single(e => e.Id == "skierg-1000-m");
        Assert.Equal(LogKind.Time, ski.LogKind);
        Assert.True(ski.IsBenchmark);

        Assert.Contains(result.Document.Exercises, e => e.Id == "chest-supported-row" && e.LogKind == LogKind.Load);
        Assert.Contains(result.Document.Exercises, e => e.Id == "farmers-carry" && e.LogKind == LogKind.Carry);
        Assert.Contains(result.Document.Exercises, e => e.Id == "incline-db-press" && e.LogKind == LogKind.Load);

        Assert.Contains(result.Notes, n => n.Kind == ConversionNoteKind.Dropped && n.Subject == "Exercise: Bodyweight");
    }
}
