using Core.Model;
using Core.Rules.Nudges;
using Core.Rules.Schedule;
using Core.Rules.Weeks;
using Core.Serialisation;
using Core.Storage;
using Core.Tests.Serialisation;

namespace Core.Tests.Storage;

public class AppDataTests
{
    private static async Task<IDataStore> StoreHolding(ExchangeDocument document, StoreMetadata? metadata = null)
    {
        var store = new InMemoryDataStore();
        Assert.IsType<StoreResult<Unit>.Success>(await store.ReplaceAllAsync(StoredRecords.FromDocument(document), metadata));
        return store;
    }

    private static async Task<StoredData> Stored(IDataStore store) =>
        Assert.IsType<StoreResult<StoredData>.Success>(await store.LoadAllAsync()).Value;

    private static async Task Put(IDataStore store, StoredRecord record) =>
        Assert.IsType<StoreResult<Unit>.Success>(await store.ApplyAsync([new StoreChange.Put(record)]));

    private static string Json(ExchangeDocument document) => ExchangeFormat.Write(document with { ExportedAt = null, ExportedFrom = null });

    private static IEnumerable<string> KeysOf(IReadOnlyList<StoredRecord> records, RecordKind kind) =>
        records.Where(r => r.Kind == kind).Select(r => r.Key).Order();

    [Fact]
    public async Task An_empty_never_initialised_store_is_a_first_run_and_nothing_is_seeded()
    {
        var store = new InMemoryDataStore();

        var app = await AppData.OpenAsync(store);

        Assert.Equal(StartupState.FirstRun, app.Startup.State);
        Assert.Empty(app.Startup.Issues);
        Assert.Empty((await Stored(store)).Records);
    }

    [Fact]
    public async Task The_snapshot_has_every_collection_present_and_default_Settings_when_nothing_is_stored()
    {
        var snapshot = (await AppData.OpenAsync(new InMemoryDataStore())).Snapshot;

        Assert.Empty(snapshot.Exercises);
        Assert.Empty(snapshot.WeighIns);
        Assert.Empty(snapshot.Breaks);
        Assert.Empty(snapshot.Badges);
        Assert.Empty(snapshot.Guides);
        Assert.Empty(snapshot.Plans);
        Assert.Empty(snapshot.Workouts);
        Assert.Empty(snapshot.WeekSchedules);
        Assert.Equal(Settings.Default, snapshot.Settings);
    }

    [Fact]
    public async Task Stored_Settings_replace_the_defaults()
    {
        var settings = TestData.ValidSettings() with { DisplayWeightUnit = WeightUnit.Lb, GoalWeightKg = 79.5 };
        var store = await StoreHolding(TestData.Minimal() with { Settings = settings });

        var app = await AppData.OpenAsync(store);

        Assert.Equal(StartupState.Ready, app.Startup.State);
        Assert.Equal(settings, app.Snapshot.Settings);
    }

    [Fact]
    public async Task Every_record_survives_a_restart_exactly()
    {
        var original = WholeDatasetTests.Everything() with { WeekSchedules = [] };
        var store = await StoreHolding(original);

        var app = await AppData.OpenAsync(store);
        var restarted = await AppData.OpenAsync(store);

        Assert.Equal(StartupState.Ready, app.Startup.State);
        Assert.Empty(app.Startup.Issues);
        Assert.Equal(Json(original), ExchangeFormat.Write(app.Snapshot.ToDocument() with { ExportedAt = null }));
        Assert.Equal(Json(original), ExchangeFormat.Write(restarted.Snapshot.ToDocument() with { ExportedAt = null }));
    }

