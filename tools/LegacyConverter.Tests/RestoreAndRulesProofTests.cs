using Core.Exchange;
using Core.Model;
using Core.Rules.Phases;
using Core.Rules.Tracks;
using Core.Rules.Weeks;
using Core.Serialisation;
using Core.Storage;

namespace LegacyConverter.Tests;

public sealed class RestoreAndRulesProofTests
{
    private sealed class RecordingBackupSink : IBackupSink
    {
        public List<BackupFile> Files { get; } = [];

        public Task<bool> DeliverAsync(BackupFile file)
        {
            Files.Add(file);
            return Task.FromResult(true);
        }
    }

    private static LegacyFolder BuildSyntheticFolder()
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
            | B | Upper + shoulders | Tue | strength |
            | C | Engine + core | Wed | cardio |
            | D | Full body | Thu | strength |
            | E | Long session | Sat | cardio |
            | W | Weigh-in | Sun | body |
            | T | Benchmark test | Every 4 weeks | test |

            ## Exercises
            | Session | Exercise | Sets × reps | Log as | Cue |
            | --- | --- | --- | --- | --- |
            | M | Cat-cow | × 8 slow | - | Mobilises spine |
            | M | Bird dog | × 5 per side, 3 s hold | - | Stabilisers |
            | A | Goblet squat | 3 × 8–10 | load | Chest up |
            | B | Seated cable row | 3 × 10 | load | Elbows to ribs |
            | C, T | Longest continuous run | in one go | mins | Unbroken |
            | D | KB or trap-bar deadlift | 3 × 8 | load | Light |
            | E | Easy cardio | 30–45 min | mins | Chatting pace |
            | W | Bodyweight | weekly | kg | Morning |
            | W | Waist | monthly | cm | Belly button |
            | T | SkiErg 1000 m | 1 all-out | time | Log as m:ss |

            ## Badges
            | Badge | Exercise | Target |
            | --- | --- | --- |
            | First rep | sessions | 1 |
            | Mobility week | mobility streak | 7 |
            | 50 mornings | mobility days | 50 |
            | Ski sub-4:30 | SkiErg 1000 m | 4:30 |
            | Bodyweight deadlift | KB or trap-bar deadlift | bodyweight |
            """;

        var guides = new Dictionary<string, string>
        {
            ["Goblet squat.md"] = """
                ---
                type: guide
                exercises:
                  - "Goblet squat"
                ---
                **Works:** quads, glutes, core.
                **Why it's in your plan:** squat pattern.
                ## Setup
                Hold dumbbell at chest.
                ## Watch-outs for you
                - Knees: keep pain-free.
                ## My notes
                - 
                """,
            ["Morning mobility routine.md"] = """
                ---
                type: guide
                exercises:
                  - "Cat-cow"
                  - "Bird dog"
                ---
                **Why it's in your plan:** daily spine prep.
                """,
            ["Measuring.md"] = """
                ---
                type: guide
                exercises:
                  - "Bodyweight"
                  - "Waist"
                ---
                **Why it's in your plan:** track progress.
                """,
        };

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

        return new LegacyFolder(config, guides, logs, lifeSettings, weightsCsv);
    }

    [Fact]
    public void Convert_IsByteIdenticalAcrossRuns()
    {
        var folder = BuildSyntheticFolder();
        var json1 = ExchangeFormat.Write(LegacyConverter.Convert(folder).Document, indented: true);
        var json2 = ExchangeFormat.Write(LegacyConverter.Convert(folder).Document, indented: true);

        Assert.Equal(json1, json2);
    }

    [Fact]
    public async Task Convert_RestoresThroughRealImporterAndRunsCoreRules()
    {
        var folder = BuildSyntheticFolder();
        var converted = LegacyConverter.Convert(folder);
        var json = ExchangeFormat.Write(converted.Document, indented: true);

        var inspect = ImportInput.Inspect(json);
        Assert.True(inspect.IsAccepted, string.Join("; ", inspect.Rejections));
        Assert.Equal(ImportKind.Restore, inspect.Inspected!.SuggestedKind);

        var now = new DateTimeOffset(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);
        var store = new InMemoryDataStore();
        var appData = await AppData.OpenAsync(store);
        var backupSink = new RecordingBackupSink();

        var preview1 = ImportPreviewer.Preview(appData, inspect.Inspected, ImportKind.Restore, ImportDecisions.None, now);
        Assert.True(preview1.IsReady);

        var outcome1 = await ImportApplier.ApplyAsync(appData, preview1, backupSink, now);
        Assert.IsType<ApplyOutcome.Applied>(outcome1);

        var snapshot1 = appData.Snapshot;
        Assert.Empty(ExchangeFormat.Validate(converted.Document));
        Assert.Equal(converted.Document.Exercises!.Count, snapshot1.Exercises.Count);
        Assert.Equal(converted.Document.Workouts!.Count, snapshot1.Workouts.Count);
        Assert.Equal(converted.Document.WeighIns!.Count, snapshot1.WeighIns.Count);

        // Second restore produces identical snapshot counts
        var preview2 = ImportPreviewer.Preview(appData, inspect.Inspected, ImportKind.Restore, ImportDecisions.None, now);
        Assert.True(preview2.IsReady);
        var outcome2 = await ImportApplier.ApplyAsync(appData, preview2, backupSink, now);
        Assert.IsType<ApplyOutcome.Applied>(outcome2);
        Assert.Equal(snapshot1.Workouts.Count, appData.Snapshot.Workouts.Count);

        // Core rules over restored snapshot
        var plan = appData.Snapshot.Plans.Single(p => p.IsActive);
        var mainTrack = plan.Tracks[0];
        var mobilityTrack = plan.Tracks[1];
        var today = new DateOnly(2026, 11, 4);

        var phase = PhaseRules.CurrentPhase(plan, mainTrack, today);
        Assert.NotNull(phase);
        Assert.Equal("foundation", phase.Id);

        var mainStreak = WeekRules.Streak(plan, mainTrack, appData.Snapshot.Workouts, appData.Snapshot.Breaks, today);
        Assert.Equal(0, mainStreak.Current);

        var mobilityStreak = DailyTrackRules.Status(plan, mobilityTrack, appData.Snapshot.Workouts, appData.Snapshot.Breaks, today);
        Assert.Equal(0, mobilityStreak.DaysDone);
    }
}
