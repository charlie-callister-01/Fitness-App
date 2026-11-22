using Core.Exchange;
using Core.Model;
using Core.Serialisation;
using Core.Storage;
using Core.Tests.Serialisation;
using Core.Tests.Storage;

namespace Core.Tests.Exchange;

public class ApplyTests
{
    private static readonly DateTimeOffset Now = ExchangeTestData.Now;

    private static Inspected Incoming(ExchangeDocument document)
    {
        var result = ImportInput.Inspect(ExchangeFormat.Write(document));
        Assert.True(result.IsAccepted, string.Join("; ", result.Rejections));
        return result.Inspected!;
    }

    private static ImportPreview Preview(AppData app, ExchangeDocument file, ImportKind kind) =>
        ImportPreviewer.Preview(app, Incoming(file), kind, ImportDecisions.None, Now);

    private static async Task<AppData> OpenWith(DataSnapshot snapshot, InMemoryDataStore? store = null)
    {
        var app = await AppData.OpenAsync(store ?? new InMemoryDataStore());
        Assert.IsType<CommitResult.Committed>(await app.ReplaceAllAsync(snapshot.ToDocument()));
        return app;
    }

    private static ExchangeDocument NewWeighIn(string id = "wi-new") =>
        new(ExchangeFormat.CurrentVersion, null, null, null, null) { WeighIns = [new WeighIn(id, new DateOnly(2026, 11, 16), 81.5)] };

    [Fact]
    public async Task A_preview_that_is_not_ready_changes_nothing_and_delivers_no_backup()
    {
        var app = await OpenWith(ExchangeTestData.Snapshot());
        var notABackup = WholeDatasetTests.Everything() with { Guides = null };
        var preview = Preview(app, notABackup, ImportKind.Restore);
        var sink = new RecordingBackupSink();
        var before = app.Snapshot;

        var outcome = await ImportApplier.ApplyAsync(app, preview, sink, Now);

        Assert.IsType<ApplyOutcome.NotReady>(outcome);
        Assert.Empty(sink.Delivered);
        Assert.Same(before, app.Snapshot);
    }

    private static async Task<AppData> OpenCannotRead(InMemoryDataStore store)
    {
        await store.WriteMetadataAsync(new StoreMetadata(null, null, ExchangeFormat.CurrentVersion + 1, true));
        var app = await AppData.OpenAsync(store);
        Assert.Equal(StartupState.CannotRead, app.Startup.State);
        return app;
    }

    [Fact]
    public async Task A_stale_preview_changes_nothing_and_delivers_no_backup()
    {
        var app = await OpenWith(ExchangeTestData.Snapshot());
        var preview = Preview(app, NewWeighIn(), ImportKind.Merge);
        await app.CommitAsync(Change.Put(new WeighIn("wi-other", new DateOnly(2026, 11, 10), 80)));
        var sink = new RecordingBackupSink();
        var before = app.Snapshot;

        var outcome = await ImportApplier.ApplyAsync(app, preview, sink, Now);

        Assert.IsType<ApplyOutcome.PreviewOutdated>(outcome);
        Assert.Empty(sink.Delivered);
        Assert.Same(before, app.Snapshot);
    }

    [Fact]
    public async Task A_sink_that_fails_stops_the_import_with_nothing_changed_and_no_backup_date()
    {
        var app = await OpenWith(ExchangeTestData.Snapshot());
        var preview = Preview(app, NewWeighIn(), ImportKind.Merge);
        var sink = new RecordingBackupSink { Succeeds = false };
        var before = app.Snapshot;

        var outcome = await ImportApplier.ApplyAsync(app, preview, sink, Now);

        Assert.IsType<ApplyOutcome.BackupNotSaved>(outcome);
        Assert.Same(before, app.Snapshot);
        Assert.Null(app.LastBackupAt);
    }