    [Fact]
    public void Records_are_keyed_by_the_key_rules()
    {
        var original = WholeDatasetTests.Everything() with
        {
            WeekSchedules = [new WeekSchedule(new DateOnly(2026, 11, 16), [])],
        };

        var records = StoredRecords.FromDocument(original);

        Assert.Equal(original.Exercises!.Select(RecordKeys.ForExercise).Order(), KeysOf(records, RecordKind.Exercise));
        Assert.Equal(original.Plans!.Select(RecordKeys.ForPlan).Order(), KeysOf(records, RecordKind.Plan));
        Assert.Equal(original.Workouts!.Select(RecordKeys.ForWorkout).Order(), KeysOf(records, RecordKind.Workout));
        Assert.Equal(original.Guides!.Select(RecordKeys.ForGuide).Order(), KeysOf(records, RecordKind.Guide));
        Assert.Equal(original.WeighIns!.Select(RecordKeys.ForWeighIn).Order(), KeysOf(records, RecordKind.WeighIn));
        Assert.Equal(original.Breaks!.Select(RecordKeys.ForBreak).Order(), KeysOf(records, RecordKind.Break));
        Assert.Equal(original.Badges!.Select(RecordKeys.ForBadge).Order(), KeysOf(records, RecordKind.Badge));
        Assert.Equal([RecordKeys.SettingsKey], KeysOf(records, RecordKind.Settings));
        Assert.Equal(["2026-11-16"], KeysOf(records, RecordKind.WeekSchedule));
    }

    [Fact]
    public async Task A_record_that_fails_reading_is_left_out_but_stays_in_storage_and_the_rest_is_kept()
    {
        var store = await StoreHolding(TestData.Minimal() with
        {
            Exercises = [TestData.GobletSquat(), TestData.GobletSquat() with { Id = "rdl", Name = "RDL" }],
        });
        var bad = new StoredRecord(RecordKind.Exercise, "rdl", """{"id":"rdl","name":"RDL","logKind":"nonsense"}""");
        await Put(store, bad);
        var before = await Stored(store);

        var app = await AppData.OpenAsync(store);

        Assert.Equal(StartupState.Ready, app.Startup.State);
        Assert.Equal(["goblet-squat"], app.Snapshot.Exercises.Select(e => e.Id));
        var issue = Assert.Single(app.Startup.Issues);
        Assert.Equal((RecordKind.Exercise, "rdl"), (issue.Kind, issue.Key));
        Assert.False(string.IsNullOrWhiteSpace(issue.Message));
        Assert.Equal(before.Records.OrderBy(r => r.Key), (await Stored(store)).Records.OrderBy(r => r.Key));
        Assert.Contains(bad, (await Stored(store)).Records);
    }

    [Fact]
    public async Task A_record_that_is_not_JSON_is_set_aside_without_stopping_the_app()
    {
        var store = await StoreHolding(TestData.Minimal());
        var broken = new StoredRecord(RecordKind.Badge, "b1", "{not json");
        await Put(store, broken);

        var app = await AppData.OpenAsync(store);

        Assert.Equal(StartupState.Ready, app.Startup.State);
        Assert.Empty(app.Snapshot.Badges);
        var issue = Assert.Single(app.Startup.Issues);
        Assert.Equal((RecordKind.Badge, "b1"), (issue.Kind, issue.Key));
        Assert.Contains(broken, (await Stored(store)).Records);
    }

    [Fact]
    public async Task A_record_that_breaks_a_rule_across_records_is_set_aside_too()
    {
        var store = await StoreHolding(TestData.Minimal());
        var ghost = new Guide("ghost", null, null, null, null, [], [], null, null, [], null, null);
        await Put(store, StoredRecords.FromDocument(TestData.Minimal() with { Guides = [ghost] }).Single(r => r.Kind == RecordKind.Guide));

        var app = await AppData.OpenAsync(store);

        Assert.Empty(app.Snapshot.Guides);
        Assert.Equal(["goblet-squat"], app.Snapshot.Exercises.Select(e => e.Id));
        var issue = Assert.Single(app.Startup.Issues);
        Assert.Equal((RecordKind.Guide, "ghost"), (issue.Kind, issue.Key));
    }

