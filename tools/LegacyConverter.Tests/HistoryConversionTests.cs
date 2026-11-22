using Core.Model;
using Core.Rules.Tracks;

namespace LegacyConverter.Tests;

public sealed class HistoryConversionTests
{
    private const string SampleConfig = """
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
        | M | Morning mobility | Daily | mobility |
        | A | Lower + core | Mon | strength |
        | B | Upper + shoulders | Tue | strength |
        | C | Engine + core | Wed | cardio |
        | W | Weigh-in | Sun | body |
        | T | Benchmark test | Every 4 weeks | test |

        ## Exercises
        | Session | Exercise | Sets × reps | Log as | Cue |
        | --- | --- | --- | --- | --- |
        | M | Cat-cow | × 8 slow | - | Spine |
        | M | Bird dog | × 5 per side, 3 s hold | - | Back |
        | A | Warm-up | 8 min | - | Drills |
        | A | Goblet squat | 3 × 8–10 | load | Chest up |
        | A | Reverse lunge | 2 × 8 per side | load | Bodyweight |
        | A | Side plank | 3 × 20–30 s per side | secs | Knees if needed |
        | B | Seated cable row | 3 × 10 | load | Neutral |
        | C, T | Longest continuous run | in one go | mins | Unbroken |
        | W | Bodyweight | weekly | kg | Morning |
        | W | Waist | monthly | cm | Belly button |
        | T | SkiErg 1000 m | 1 all-out | time | Log as m:ss |

        ## Badges
        | Badge | Exercise | Target |
        | --- | --- | --- |
        | First rep | sessions | 1 |
        """;

    [Fact]
    public void Convert_ParsesWeighInsWorkoutsValuesAndSkipsEmptyLogs()
    {
        var logs = new Dictionary<string, string>
        {
            ["2026-10-18 W.md"] = """
                ---
                type: log
                session: W
                date: 2026-10-18
                bodyweight: 86.5
                waist: 90
                done: []
                ---
                """,
            ["2026-10-27 M.md"] = """
                ---
                type: log
                session: M
                date: 2026-10-27
                done:
                  - cat_cow
                ---
                """,
            ["2026-11-02 A.md"] = """
                ---
                type: log
                session: A
                date: 2026-11-02
                goblet_squat: 12×10
                reverse_lunge: bwx8
                side_plank: 2.3
                done:
                  - warm_up
                ---
                """,
            ["2026-11-03 B.md"] = """
                ---
                type: log
                session: B
                date: 2026-11-03
                seated_cable_row: 35 × 10
                done: []
                ---
                """,
            ["2026-11-04 C.md"] = """
                ---
                type: log
                session: "C"
                date: 2026-11-04
                longest_continuous_run:
                done: []
                ---
                """,
        };

        var lifeSettings = """
            ---
            start_weight: 113
            start_weight_date: 2026-02-01
            ---
            """;

        var weightsCsv = """
            date,kg
            2026-09-26,85.5
            """;

        var result = LegacyConverter.Convert(new LegacyFolder(
            SampleConfig,
            new Dictionary<string, string>(),
            logs,
            lifeSettings,
            weightsCsv));

        // Weigh-ins: 1 Feb (113), 26 Sep (85.5), 18 Oct (86.5)
        Assert.Equal(3, result.Document.WeighIns!.Count);
        Assert.Equal(new WeighIn("w-2026-02-01", new DateOnly(2026, 2, 1), 113), result.Document.WeighIns[0]);
        Assert.Equal(new WeighIn("w-2026-09-26", new DateOnly(2026, 9, 26), 85.5), result.Document.WeighIns[1]);
        Assert.Equal(new WeighIn("w-2026-10-18", new DateOnly(2026, 10, 18), 86.5), result.Document.WeighIns[2]);

        // Workouts: W, M, A, B (C is empty shell -> dropped)
        Assert.Equal(4, result.Document.Workouts!.Count);
        Assert.All(result.Document.Workouts, w => Assert.True(w.LoggedAfterwards));
        Assert.Contains(result.Notes, n => n.Kind == ConversionNoteKind.Dropped && n.Subject == "Log: 2026-11-04 C.md");

        var wWorkout = result.Document.Workouts.Single(w => w.Id == "w-2026-10-18-w");
        Assert.Null(wWorkout.SessionId);
        Assert.Empty(wWorkout.Planned);
        var waistEntry = Assert.Single(wWorkout.Entries);
        Assert.Equal("waist", waistEntry.ExerciseId);
        Assert.Equal(90, waistEntry.Sets[0].MeasurementCm);

        var mWorkout = result.Document.Workouts.Single(w => w.Id == "w-2026-10-27-m");
        Assert.Equal("m-mobility", mWorkout.SessionId);
        Assert.Equal(2, mWorkout.Planned.Count);
        var catCowEntry = Assert.Single(mWorkout.Entries);
        Assert.Equal("cat-cow", catCowEntry.ExerciseId);
        Assert.Empty(catCowEntry.Sets);
        // 1 of 2 rows = 50% < 75% -> not a done day
        var plan = result.Document.Plans![0];
        var mobilityTrack = plan.Tracks[1];
        Assert.False(DailyTrackRules.IsDayDone(plan, mobilityTrack, result.Document.Workouts!, new DateOnly(2026, 10, 27)));

        var aWorkout = result.Document.Workouts.Single(w => w.Id == "w-2026-11-02-a");
        Assert.Equal("a-lower", aWorkout.SessionId);
        Assert.Equal(4, aWorkout.Planned.Count);
        var squat = aWorkout.Entries.Single(e => e.ExerciseId == "goblet-squat");
        Assert.Equal(12, squat.Sets[0].WeightKg);
        Assert.Equal(10, squat.Sets[0].Reps);
        var lunge = aWorkout.Entries.Single(e => e.ExerciseId == "reverse-lunge");
        Assert.Equal(0, lunge.Sets[0].WeightKg);
        Assert.Equal(8, lunge.Sets[0].Reps);
        var sidePlank = aWorkout.Entries.Single(e => e.ExerciseId == "side-plank");
        Assert.Equal(2, sidePlank.Sets.Count);
        Assert.All(sidePlank.Sets, s => Assert.Equal(30, s.DurationSeconds));

        var bWorkout = result.Document.Workouts.Single(w => w.Id == "w-2026-11-03-b");
        var row = Assert.Single(bWorkout.Entries);
        Assert.Equal(35, row.Sets[0].WeightKg);
        Assert.Equal(10, row.Sets[0].Reps);
    }
}
