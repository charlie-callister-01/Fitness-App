using Core.Storage;
using Core.Tests.Serialisation;

namespace Core.Tests.Storage;

public class AppDataEnvironmentTests
{
    private static readonly DateTimeOffset BackupTime = new(2026, 11, 17, 8, 0, 0, TimeSpan.Zero);

    private static Core.Model.Exercise Rdl() => TestData.GobletSquat() with { Id = "rdl", Name = "RDL" };

    private static async Task<InMemoryDataStore> StoreHolding()
    {
        var store = new InMemoryDataStore();
        var metadata = new StoreMetadata(null, null, null, true);
        Assert.IsType<StoreResult<Unit>.Success>(await store.ReplaceAllAsync(StoredRecords.FromDocument(TestData.Minimal()), metadata));
        return store;
    }

    [Theory]
    [InlineData(PersistenceStatus.Granted)]
    [InlineData(PersistenceStatus.NotGranted)]
    [InlineData(PersistenceStatus.Unsupported)]
    public async Task Persistence_result_is_in_health_and_blocks_nothing(PersistenceStatus status)
    {
        var environment = new InMemoryStorageEnvironment { Persistence = status };
        var app = await AppData.OpenAsync(await StoreHolding(), environment);

        Assert.Equal(status, app.Health.Persistence);
        Assert.True(app.Health.CanWrite);
        Assert.IsType<CommitResult.Committed>(await app.CommitAsync(Change.Put(Rdl())));
    }

    [Fact]
    public async Task Persistence_is_requested_at_every_launch()
    {
        var environment = new InMemoryStorageEnvironment { Persistence = PersistenceStatus.NotGranted };
        var store = await StoreHolding();

        await AppData.OpenAsync(store, environment);
        environment.Persistence = PersistenceStatus.Granted;
        var second = await AppData.OpenAsync(store, environment);

        Assert.Equal(2, environment.PersistenceRequests);
        Assert.Equal(PersistenceStatus.Granted, second.Health.Persistence);
    }

    [Fact]
    public async Task Health_carries_the_estimate_and_can_refresh_it()
    {
        var environment = new InMemoryStorageEnvironment { Estimate = new StorageEstimate(10, 100) };
        var app = await AppData.OpenAsync(await StoreHolding(), environment);
        Assert.Equal(new StorageEstimate(10, 100), app.Health.Estimate);

        environment.Estimate = new StorageEstimate(50, 100);
        await app.RefreshEstimateAsync();

        Assert.Equal(new StorageEstimate(50, 100), app.Health.Estimate);
    }

    [Fact]
    public async Task A_healthy_open_reports_available_and_writable()
    {
        var app = await AppData.OpenAsync(await StoreHolding(), new InMemoryStorageEnvironment());

        Assert.True(app.Health.DatabaseAvailable);
        Assert.True(app.Health.CanWrite);
        Assert.False(app.Health.AnotherWindowOpen);
    }

    [Fact]
    public async Task An_empty_store_with_no_marker_is_a_first_run()
    {
        var app = await AppData.OpenAsync(new InMemoryDataStore(), new InMemoryStorageEnvironment());

        Assert.Equal(StartupState.FirstRun, app.Startup.State);
    }

    [Fact]
    public async Task An_empty_store_with_the_marker_is_browser_cleared_data_not_first_run()
    {
        var app = await AppData.OpenAsync(new InMemoryDataStore(), new InMemoryStorageEnvironment { MarkerPresent = true });

        Assert.Equal(StartupState.BrowserCleared, app.Startup.State);
        Assert.Empty(app.Snapshot.Exercises);
    }

    [Fact]
    public async Task An_empty_store_that_says_it_was_initialised_is_ready_not_browser_cleared()
    {
        var store = new InMemoryDataStore();
        await store.WriteMetadataAsync(new StoreMetadata(null, null, null, true));

        var app = await AppData.OpenAsync(store, new InMemoryStorageEnvironment { MarkerPresent = true });

        Assert.Equal(StartupState.Ready, app.Startup.State);
    }

    [Fact]
    public async Task A_database_that_is_unavailable_never_consults_the_marker()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        store.FailEverything(StoreFailureKind.StorageUnavailable);