    [Fact]
    public async Task A_record_left_dangling_by_a_set_aside_record_is_set_aside_as_well()
    {
        var workout = new Workout("w1", new DateOnly(2026, 11, 16), null, false, null, null, [],
            [new WorkoutEntry("goblet-squat", EntryStatus.Done, null, null, null, [])], null);
        var store = await StoreHolding(TestData.Minimal() with { Workouts = [workout] });
        await Put(store, new StoredRecord(RecordKind.Exercise, "goblet-squat", """{"id":"goblet-squat","name":"Goblet squat","logKind":"nonsense"}"""));

        var app = await AppData.OpenAsync(store);

        Assert.Equal(StartupState.Ready, app.Startup.State);
        Assert.Empty(app.Snapshot.Exercises);
        Assert.Empty(app.Snapshot.Workouts);
        Assert.Equal([(RecordKind.Exercise, "goblet-squat"), (RecordKind.Workout, "w1")], app.Startup.Issues.Select(i => (i.Kind!.Value, i.Key!)));
    }

    [Fact]
    public async Task Data_written_by_a_newer_format_is_not_read_and_nothing_is_written()
    {
        var newer = new StoreMetadata(null, 1, ExchangeFormat.CurrentVersion + 1, true);
        var store = await StoreHolding(TestData.Minimal(), newer);
        var before = await Stored(store);

        var app = await AppData.OpenAsync(store);

        Assert.Equal(StartupState.CannotRead, app.Startup.State);
        var issue = Assert.Single(app.Startup.Issues);
        Assert.Null(issue.Kind);
        Assert.Contains("update the app", issue.Message);
        Assert.Empty(app.Snapshot.Exercises);
        var after = await Stored(store);
        Assert.Equal(before.Records, after.Records);
        Assert.Equal(before.Metadata, after.Metadata);
    }

    [Fact]
    public async Task A_store_that_cannot_be_loaded_reports_the_failure_and_starts_empty()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        store.FailEverything(StoreFailureKind.Unexpected);

        var app = await AppData.OpenAsync(store);

        Assert.Equal(StartupState.StorageFailed, app.Startup.State);
        Assert.Equal(StoreFailureKind.Unexpected, app.Startup.Failure?.Kind);
        Assert.Empty(app.Snapshot.Plans);
        Assert.Equal(Settings.Default, app.Snapshot.Settings);
    }

    [Fact]
    public async Task A_restarted_dataset_drives_the_Streak_Missed_sessions_and_Backup_Nudge_rules()
    {
        var everything = WholeDatasetTests.Everything();
        var lastBackup = new DateTimeOffset(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);
        var store = await StoreHolding(everything, new StoreMetadata(lastBackup, 1, ExchangeFormat.CurrentVersion, true));
        await AppData.OpenAsync(store);

        var snapshot = (await AppData.OpenAsync(store)).Snapshot;

        var today = new DateOnly(2026, 11, 30);
        var expectedPlan = everything.Plans!.Single(p => p.IsActive);
        var plan = snapshot.Plans.Single(p => p.IsActive);
        var storedLastBackup = (await Stored(store)).Metadata.LastBackupAt;
        Assert.Equal(lastBackup, storedLastBackup);
        Assert.Equal(
            WeekRules.Streak(expectedPlan, everything.Workouts!, everything.Breaks!, today),
            WeekRules.Streak(plan, snapshot.Workouts, snapshot.Breaks, today));
        var missed = WeekScheduleRules.Missed(plan, snapshot.WeekSchedules, snapshot.Breaks, snapshot.Workouts, today);
        Assert.Equal(WeekScheduleRules.Missed(expectedPlan, [], everything.Breaks!, everything.Workouts!, today), missed);
        Assert.NotEmpty(missed);
        var nudges = NudgeRules.Compute(plan, snapshot.WeekSchedules, snapshot.Breaks, snapshot.Workouts, storedLastBackup, today);
        Assert.Contains(nudges, n => n.Kind == NudgeKind.NoRecentBackup);
    }
}
