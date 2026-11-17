using Core.Model;
using Core.Serialisation;
using Core.Storage;
using Core.Tests.Serialisation;

namespace Core.Tests.Storage;

public class AppDataCommitTests
{
    private static readonly DateTimeOffset BackupTime = new(2026, 11, 17, 8, 0, 0, TimeSpan.Zero);

    private static async Task<InMemoryDataStore> StoreHolding(ExchangeDocument document)
    {
        var store = new InMemoryDataStore();
        var metadata = new StoreMetadata(null, null, null, true);
        Assert.IsType<StoreResult<Unit>.Success>(await store.ReplaceAllAsync(StoredRecords.FromDocument(document), metadata));
        return store;
    }

    private static async Task<StoredData> Stored(IDataStore store) =>
        Assert.IsType<StoreResult<StoredData>.Success>(await store.LoadAllAsync()).Value;

    private static Exercise Rdl() => TestData.GobletSquat() with { Id = "rdl", Name = "RDL" };

    private static WeighIn Weigh(string id, double kg) => new(id, new DateOnly(2026, 11, 17), kg);

    [Fact]
    public async Task A_saved_record_is_in_the_snapshot_and_survives_a_restart()
    {
        var store = await StoreHolding(TestData.Minimal());
        var app = await AppData.OpenAsync(store);

        var result = await app.CommitAsync(Change.Put(Rdl()));

        Assert.IsType<CommitResult.Committed>(result);
        Assert.Equal(["goblet-squat", "rdl"], app.Snapshot.Exercises.Select(e => e.Id));
        var restarted = await AppData.OpenAsync(store);
        Assert.Equal(["goblet-squat", "rdl"], restarted.Snapshot.Exercises.Select(e => e.Id));
        Assert.Equal(ExchangeFormat.Write(app.Snapshot.ToDocument()), ExchangeFormat.Write(restarted.Snapshot.ToDocument()));
    }

    [Fact]
    public async Task Putting_a_record_that_exists_replaces_it_in_place()
    {
        var store = await StoreHolding(TestData.Minimal() with { Exercises = [TestData.GobletSquat(), Rdl()] });
        var app = await AppData.OpenAsync(store);

        await app.CommitAsync(Change.Put(TestData.GobletSquat() with { Name = "Goblet squat (heavy)" }));

        Assert.Equal(["goblet-squat", "rdl"], app.Snapshot.Exercises.Select(e => e.Id));
        Assert.Equal("Goblet squat (heavy)", app.Snapshot.Exercises[0].Name);
    }

    [Fact]
    public async Task Settings_are_saved_under_the_one_Settings_key()
    {
        var store = await StoreHolding(TestData.Minimal());
        var app = await AppData.OpenAsync(store);
        var settings = TestData.ValidSettings() with { DisplayWeightUnit = WeightUnit.Lb };

        await app.CommitAsync(Change.Put(settings));

        Assert.Equal(settings, app.Snapshot.Settings);
        Assert.Equal(settings, (await AppData.OpenAsync(store)).Snapshot.Settings);
    }

    [Fact]
    public async Task A_delete_removes_the_record_from_the_snapshot_and_from_storage()
    {
        var store = await StoreHolding(TestData.Minimal() with { WeighIns = [Weigh("wi-1", 80), Weigh("wi-2", 81)] });
        var app = await AppData.OpenAsync(store);

        var result = await app.CommitAsync(Change.Delete(RecordKind.WeighIn, "wi-1"));

        Assert.IsType<CommitResult.Committed>(result);
        Assert.Equal(["wi-2"], app.Snapshot.WeighIns.Select(w => w.Id));
        Assert.DoesNotContain((await Stored(store)).Records, r => r is { Kind: RecordKind.WeighIn, Key: "wi-1" });
    }