    [Fact]
    public async Task A_commit_the_rules_reject_leaves_the_data_unchanged_and_the_backup_stamped()
    {
        var app = await OpenWith(ExchangeTestData.Snapshot());
        var bad = ExchangeTestData.OldWorkout() with { Id = "w-bad", Entries = [new WorkoutEntry("no-such-exercise", EntryStatus.Done, null, null, null, [])] };
        var preview = Preview(app, NewWeighIn(), ImportKind.Merge) with { Changes = [Change.Put(bad)] };
        var before = app.Snapshot;

        var outcome = await ImportApplier.ApplyAsync(app, preview, new RecordingBackupSink(), Now);

        Assert.NotEmpty(Assert.IsType<ApplyOutcome.Rejected>(outcome).Issues);
        Assert.Same(before, app.Snapshot);
        Assert.Equal(Now, app.LastBackupAt);
    }

    [Fact]
    public async Task A_store_that_fails_the_commit_leaves_the_data_unchanged_and_the_backup_stamped()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        var app = await AppData.OpenAsync(store);
        await app.ReplaceAllAsync(ExchangeTestData.Snapshot().ToDocument());
        var preview = Preview(app, NewWeighIn(), ImportKind.Merge);
        store.FailLastOperationOfNextBatch(StoreFailureKind.StorageFull);
        var before = app.Snapshot;

        var outcome = await ImportApplier.ApplyAsync(app, preview, new RecordingBackupSink(), Now);

