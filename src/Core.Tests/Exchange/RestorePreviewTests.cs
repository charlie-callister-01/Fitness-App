using Core.Exchange;
using Core.Model;
using Core.Serialisation;
using Core.Storage;
using Core.Tests.Serialisation;

namespace Core.Tests.Exchange;

public class RestorePreviewTests
{
    private static readonly DateTimeOffset Now = new(2026, 12, 1, 8, 0, 0, TimeSpan.Zero);

    private static ExchangeDocument Backup() => WholeDatasetTests.Everything();

    private static Inspected Incoming(ExchangeDocument document)
    {
        var result = ImportInput.Inspect(ExchangeFormat.Write(document));
        Assert.True(result.IsAccepted, string.Join("; ", result.Rejections));
        return result.Inspected!;
    }

    private static ImportPreview Restore(DataSnapshot current, ExchangeDocument file, DateTimeOffset? now = null, int setAside = 0) =>
        ImportPreviewer.Preview(current, Incoming(file), ImportKind.Restore, ImportDecisions.None, now ?? Now, setAside);

    [Fact]
    public void A_complete_backup_is_ready_and_carries_the_document_and_the_file_time_as_the_stamp()
    {
        var file = Backup();

        var preview = Restore(DataSnapshot.Empty, file);

        Assert.True(preview.IsReady, string.Join("; ", preview.Blockers.Select(b => b.Text)));
        Assert.Empty(preview.Changes);
        Assert.Equal(file.ExportedAt, preview.BackupStamp);
        Assert.Equal(ExchangeFormat.Write(file), ExchangeFormat.Write(preview.Replacement!));
    }

    [Fact]
    public void A_partial_file_is_not_a_backup_and_names_what_is_missing()
    {
        var file = Backup() with { Guides = null, Settings = null };

        var preview = Restore(DataSnapshot.Empty, file);

        Assert.False(preview.IsReady);
        var blocker = Assert.Single(preview.Blockers);
        Assert.Equal("not_a_backup", blocker.Key);
        Assert.Equal("This file is not a complete Backup, so it cannot replace your data. It is missing: settings, guides.", blocker.Text);
        Assert.Null(preview.Replacement);
    }

    [Fact]
    public void A_backup_without_week_schedules_is_still_complete()
    {
        var preview = Restore(DataSnapshot.Empty, Backup() with { WeekSchedules = null });

        Assert.True(preview.IsReady, string.Join("; ", preview.Blockers.Select(b => b.Text)));
    }

    [Fact]
    public void Without_an_exported_at_the_stamp_is_the_time_the_caller_gave()
    {
        var preview = Restore(DataSnapshot.Empty, Backup() with { ExportedAt = null });

        Assert.Equal(Now, preview.BackupStamp);
    }

    private static Workout WorkoutOn(string id, DateOnly date) => Backup().Workouts![0] with { Id = id, Date = date };

    [Fact]
    public void The_preview_says_what_will_be_lost()
    {
        var file = Backup();
        var current = DataSnapshot.From(file) with
        {
            Workouts = [.. file.Workouts!, WorkoutOn("w-new-1", new DateOnly(2026, 11, 20)), WorkoutOn("w-new-2", new DateOnly(2026, 12, 1)), WorkoutOn("w-old", new DateOnly(2026, 1, 5))],
            WeighIns = [.. file.WeighIns!, new WeighIn("wi-new", new DateOnly(2026, 12, 1), 80)],
        };

        var preview = Restore(current, file, setAside: 2);

        Assert.True(preview.IsReady);
        Assert.Equal(
            [
                "Replaces all your data with this Backup.",
                "Loses 3 Workouts and 1 Weigh-in that are not in the file.",
                "2 of your Workouts are dated after this Backup was made (16 Nov 2026).",
                "Removes 2 records that could not be read and were set aside.",
            ],
            preview.Summary);
        Assert.Equal(
            [ImportItemAction.Warning, ImportItemAction.Warning, ImportItemAction.Warning],
            preview.Items.Select(i => i.Action));
    }