    [Fact]
    public async Task A_batch_puts_and_deletes_across_kinds_in_one_commit()
    {
        var store = await StoreHolding(TestData.Minimal() with { WeighIns = [Weigh("wi-1", 80)] });
        var app = await AppData.OpenAsync(store);

        await app.CommitAsync([Change.Put(Rdl()), Change.Put(Weigh("wi-2", 79)), Change.Delete(RecordKind.WeighIn, "wi-1")]);

        Assert.Equal(["goblet-squat", "rdl"], app.Snapshot.Exercises.Select(e => e.Id));
        Assert.Equal(["wi-2"], app.Snapshot.WeighIns.Select(w => w.Id));
    }

    [Fact]
    public async Task A_failing_store_leaves_the_snapshot_unchanged_raises_no_event_and_reports_the_failure_kind()
    {
        var store = new FaultInjectingDataStore(await StoreHolding(TestData.Minimal()));
        var app = await AppData.OpenAsync(store);
        var before = app.Snapshot;
        var events = 0;
        app.Changed += () => events++;
        store.FailAllWrites(StoreFailureKind.StorageFull);

        var result = await app.CommitAsync(Change.Put(Rdl()));

        Assert.Equal(StoreFailureKind.StorageFull, Assert.IsType<CommitResult.StoreFailed>(result).Failure.Kind);
        Assert.Same(before, app.Snapshot);
        Assert.Equal(0, events);
        Assert.Single((await Stored(store)).Records, r => r.Kind == RecordKind.Exercise);
    }

    [Fact]
    public async Task A_commit_succeeds_after_a_failed_one_once_the_store_recovers()
    {
        var store = new FaultInjectingDataStore(await StoreHolding(TestData.Minimal()));
        var app = await AppData.OpenAsync(store);
        store.FailAllWrites(StoreFailureKind.WriterLockHeld);
        await app.CommitAsync(Change.Put(Rdl()));
        store.Recover();

        var result = await app.CommitAsync(Change.Put(Rdl()));

        Assert.IsType<CommitResult.Committed>(result);
        Assert.Equal(["goblet-squat", "rdl"], app.Snapshot.Exercises.Select(e => e.Id));
    }

    [Fact]
    public async Task One_change_event_fires_per_successful_commit_after_the_snapshot_is_published()
    {
        var app = await AppData.OpenAsync(await StoreHolding(TestData.Minimal()));
        var seen = new List<int>();
        app.Changed += () => seen.Add(app.Snapshot.Exercises.Count);

        await app.CommitAsync([Change.Put(Rdl()), Change.Put(Weigh("wi-1", 80))]);
        await app.CommitAsync(Change.Put(TestData.GobletSquat() with { Name = "Renamed" }));

        Assert.Equal([2, 2], seen);
    }

    [Fact]
    public async Task A_snapshot_held_by_a_reader_never_changes_and_unchanged_kinds_are_shared()
    {
        var app = await AppData.OpenAsync(await StoreHolding(TestData.Minimal() with { WeighIns = [Weigh("wi-1", 80)] }));
        var held = app.Snapshot;
        var heldExercises = held.Exercises.ToList();

        await app.CommitAsync(Change.Put(Rdl()));
        var after = app.Snapshot;

        Assert.Equal(heldExercises, held.Exercises);
        Assert.Single(held.Exercises);
        Assert.NotSame(held.Exercises, after.Exercises);
        Assert.Same(held.WeighIns, after.WeighIns);
        Assert.Same(held.Plans, after.Plans);
    }

    [Fact]
    public async Task Commits_run_one_at_a_time_in_call_order_each_seeing_the_one_before()
    {
        var app = await AppData.OpenAsync(await StoreHolding(TestData.Minimal()));

        var results = await Task.WhenAll(
            Enumerable.Range(1, 10).Select(i => app.CommitAsync(Change.Put(Weigh($"wi-{i:00}", 70 + i)))));

        Assert.All(results, r => Assert.IsType<CommitResult.Committed>(r));
        Assert.Equal(Enumerable.Range(1, 10).Select(i => $"wi-{i:00}"), app.Snapshot.WeighIns.Select(w => w.Id));
    }