        var app = await AppData.OpenAsync(store, new InMemoryStorageEnvironment { MarkerPresent = true });

        Assert.Equal(StartupState.FirstRun, app.Startup.State);
        Assert.False(app.Health.DatabaseAvailable);
    }

    [Fact]
    public async Task A_record_set_aside_at_startup_cannot_be_overwritten_or_deleted_by_a_commit()
    {
        var store = await StoreHolding();
        var bad = new StoredRecord(RecordKind.Exercise, "rdl", """{"id":"rdl","name":"RDL","logKind":"nonsense"}""");
        await store.ApplyAsync([new StoreChange.Put(bad)]);
        var app = await AppData.OpenAsync(store, new InMemoryStorageEnvironment());

        var put = await app.CommitAsync(Change.Put(Rdl()));
        var delete = await app.CommitAsync(Change.Delete(RecordKind.Exercise, "rdl"));

        foreach (var result in new[] { put, delete })
        {
            var issue = Assert.Single(Assert.IsType<CommitResult.Rejected>(result).Issues);
            Assert.Equal("record_set_aside", issue.Code);
            Assert.Contains("rdl", issue.Path);
        }

        var stored = Assert.IsType<StoreResult<StoredData>.Success>(await store.LoadAllAsync()).Value;
        Assert.Contains(bad, stored.Records);
        Assert.Equal(["goblet-squat"], app.Snapshot.Exercises.Select(e => e.Id));
    }

    [Fact]
    public async Task Replace_all_still_replaces_a_set_aside_record_and_clears_the_block()
    {
        var store = await StoreHolding();
        await store.ApplyAsync([new StoreChange.Put(new StoredRecord(RecordKind.Exercise, "rdl", "{not json"))]);
        var app = await AppData.OpenAsync(store, new InMemoryStorageEnvironment());

        Assert.IsType<CommitResult.Committed>(await app.ReplaceAllAsync(TestData.Minimal()));

        Assert.IsType<CommitResult.Committed>(await app.CommitAsync(Change.Put(Rdl())));
    }

    [Fact]
    public async Task A_commit_queued_behind_a_replace_all_is_judged_against_the_data_the_replace_all_left()
    {
        var inner = await StoreHolding();
        await inner.ApplyAsync([new StoreChange.Put(new StoredRecord(RecordKind.Exercise, "rdl", "{not json"))]);
        var gated = new GatedReplaceAllDataStore(inner);
        var app = await AppData.OpenAsync(gated, new InMemoryStorageEnvironment());
        Assert.Single(app.Startup.Issues);

        var replace = app.ReplaceAllAsync(TestData.Minimal());
        await gated.ReplaceAllReached;
        var commit = app.CommitAsync(Change.Put(Rdl()));
        gated.Release();

        Assert.IsType<CommitResult.Committed>(await replace);
        Assert.IsType<CommitResult.Committed>(await commit);
    }

    [Fact]
    public async Task A_Changed_handler_that_throws_does_not_make_a_saved_commit_fail_or_stop_other_handlers()
    {
        var app = await AppData.OpenAsync(await StoreHolding());
        var laterHandlerRan = false;
        app.Changed += () => throw new InvalidOperationException("handler bug");
        app.Changed += () => laterHandlerRan = true;

        var result = await app.CommitAsync(Change.Put(Rdl()));

        Assert.IsType<CommitResult.Committed>(result);
        Assert.True(laterHandlerRan);
        Assert.Contains(app.Snapshot.Exercises, e => e.Id == "rdl");
    }

    [Fact]
    public void Only_stored_record_types_can_be_put_and_Settings_cannot_be_deleted()
    {
        Assert.Throws<ArgumentException>(() => Change.Put("not a record"));
        Assert.Throws<ArgumentException>(() => Change.Delete(RecordKind.Settings, "settings"));
    }

    [Fact]
    public async Task Recording_a_backup_is_refused_when_the_stored_metadata_is_unknown()
    {
        var inner = new InMemoryDataStore();
        var original = new StoreMetadata(null, 1, 1, true);
        await inner.WriteMetadataAsync(original);
        var store = new FaultInjectingDataStore(inner);
        store.FailEverything(StoreFailureKind.Unexpected);
        var app = await AppData.OpenAsync(store, new InMemoryStorageEnvironment());
        store.Recover();

        var result = await app.RecordBackupAsync(BackupTime);

        Assert.Equal(StoreFailureKind.Unexpected, Assert.IsType<StoreResult<Unit>.Failure>(result).Error.Kind);
        Assert.Equal(original, Assert.IsType<StoreResult<StoredData>.Success>(await inner.LoadAllAsync()).Value.Metadata);
    }

    [Fact]
    public async Task A_marker_with_an_empty_restore_is_not_browser_cleared()
    {
        var environment = new InMemoryStorageEnvironment { MarkerPresent = true };
        var store = new InMemoryDataStore();
        var app = await AppData.OpenAsync(store, environment);
        Assert.Equal(StartupState.BrowserCleared, app.Startup.State);

        await app.ReplaceAllAsync(TestData.Minimal() with { Exercises = [] });
        var restarted = await AppData.OpenAsync(store, environment);

        Assert.Equal(StartupState.Ready, restarted.Startup.State);
    }

    [Fact]
    public async Task A_store_with_data_is_ready_even_without_the_marker()
    {
        var app = await AppData.OpenAsync(await StoreHolding(), new InMemoryStorageEnvironment());

        Assert.Equal(StartupState.Ready, app.Startup.State);
    }

    [Fact]
    public async Task After_browser_cleared_data_the_owner_can_save_again()
    {
        var app = await AppData.OpenAsync(new InMemoryDataStore(), new InMemoryStorageEnvironment { MarkerPresent = true });

        Assert.IsType<CommitResult.Committed>(await app.CommitAsync(Change.Put(Rdl())));
    }

    [Fact]
    public async Task The_marker_is_written_on_the_first_successful_save_and_not_before()
    {
        var environment = new InMemoryStorageEnvironment();
        var app = await AppData.OpenAsync(new InMemoryDataStore(), environment);
        Assert.False(environment.MarkerPresent);

        await app.CommitAsync(Change.Put(Rdl()));

        Assert.True(environment.MarkerPresent);
        var restarted = await AppData.OpenAsync(new InMemoryDataStore(), environment);
        Assert.Equal(StartupState.BrowserCleared, restarted.Startup.State);
    }

    [Fact]
    public async Task A_failed_first_save_writes_no_marker()
    {
        var environment = new InMemoryStorageEnvironment();
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        var app = await AppData.OpenAsync(store, environment);
        store.FailAllWrites(StoreFailureKind.StorageFull);

        var result = await app.CommitAsync(Change.Put(Rdl()));

        Assert.IsType<CommitResult.StoreFailed>(result);
        Assert.False(environment.MarkerPresent);
    }

    [Fact]
    public async Task A_second_window_is_read_only_and_a_commit_changes_nothing()
    {
        var store = await StoreHolding();
        var environment = new InMemoryStorageEnvironment { WriterLock = WriterLockResult.HeldElsewhere };
        var app = await AppData.OpenAsync(store, environment);
        var before = app.Snapshot;

        var result = await app.CommitAsync(Change.Put(Rdl()));

        var failed = Assert.IsType<CommitResult.StoreFailed>(result);
        Assert.Equal(StoreFailureKind.WriterLockHeld, failed.Failure.Kind);
        Assert.Same(before, app.Snapshot);
        Assert.Equal(StartupState.Ready, app.Startup.State);
        Assert.True(app.Health.AnotherWindowOpen);
        Assert.False(app.Health.CanWrite);
        Assert.Equal(["goblet-squat"], (await AppData.OpenAsync(store)).Snapshot.Exercises.Select(e => e.Id));
    }

    [Fact]
    public async Task A_browser_without_web_locks_is_treated_as_a_single_window_and_says_so()
    {
        var app = await AppData.OpenAsync(await StoreHolding(), new InMemoryStorageEnvironment { WriterLock = WriterLockResult.Unsupported });

        var result = await app.CommitAsync(Change.Put(Rdl()));

        Assert.IsType<CommitResult.Committed>(result);
        Assert.True(app.Health.CanWrite);
        Assert.False(app.Health.AnotherWindowOpen);
        Assert.Equal(WriterLockResult.Unsupported, app.Health.WriterLock);
    }

    [Fact]
    public async Task When_the_lock_state_cannot_be_told_the_window_stays_read_only()
    {
        var app = await AppData.OpenAsync(await StoreHolding(), new InMemoryStorageEnvironment { WriterLock = WriterLockResult.Unknown });

        var result = await app.CommitAsync(Change.Put(Rdl()));

        Assert.Equal(StoreFailureKind.WriterLockHeld, Assert.IsType<CommitResult.StoreFailed>(result).Failure.Kind);
        Assert.False(app.Health.CanWrite);
        Assert.False(app.Health.AnotherWindowOpen);
        Assert.Equal(WriterLockResult.Unknown, app.Health.WriterLock);
    }

    [Fact]
    public async Task A_second_window_cannot_record_a_backup_either()
    {
        var app = await AppData.OpenAsync(await StoreHolding(), new InMemoryStorageEnvironment { WriterLock = WriterLockResult.HeldElsewhere });

        var result = await app.RecordBackupAsync(BackupTime);

        var failure = Assert.IsType<StoreResult<Unit>.Failure>(result);
        Assert.Equal(StoreFailureKind.WriterLockHeld, failure.Error.Kind);
        Assert.Null(app.LastBackupAt);
    }

    [Fact]
    public async Task Restoring_a_Backup_after_browser_cleared_data_writes_the_marker_and_starts_ready()
    {
        var environment = new InMemoryStorageEnvironment { MarkerPresent = true };
        var app = await AppData.OpenAsync(new InMemoryDataStore(), environment);
        environment.MarkerPresent = false;

        var result = await app.ReplaceAllAsync(TestData.Minimal());

        Assert.IsType<CommitResult.Committed>(result);
        Assert.Equal(StartupState.Ready, app.Startup.State);
        Assert.True(environment.MarkerPresent);
    }

    [Fact]
    public async Task A_second_window_cannot_replace_all()
    {
        var store = await StoreHolding();
        var app = await AppData.OpenAsync(store, new InMemoryStorageEnvironment { WriterLock = WriterLockResult.HeldElsewhere });

        var result = await app.ReplaceAllAsync(TestData.Minimal() with { Exercises = [Rdl()] });

        Assert.Equal(StoreFailureKind.WriterLockHeld, Assert.IsType<CommitResult.StoreFailed>(result).Failure.Kind);
        Assert.Equal(["goblet-squat"], (await AppData.OpenAsync(store)).Snapshot.Exercises.Select(e => e.Id));
    }

    [Fact]
    public async Task An_unavailable_database_falls_back_to_memory_and_the_owner_can_still_log()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        store.FailEverything(StoreFailureKind.StorageUnavailable);
        var environment = new InMemoryStorageEnvironment();

        var app = await AppData.OpenAsync(store, environment);

        Assert.False(app.Health.DatabaseAvailable);
        Assert.True(app.Health.CanWrite);
        Assert.Equal(StartupState.FirstRun, app.Startup.State);
        Assert.IsType<CommitResult.Committed>(await app.CommitAsync(Change.Put(Rdl())));
        Assert.Equal(["rdl"], app.Snapshot.Exercises.Select(e => e.Id));
        Assert.False(environment.MarkerPresent);
    }

    [Fact]
    public async Task A_store_that_cannot_be_read_is_not_writable_in_health()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        store.FailEverything(StoreFailureKind.Unexpected);

        var app = await AppData.OpenAsync(store, new InMemoryStorageEnvironment());

        Assert.Equal(StartupState.StorageFailed, app.Startup.State);
        Assert.True(app.Health.DatabaseAvailable);
        Assert.False(app.Health.CanWrite);
    }

    [Fact]
    public async Task RequestPersistence_asks_the_environment_again_and_updates_health()
    {
        var environment = new InMemoryStorageEnvironment { Persistence = PersistenceStatus.NotGranted };
        var app = await AppData.OpenAsync(new InMemoryDataStore(), environment);
        Assert.Equal(PersistenceStatus.NotGranted, app.Health.Persistence);

        environment.Persistence = PersistenceStatus.Granted;
        var status = await app.RequestPersistenceAsync();

        Assert.Equal(PersistenceStatus.Granted, status);
        Assert.Equal(PersistenceStatus.Granted, app.Health.Persistence);
        Assert.Equal(2, environment.PersistenceRequests);
    }
}