    [Fact]
    public async Task From_a_start_that_could_not_be_read_the_preview_restores_and_stamps_in_one_write()
    {
        var store = new InMemoryDataStore();
        await store.ReplaceAllAsync(StoredRecords.FromDocument(Backup()), new StoreMetadata(null, null, ExchangeFormat.CurrentVersion + 1, true));
        var app = await AppData.OpenAsync(store);
        Assert.Equal(StartupState.CannotRead, app.Startup.State);
        var file = Backup();

        var preview = Restore(app.Snapshot, file, setAside: app.Startup.Issues.Count(i => i.Kind is not null));
        var result = await app.ReplaceAllAsync(preview.Replacement!, preview.BackupStamp);

        Assert.True(preview.IsReady);
        Assert.IsType<CommitResult.Committed>(result);
        Assert.Equal(StartupState.Ready, app.Startup.State);
        Assert.Equal(file.ExportedAt, app.LastBackupAt);
        Assert.Equal(file.Workouts!.Count, app.Snapshot.Workouts.Count);
    }

    [Fact]
    public void Nothing_lost_adds_no_loss_lines()
    {
        var file = Backup();

        var preview = Restore(DataSnapshot.From(file), file);

        Assert.Equal(["Replaces all your data with this Backup."], preview.Summary);
        Assert.Empty(preview.Items);
    }

    [Fact]
    public void A_v1_backup_is_ready_and_upgrades_plans_into_a_main_track()
    {
        const string v1Backup = """
            {
              "formatVersion": 1,
              "exportedAt": "2026-11-16T09:30:00+00:00",
              "exportedFrom": "fitness-app 0.1",
              "exercises": [
                { "id": "goblet-squat", "name": "Goblet squat", "logKind": "load" }
              ],
              "settings": {
                "displayWeightUnit": "kg",
                "displayDistanceUnit": "km",
                "weightIncrementKg": 2.5,
                "restTimerSeconds": 90,
                "streakMinWorkoutsPerWeek": 3,
                "streakCountsCustomWorkouts": false
              },
              "plans": [
                {
                  "id": "v1-plan",
                  "name": "Legacy Plan",
                  "isActive": true,
                  "startDate": "2026-10-19",
                  "weeklyTarget": 3,
                  "streakMinWorkoutsPerWeek": 2,
                  "streakCountsCustomWorkouts": false,
                  "weightIncrementKg": 2.5,
                  "phases": [{ "id": "ph1", "focus": "Base", "startWeek": 1, "endWeek": 4 }],
                  "sessions": [
                    {
                      "id": "s1",
                      "name": "Session 1",
                      "defaultDay": "monday",
                      "type": "strength",
                      "exercises": [{ "exerciseId": "goblet-squat", "sets": 3, "reps": 8 }]
                    }
                  ]
                }
              ],
              "weighIns": [],
              "breaks": [],
              "badges": [],
              "guides": [],
              "workouts": [],
              "weekSchedules": []
            }
            """;

        var inspected = ImportInput.Inspect(v1Backup);
        Assert.True(inspected.IsAccepted, string.Join("; ", inspected.Rejections));
        Assert.Equal(ImportKind.Restore, inspected.Inspected!.SuggestedKind);

        var preview = ImportPreviewer.Preview(DataSnapshot.Empty, inspected.Inspected!, ImportKind.Restore, ImportDecisions.None, Now);

        Assert.True(preview.IsReady, string.Join("; ", preview.Blockers.Select(b => b.Text)));
        var plan = Assert.Single(preview.Replacement!.Plans!);
        var track = Assert.Single(plan.Tracks);
        Assert.Equal("v1-plan-main", track.Id);
        Assert.Equal("v1-plan-main", Assert.Single(plan.Sessions).TrackId);
    }
}