    [Fact]
    public async Task The_first_successful_commit_marks_the_store_initialised_in_the_same_unit_and_keeps_other_metadata()
    {
        var store = new InMemoryDataStore();
        var existing = new StoreMetadata(BackupTime, 1, ExchangeFormat.CurrentVersion, false);
        await store.WriteMetadataAsync(existing);
        var app = await AppData.OpenAsync(store);
        Assert.Equal(StartupState.FirstRun, app.Startup.State);

        await app.CommitAsync([Change.Put(TestData.GobletSquat()), Change.Put(TestData.ValidSettings())]);

        var metadata = (await Stored(store)).Metadata;
        Assert.Equal(existing with { Initialised = true }, metadata);
    }

    [Fact]
    public async Task A_failed_first_commit_does_not_mark_the_store_initialised()
    {
        var inner = new InMemoryDataStore();
        var store = new FaultInjectingDataStore(inner);
        var app = await AppData.OpenAsync(store);
        store.FailAllWrites(StoreFailureKind.StorageFull);

        await app.CommitAsync(Change.Put(TestData.GobletSquat()));

        Assert.False((await Stored(inner)).Metadata.Initialised);
    }

    [Theory]
    [InlineData(StartupState.CannotRead)]
    [InlineData(StartupState.StorageFailed)]
    public async Task Nothing_is_committed_when_startup_could_not_read_the_data(StartupState state)
    {
        var inner = await StoreHolding(TestData.Minimal());
        var store = new FaultInjectingDataStore(inner);
        if (state == StartupState.StorageFailed)
        {
            store.FailEverything(StoreFailureKind.Unexpected);
        }
        else
        {
            await inner.WriteMetadataAsync(new StoreMetadata(null, 1, ExchangeFormat.CurrentVersion + 1, true));
        }

        var app = await AppData.OpenAsync(store);
        store.Recover();
        var before = await Stored(inner);

        var result = await app.CommitAsync(Change.Put(Rdl()));

        Assert.Equal(state, Assert.IsType<CommitResult.Blocked>(result).State);
        Assert.Equal(before.Records, (await Stored(inner)).Records);
    }

    private static Plan Hyrox(ExchangeDocument document) => document.Plans!.Single(p => p.Id == "hyrox-2027");

    private static Plan OldPlan(ExchangeDocument document) => document.Plans!.Single(p => p.Id == "old-plan");

    [Fact]
    public async Task A_commit_that_leaves_two_active_Plans_is_rejected_with_the_validators_path_and_code_and_changes_nothing()
    {
        var plans = PlanTestData.Rich();
        var inner = await StoreHolding(plans);
        var app = await AppData.OpenAsync(inner);
        var before = app.Snapshot;
        var storedBefore = await Stored(inner);
        var events = 0;
        app.Changed += () => events++;

        var result = await app.CommitAsync(Change.Put(OldPlan(plans) with { IsActive = true }));

        var issue = Assert.Single(Assert.IsType<CommitResult.Rejected>(result).Issues);
        Assert.Equal(("plans[1].isActive", "multiple_active_plans"), (issue.Path, issue.Code));
        Assert.Same(before, app.Snapshot);
        Assert.Equal(0, events);
        Assert.Equal(storedBefore.Records, (await Stored(inner)).Records);
    }

    [Fact]
    public async Task A_commit_that_names_an_unknown_Exercise_is_rejected_with_the_exact_path_and_code()
    {
        var plans = PlanTestData.Rich();
        var app = await AppData.OpenAsync(await StoreHolding(plans));
        var workout = new Workout("w1", new DateOnly(2026, 11, 16), "a-lower", false, null, null, [],
            [new WorkoutEntry("nope", EntryStatus.Done, null, null, null, [])], null);

        var result = await app.CommitAsync(Change.Put(workout));

        var issue = Assert.Single(Assert.IsType<CommitResult.Rejected>(result).Issues);
        Assert.Equal(("workouts[0].entries[0].exerciseId", "unknown_exercise"), (issue.Path, issue.Code));
        Assert.Empty(app.Snapshot.Workouts);
    }