        Assert.Equal(StoreFailureKind.StorageFull, Assert.IsType<ApplyOutcome.StoreFailed>(outcome).Failure.Kind);
        Assert.Same(before, app.Snapshot);
        Assert.Equal(Now, app.LastBackupAt);
    }

    [Fact]
    public async Task Data_that_cannot_be_read_blocks_a_merge_and_delivers_no_backup()
    {
        var app = await OpenCannotRead(new InMemoryDataStore());
        var preview = Preview(app, NewWeighIn(), ImportKind.Merge);
        var sink = new RecordingBackupSink();

        var outcome = await ImportApplier.ApplyAsync(app, preview, sink, Now);

        Assert.Equal(StartupState.CannotRead, Assert.IsType<ApplyOutcome.Blocked>(outcome).State);
        Assert.Empty(sink.Delivered);
    }

    [Fact]
    public async Task A_restore_rescues_unreadable_data_without_a_backup_and_stamps_the_files_time()
    {
        var app = await OpenCannotRead(new InMemoryDataStore());
        var file = WholeDatasetTests.Everything();
        var sink = new RecordingBackupSink();

        var outcome = await ImportApplier.ApplyAsync(app, Preview(app, file, ImportKind.Restore), sink, Now);

        Assert.IsType<ApplyOutcome.Applied>(outcome);
        Assert.Empty(sink.Delivered);
        Assert.Equal(file.ExportedAt, app.LastBackupAt);
        Assert.Equal(StartupState.Ready, app.Startup.State);
        Assert.Equal(file.Workouts!.Count, app.Snapshot.Workouts.Count);
    }

    [Fact]
    public async Task A_restore_over_existing_data_backs_that_data_up_first()
    {
        var app = await OpenWith(ExchangeTestData.Snapshot());
        var oldWorkouts = app.Snapshot.Workouts.Count;
        var file = WholeDatasetTests.Everything();
        var sink = new RecordingBackupSink();

        var outcome = await ImportApplier.ApplyAsync(app, Preview(app, file, ImportKind.Restore), sink, Now);

        Assert.IsType<ApplyOutcome.Applied>(outcome);
        Assert.Equal(oldWorkouts, ExchangeTestData.ReadBack(Assert.Single(sink.Delivered).Text).Workouts!.Count);
        Assert.Equal(file.Workouts!.Count, app.Snapshot.Workouts.Count);
        Assert.Equal(file.ExportedAt, app.LastBackupAt);
    }

    [Fact]
    public async Task A_dataset_with_no_records_needs_no_backup()
    {
        var app = await AppData.OpenAsync(new InMemoryDataStore());
        var sink = new RecordingBackupSink();

        var outcome = await ImportApplier.ApplyAsync(app, Preview(app, NewWeighIn(), ImportKind.Merge), sink, Now);

        Assert.IsType<ApplyOutcome.Applied>(outcome);
        Assert.Empty(sink.Delivered);
        Assert.Null(app.LastBackupAt);
    }

    [Fact]
    public async Task Changed_fires_once_for_the_commit_and_once_for_the_backup_stamp()
    {
        var app = await OpenWith(ExchangeTestData.Snapshot());
        var preview = Preview(app, NewWeighIn(), ImportKind.Merge);
        var raised = 0;
        app.Changed += () => raised++;

        await ImportApplier.ApplyAsync(app, preview, new RecordingBackupSink(), Now);

        Assert.Equal(2, raised);
    }

    [Fact]
    public async Task Importing_the_same_file_twice_says_there_is_nothing_to_change()
    {
        var app = await OpenWith(ExchangeTestData.Snapshot());
        var file = NewWeighIn();
        await ImportApplier.ApplyAsync(app, Preview(app, file, ImportKind.Merge), new RecordingBackupSink(), Now);

        var again = Preview(app, file, ImportKind.Merge);

        Assert.Contains("Nothing to change.", again.Summary);
        Assert.Empty(again.Changes);
    }

    [Fact]
    public async Task A_backup_survives_a_wiped_store_and_restores_to_an_identical_dataset()
    {
        var source = await OpenWith(ExchangeTestData.Snapshot());
        var backup = Exporter.Export(source.Snapshot, ExportRequest.Backup, "test", Now);
        var wiped = await AppData.OpenAsync(new InMemoryDataStore());
        Assert.Equal(StartupState.FirstRun, wiped.Startup.State);

        var outcome = await ImportApplier.ApplyAsync(wiped, Preview(wiped, ExchangeTestData.ReadBack(backup.Text), ImportKind.Restore), new RecordingBackupSink(), Now);

        Assert.IsType<ApplyOutcome.Applied>(outcome);
        Assert.Equal(ExchangeFormat.Write(source.Snapshot.ToDocument()), ExchangeFormat.Write(wiped.Snapshot.ToDocument()));
        Assert.Equal(Now, wiped.LastBackupAt);
    }

    [Fact]
    public async Task A_restore_of_a_file_without_exportedAt_stamps_the_time_given_to_apply()
    {
        var app = await OpenWith(ExchangeTestData.Snapshot());
        var file = WholeDatasetTests.Everything() with { ExportedAt = null };
        var preview = ImportPreviewer.Preview(app.Snapshot, Incoming(file), ImportKind.Restore, ImportDecisions.None);
        var later = Now.AddDays(1);

        var outcome = await ImportApplier.ApplyAsync(app, preview, new RecordingBackupSink(), later);

        Assert.IsType<ApplyOutcome.Applied>(outcome);
        Assert.Equal(later, app.LastBackupAt);
    }

    [Fact]
    public async Task The_AppData_overload_counts_set_aside_records_and_passes_the_time_for_a_restore()
    {
        var store = new InMemoryDataStore();
        await store.ReplaceAllAsync(StoredRecords.FromDocument(ExchangeTestData.Snapshot().ToDocument()), null);
        await store.ApplyAsync([new StoreChange.Put(new StoredRecord(RecordKind.Exercise, "bad", """{"id":"bad","name":"Bad","logKind":"nonsense"}"""))]);
        var app = await AppData.OpenAsync(store);
        var file = WholeDatasetTests.Everything() with { ExportedAt = null };

        var preview = ImportPreviewer.Preview(app, Incoming(file), ImportKind.Restore, ImportDecisions.None, Now);

        Assert.Equal(Now, preview.BackupStamp);
        Assert.Contains("Removes 1 record that could not be read and was set aside.", preview.Summary);
    }

    [Fact]
    public async Task A_merge_delivers_the_pre_import_backup_first_stamps_it_then_commits()
    {
        var app = await OpenWith(ExchangeTestData.Snapshot());
        var original = app.Snapshot;
        var preview = Preview(app, NewWeighIn(), ImportKind.Merge);
        var sink = new RecordingBackupSink();
        DataSnapshot? seenAtDelivery = null;
        DateTimeOffset? stampAtDelivery = null;
        sink.OnDelivering = () => (seenAtDelivery, stampAtDelivery) = (app.Snapshot, app.LastBackupAt);
        var stampBefore = app.LastBackupAt;

        var outcome = await ImportApplier.ApplyAsync(app, preview, sink, Now);

        Assert.Equal(preview.Summary, Assert.IsType<ApplyOutcome.Applied>(outcome).Summary);
        Assert.Contains("Adds 1 Weigh-in.", preview.Summary);
        Assert.Same(original, seenAtDelivery);
        Assert.Equal(stampBefore, stampAtDelivery);
        var file = Assert.Single(sink.Delivered);
        Assert.Equal("fitness-backup-before-import-2026-11-17.json", file.FileName);
        Assert.Equal(Now, file.At);
        var inFile = ExchangeTestData.ReadBack(file.Text);
        Assert.Equal(original.WeighIns.Count, inFile.WeighIns!.Count);
        Assert.DoesNotContain(inFile.WeighIns!, w => w.Id == "wi-new");
        Assert.Equal(Now, app.LastBackupAt);
        Assert.Contains(app.Snapshot.WeighIns, w => w.Id == "wi-new");
    }

    [Fact]
    public async Task A_v2_backup_with_two_tracks_round_trips_through_export_and_restore()
    {
        var baseSnapshot = ExchangeTestData.Snapshot();
        var active = baseSnapshot.Plans.Single(p => p.IsActive);
        var twoTrack = active with
        {
            Tracks =
            [
                active.Tracks[0],
                new Track("mobility", "Morning mobility", new TrackRhythm.Daily(80), [new Phase("mob-habit", "Habit", 1, 4, [], [])]),
            ],
            Sessions =
            [
                .. active.Sessions,
                new Session("m-mobility", "Morning mobility", "mobility", DefaultDay.Daily, SessionType.Mobility, null, [new SessionExercise("goblet-squat", 1, null, null, null)]),
            ],
        };
        var snapshot = baseSnapshot with
        {
            Plans = [.. baseSnapshot.Plans.Select(p => p.IsActive ? twoTrack : p)],
            Badges = [.. baseSnapshot.Badges, new Badge("b-mob", "7 mornings", "streak:mobility", 7)],
        };
        var source = await OpenWith(snapshot);
        var backup = Exporter.Export(source.Snapshot, ExportRequest.Backup, "test", Now);
        var wiped = await AppData.OpenAsync(new InMemoryDataStore());

        var outcome = await ImportApplier.ApplyAsync(wiped, Preview(wiped, ExchangeTestData.ReadBack(backup.Text), ImportKind.Restore), new RecordingBackupSink(), Now);

        Assert.IsType<ApplyOutcome.Applied>(outcome);
        Assert.Equal(ExchangeFormat.Write(source.Snapshot.ToDocument()), ExchangeFormat.Write(wiped.Snapshot.ToDocument()));
        var restoredPlan = wiped.Snapshot.Plans.Single(p => p.IsActive);
        Assert.Equal(2, restoredPlan.Tracks.Count);
        Assert.Equal(80, Assert.IsType<TrackRhythm.Daily>(restoredPlan.Tracks[1].Rhythm).DayDonePercent);
        Assert.Equal("mobility", restoredPlan.Sessions.Single(s => s.Id == "m-mobility").TrackId);
    }
}