    [Fact]
    public async Task Switching_the_active_Plan_is_one_atomic_commit()
    {
        var plans = PlanTestData.Rich();
        var inner = await StoreHolding(plans);
        var app = await AppData.OpenAsync(inner);

        var result = await app.CommitAsync([Change.Put(Hyrox(plans) with { IsActive = false }), Change.Put(OldPlan(plans) with { IsActive = true })]);

        Assert.IsType<CommitResult.Committed>(result);
        Assert.Equal("old-plan", app.Snapshot.Plans.Single(p => p.IsActive).Id);
        Assert.Equal("old-plan", (await AppData.OpenAsync(inner)).Snapshot.Plans.Single(p => p.IsActive).Id);
    }

    [Fact]
    public async Task A_multi_record_commit_that_fails_in_the_store_saves_none_of_it()
    {
        var plans = PlanTestData.Rich();
        var inner = await StoreHolding(plans);
        var store = new FaultInjectingDataStore(inner);
        var app = await AppData.OpenAsync(store);
        var before = app.Snapshot;
        var storedBefore = await Stored(inner);
        store.FailLastOperationOfNextBatch(StoreFailureKind.Unexpected);

        var result = await app.CommitAsync([Change.Put(Hyrox(plans) with { IsActive = false }), Change.Put(OldPlan(plans) with { IsActive = true })]);

        Assert.IsType<CommitResult.StoreFailed>(result);
        Assert.Same(before, app.Snapshot);
        Assert.Equal(storedBefore.Records, (await Stored(inner)).Records);
    }

    [Fact]
    public async Task A_rejected_first_commit_does_not_mark_the_store_initialised()
    {
        var store = new InMemoryDataStore();
        var app = await AppData.OpenAsync(store);

        var workout = new Workout("w1", new DateOnly(2026, 11, 16), null, false, null, null, [],
            [new WorkoutEntry("nope", EntryStatus.Done, null, null, null, [])], null);

        var result = await app.CommitAsync(Change.Put(workout));

        Assert.IsType<CommitResult.Rejected>(result);
        Assert.Equal(StoreMetadata.Empty, (await Stored(store)).Metadata);
    }

    [Fact]
    public async Task The_last_Backup_time_is_recorded_and_read_back_after_a_restart()
    {
        var store = await StoreHolding(TestData.Minimal());
        var app = await AppData.OpenAsync(store);
        Assert.Null(app.LastBackupAt);

        var result = await app.RecordBackupAsync(BackupTime);

        Assert.IsType<StoreResult<Unit>.Success>(result);
        Assert.Equal(BackupTime, app.LastBackupAt);
        Assert.Equal(BackupTime, (await AppData.OpenAsync(store)).LastBackupAt);
    }

    [Fact]
    public async Task Recording_a_Backup_keeps_the_other_metadata_and_is_not_in_the_exchange_document()
    {
        var store = await StoreHolding(TestData.Minimal());
        await store.WriteMetadataAsync(new StoreMetadata(null, 3, ExchangeFormat.CurrentVersion, true));
        var app = await AppData.OpenAsync(store);

        await app.RecordBackupAsync(BackupTime);

        Assert.Equal(new StoreMetadata(BackupTime, 3, ExchangeFormat.CurrentVersion, true), (await Stored(store)).Metadata);
        Assert.DoesNotContain("2026-11-17T08", ExchangeFormat.Write(app.Snapshot.ToDocument()));
    }

    [Fact]
    public async Task A_Backup_time_that_cannot_be_saved_is_reported_and_not_remembered()
    {
        var store = new FaultInjectingDataStore(await StoreHolding(TestData.Minimal()));
        var app = await AppData.OpenAsync(store);
        store.FailAllWrites(StoreFailureKind.StorageFull);

        var result = await app.RecordBackupAsync(BackupTime);

        Assert.Equal(StoreFailureKind.StorageFull, Assert.IsType<StoreResult<Unit>.Failure>(result).Error.Kind);
        Assert.Null(app.LastBackupAt);
    }
}
