using System.Text.Json.Nodes;
using Core.Exchange;
using Core.Model;
using Core.Rules.Nudges;
using Core.Rules.Phases;
using Core.Rules.Schedule;
using Core.Rules.Tracks;
using Core.Rules.Weeks;
using Core.Serialisation;
using Core.Storage;
using Core.Tests.Exchange;
using Core.Tests.Serialisation;

namespace Core.Tests.Storage;

/// <summary>
/// Real format 1 to format 2 stored-data migration: v1 records (Plans with Plan-level target, Streak fields and Phases;
/// Sessions without <c>trackId</c>) open through <see cref="StartupReader"/> and the v1 upgrader, rewrite deterministically
/// into Track shape with <c>RecordFormatVersion = 2</c>, and fail safely on write failure or quarantined records.
/// </summary>
public class AppDataMigrationTests
{
    private static ExchangeDocument RichV1Equivalent()
    {
        var baseDoc = WorkoutTestData.Rich();
        return baseDoc with
        {
            Settings = TestData.ValidSettings() with { DisplayWeightUnit = WeightUnit.Lb, GoalWeightKg = 79.5 },
            WeighIns = [new WeighIn("wi-1", new DateOnly(2026, 11, 16), 84.35)],
            Breaks = [new Break("xmas", new DateOnly(2026, 12, 20), new DateOnly(2027, 1, 2), "Holiday")],
            Badges = [new Badge("b1", "Streak", "streak", 4)],
            Guides = [new Guide("goblet-squat", "Quads", null, null, null, [], [], null, null, [], null, null)],
            WeekSchedules = [new WeekSchedule(new DateOnly(2026, 11, 16), [new SessionMove("a-lower", new DateOnly(2026, 11, 18))])],
        };
    }

    private static IReadOnlyList<StoredRecord> ToV1Records(ExchangeDocument document) =>
        [
            .. StoredRecords.FromDocument(document)
                .Select(r => r.Kind == RecordKind.Plan
                    ? r with { Json = ToV1PlanJson(JsonNode.Parse(r.Json)!.AsObject()).ToJsonString() }
                    : r),
        ];

    private static string ToV1DocumentJson(ExchangeDocument document)
    {
        var root = JsonNode.Parse(ExchangeFormat.Write(document))!.AsObject();
        root["formatVersion"] = 1;
        root.Remove("readMe");
        if (root["plans"] is JsonArray plans)
        {
            for (var i = 0; i < plans.Count; i++)
            {
                plans[i] = ToV1PlanJson(plans[i]!.AsObject());
            }
        }

        return root.ToJsonString();
    }

    private static JsonObject ToV1PlanJson(JsonObject v2Plan)
    {
        var main = v2Plan["tracks"]![0]!.AsObject();
        var v1 = new JsonObject
        {
            ["id"] = v2Plan["id"]!.DeepClone(),
            ["name"] = v2Plan["name"]!.DeepClone(),
            ["isActive"] = v2Plan["isActive"]!.DeepClone(),
            ["startDate"] = v2Plan["startDate"]!.DeepClone(),
        };
        if (v2Plan["goal"] is { } goal)
        {
            v1["goal"] = goal.DeepClone();
        }

        if (v2Plan["goalDate"] is { } goalDate)
        {
            v1["goalDate"] = goalDate.DeepClone();
        }

        v1["weeklyTarget"] = main["weeklyTarget"]!.DeepClone();
        v1["streakMinWorkoutsPerWeek"] = main["streakMinWorkoutsPerWeek"]!.DeepClone();
        v1["streakCountsCustomWorkouts"] = main["streakCountsCustomWorkouts"]!.DeepClone();
        v1["weightIncrementKg"] = v2Plan["weightIncrementKg"]!.DeepClone();
        v1["phases"] = main["phases"]!.DeepClone();

        var sessions = new JsonArray();
        foreach (var session in v2Plan["sessions"]!.AsArray())
        {
            var copy = session!.AsObject().DeepClone().AsObject();
            copy.Remove("trackId");
            sessions.Add(copy);
        }

        v1["sessions"] = sessions;
        return v1;
    }

    private static async Task<InMemoryDataStore> StoreHolding(IReadOnlyList<StoredRecord> records, StoreMetadata metadata)
    {
        var store = new InMemoryDataStore();
        Assert.IsType<StoreResult<Unit>.Success>(await store.ReplaceAllAsync(records, metadata));
        return store;
    }

    private static async Task<InMemoryDataStore> StoreHolding(ExchangeDocument document, StoreMetadata metadata) =>
        await StoreHolding(StoredRecords.FromDocument(document), metadata);

    private static async Task<StoredData> Stored(IDataStore store) =>
        Assert.IsType<StoreResult<StoredData>.Success>(await store.LoadAllAsync()).Value;

    private static StoreMetadata V1Metadata(DateTimeOffset? lastBackupAt = null) =>
        new(lastBackupAt, 7, 1, Initialised: true);

    [Fact]
    public async Task Stored_v1_dataset_migrates_to_track_shape_with_v2_metadata_leaving_other_records_unchanged()
    {
        var expectedDoc = RichV1Equivalent();
        var v1Records = ToV1Records(expectedDoc);
        var lastBackup = new DateTimeOffset(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);
        var store = await StoreHolding(v1Records, V1Metadata(lastBackup));

        var app = await AppData.OpenAsync(store);

        Assert.Equal(StartupState.Ready, app.Startup.State);
        Assert.Empty(app.Startup.Issues);

        Assert.Equal(["hyrox-2027", "old-plan"], app.Snapshot.Plans.Select(p => p.Id));
        var activeTrack = Assert.Single(app.Snapshot.Plans[0].Tracks);
        var inactiveTrack = Assert.Single(app.Snapshot.Plans[1].Tracks);
        Assert.Equal(("hyrox-2027-main", "Main"), (activeTrack.Id, activeTrack.Name));
        Assert.Equal(("old-plan-main", "Main"), (inactiveTrack.Id, inactiveTrack.Name));
        Assert.NotEqual(activeTrack.Id, inactiveTrack.Id);
        Assert.Equal(new TrackRhythm.Weekly(5, 4, false), activeTrack.Rhythm);
        Assert.Equal(new TrackRhythm.Weekly(3, 3, true), inactiveTrack.Rhythm);
        Assert.Equal(3, activeTrack.Phases.Count);
        Assert.All(app.Snapshot.Plans[0].Sessions, s => Assert.Equal("hyrox-2027-main", s.TrackId));

        var stored = await Stored(store);
        Assert.Equal(2, stored.Metadata.RecordFormatVersion);
        Assert.Equal(ExchangeFormat.CurrentVersion, stored.Metadata.RecordFormatVersion);
        Assert.Equal(lastBackup, stored.Metadata.LastBackupAt);

        var expectedNonPlans = v1Records.Where(r => r.Kind != RecordKind.Plan).OrderBy(r => (r.Kind, r.Key)).ToList();
        var actualNonPlans = stored.Records.Where(r => r.Kind != RecordKind.Plan).OrderBy(r => (r.Kind, r.Key)).ToList();
        Assert.Equal(expectedNonPlans, actualNonPlans);

        foreach (var planRecord in stored.Records.Where(r => r.Kind == RecordKind.Plan))
        {
            var json = JsonNode.Parse(planRecord.Json)!.AsObject();
            Assert.True(json.ContainsKey("tracks"));
            Assert.False(json.ContainsKey("weeklyTarget"));
            Assert.False(json.ContainsKey("streakMinWorkoutsPerWeek"));
            Assert.False(json.ContainsKey("streakCountsCustomWorkouts"));
            Assert.False(json.ContainsKey("phases"));
        }

        // Weekly rules on the migrated dataset match the pre-migration expectations.
        var today = new DateOnly(2026, 11, 20);
        var migratedActive = app.Snapshot.Plans[0];
        var migratedMain = TrackRules.Main(migratedActive);
        var referenceActive = expectedDoc.Plans![0];
        var referenceMain = TrackRules.Main(referenceActive);
        Assert.Equal(
            WeekRules.CountedWorkouts(referenceActive, referenceMain, expectedDoc.Workouts!, new DateOnly(2026, 11, 16)),
            WeekRules.CountedWorkouts(migratedActive, migratedMain, app.Snapshot.Workouts, new DateOnly(2026, 11, 16)));
        Assert.Equal(
            WeekRules.CountedWorkoutsByWeek(referenceActive, referenceMain, expectedDoc.Workouts!),
            WeekRules.CountedWorkoutsByWeek(migratedActive, migratedMain, app.Snapshot.Workouts));
        Assert.Equal(
            WeekRules.Streak(referenceActive, referenceMain, expectedDoc.Workouts!, expectedDoc.Breaks!, today),
            WeekRules.Streak(migratedActive, migratedMain, app.Snapshot.Workouts, app.Snapshot.Breaks, today));
        Assert.Equal(
            WeekRules.Consistency(referenceActive, referenceMain, expectedDoc.Workouts!, expectedDoc.Breaks!, today),
            WeekRules.Consistency(migratedActive, migratedMain, app.Snapshot.Workouts, app.Snapshot.Breaks, today));
        Assert.Equal(
            WeekScheduleRules.Layout(referenceActive, referenceMain, expectedDoc.WeekSchedules!, expectedDoc.Breaks!, today).Sessions.Select(s => (s.SessionId, s.Date, s.IsMoved, s.IsBreakDay)),
            WeekScheduleRules.Layout(migratedActive, migratedMain, app.Snapshot.WeekSchedules, app.Snapshot.Breaks, today).Sessions.Select(s => (s.SessionId, s.Date, s.IsMoved, s.IsBreakDay)));
        Assert.Equal(
            WeekScheduleRules.Missed(referenceActive, referenceMain, expectedDoc.WeekSchedules!, expectedDoc.Breaks!, expectedDoc.Workouts!, today),
            WeekScheduleRules.Missed(migratedActive, migratedMain, app.Snapshot.WeekSchedules, app.Snapshot.Breaks, app.Snapshot.Workouts, today));
        var expectedPhase = PhaseRules.Position(referenceActive, referenceMain, today);
        var actualPhase = PhaseRules.Position(migratedActive, migratedMain, today);
        Assert.Equal(
            (expectedPhase.State, expectedPhase.PlanWeek, expectedPhase.Phase?.Id, expectedPhase.Phase?.Focus, expectedPhase.WeekInPhase, expectedPhase.PhaseLengthWeeks),
            (actualPhase.State, actualPhase.PlanWeek, actualPhase.Phase?.Id, actualPhase.Phase?.Focus, actualPhase.WeekInPhase, actualPhase.PhaseLengthWeeks));
        Assert.Equal(expectedPhase.Phase?.Goals, actualPhase.Phase?.Goals);
        Assert.Equal(
            PhaseRules.ExerciseRows(referenceActive, "a-lower", today),
            PhaseRules.ExerciseRows(migratedActive, "a-lower", today));
        Assert.Equal(
            NudgeRules.Compute(referenceActive, expectedDoc.WeekSchedules!, expectedDoc.Breaks!, expectedDoc.Workouts!, lastBackup, today),
            NudgeRules.Compute(migratedActive, app.Snapshot.WeekSchedules, app.Snapshot.Breaks, app.Snapshot.Workouts, app.LastBackupAt, today));
    }

    [Fact]
    public async Task Migrating_twice_from_the_same_v1_input_gives_byte_identical_output_and_reopening_writes_nothing()
    {
        var v1Records = ToV1Records(RichV1Equivalent());
        var meta = V1Metadata();
        var storeA = await StoreHolding(v1Records, meta);
        var storeB = await StoreHolding(v1Records, meta);

        var appA = await AppData.OpenAsync(storeA);
        var appB = await AppData.OpenAsync(storeB);

        var storedA = await Stored(storeA);
        var storedB = await Stored(storeB);
        Assert.Equal(storedA.Metadata, storedB.Metadata);
        Assert.Equal(storedA.Records, storedB.Records);
        Assert.Equal(
            ExchangeFormat.Write(appA.Snapshot.ToDocument()),
            ExchangeFormat.Write(appB.Snapshot.ToDocument()));

        // Reopening the migrated v2 store writes nothing.
        var readOnlyStore = new FaultInjectingDataStore(storeA);
        readOnlyStore.FailAllWrites(StoreFailureKind.Unexpected);

        var reopened = await AppData.OpenAsync(readOnlyStore);

        Assert.Equal(StartupState.Ready, reopened.Startup.State);
        Assert.Equal(
            ExchangeFormat.Write(appA.Snapshot.ToDocument()),
            ExchangeFormat.Write(reopened.Snapshot.ToDocument()));
    }

    [Fact]
    public async Task The_layout_version_is_not_changed_by_a_format_migration()
    {
        var store = await StoreHolding(ToV1Records(RichV1Equivalent()), V1Metadata());

        await AppData.OpenAsync(store);

        Assert.Equal(7, (await Stored(store)).Metadata.LayoutVersion);
    }

    [Fact]
    public async Task Data_already_in_the_current_version_is_not_rewritten()
    {
        var current = V1Metadata() with { RecordFormatVersion = ExchangeFormat.CurrentVersion };
        var store = new FaultInjectingDataStore(await StoreHolding(RichV1Equivalent(), current));
        store.FailAllWrites(StoreFailureKind.Unexpected);

        var app = await AppData.OpenAsync(store);

        Assert.Equal(StartupState.Ready, app.Startup.State);
    }

    [Fact]
    public async Task A_migration_that_fails_changes_nothing_and_starts_as_CannotRead()
    {
        var inner = await StoreHolding(ToV1Records(RichV1Equivalent()), V1Metadata());
        var before = await Stored(inner);
        var store = new FaultInjectingDataStore(inner);
        store.FailAllWrites(StoreFailureKind.StorageFull);

        var app = await AppData.OpenAsync(store);

        Assert.Equal(StartupState.CannotRead, app.Startup.State);
        Assert.False(app.Health.CanWrite);
        Assert.Empty(app.Snapshot.Plans);
        Assert.IsType<CommitResult.Blocked>(await app.CommitAsync(Change.Put(TestData.GobletSquat())));
        var after = await Stored(inner);
        Assert.Equal(before.Records, after.Records);
        Assert.Equal(before.Metadata, after.Metadata);
    }

    [Theory]
    [InlineData("old-plan", 1)]
    [InlineData("hyrox-2027", 0)]
    public async Task A_quarantined_v1_plan_prevents_migration_and_leaves_stored_data_untouched(string planKey, int planIndex)
    {
        var baseRecords = ToV1Records(RichV1Equivalent());
        var badPlanJson = ToV1PlanJson(JsonNode.Parse(ExchangeFormat.Write(RichV1Equivalent()))!["plans"]![planIndex]!.AsObject());
        badPlanJson["weeklyTarget"] = 0;
        var badPlan = new StoredRecord(RecordKind.Plan, planKey, badPlanJson.ToJsonString());
        var records = baseRecords.Where(r => !(r.Kind == RecordKind.Plan && r.Key == planKey)).Append(badPlan).ToList();

        var inner = await StoreHolding(records, V1Metadata());
        var before = await Stored(inner);

        var app = await AppData.OpenAsync(inner);

        Assert.Equal(StartupState.CannotRead, app.Startup.State);
        Assert.False(app.Health.CanWrite);
        Assert.IsType<CommitResult.Blocked>(await app.CommitAsync(Change.Put(TestData.GobletSquat())));
        var after = await Stored(inner);
        Assert.Equal(before.Records, after.Records);
        Assert.Equal(before.Metadata, after.Metadata);
    }

    [Fact]
    public async Task A_malformed_v1_plan_prevents_migration_and_leaves_stored_data_untouched()
    {
        var records = ToV1Records(RichV1Equivalent())
            .Select(r => r.Kind == RecordKind.Plan && r.Key == "old-plan" ? r with { Json = "{not json" } : r)
            .ToList();
        var inner = await StoreHolding(records, V1Metadata());
        var before = await Stored(inner);

        var app = await AppData.OpenAsync(inner);

        Assert.Equal(StartupState.CannotRead, app.Startup.State);
        var after = await Stored(inner);
        Assert.Equal(before.Records, after.Records);
        Assert.Equal(before.Metadata, after.Metadata);
    }

    [Fact]
    public async Task Data_from_a_newer_version_than_the_app_still_cannot_be_read_and_nothing_is_written()
    {
        var store = await StoreHolding(TestData.Minimal(), V1Metadata() with { RecordFormatVersion = ExchangeFormat.CurrentVersion + 1 });
        var before = await Stored(store);

        var app = await AppData.OpenAsync(store);

        Assert.Equal(StartupState.CannotRead, app.Startup.State);
        Assert.Equal(before.Metadata, (await Stored(store)).Metadata);
    }

    [Fact]
    public async Task Every_save_records_the_current_format_version()
    {
        var store = await StoreHolding(TestData.Minimal(), new StoreMetadata(null, null, null, true));
        var app = await AppData.OpenAsync(store);

        await app.CommitAsync(Change.Put(TestData.GobletSquat() with { Name = "Renamed" }));

        Assert.Equal(ExchangeFormat.CurrentVersion, (await Stored(store)).Metadata.RecordFormatVersion);
    }

    [Fact]
    public async Task A_save_after_a_migration_writes_version_2_records()
    {
        var store = await StoreHolding(ToV1Records(RichV1Equivalent()), V1Metadata());
        var app = await AppData.OpenAsync(store);
        var editedPlan = app.Snapshot.Plans[0] with { Name = "HYROX 2027 Updated" };

        var result = await app.CommitAsync(Change.Put(editedPlan));

        Assert.IsType<CommitResult.Committed>(result);
        var stored = await Stored(store);
        Assert.Equal(2, stored.Metadata.RecordFormatVersion);
        var storedPlan = stored.Records.Single(r => r.Kind == RecordKind.Plan && r.Key == "hyrox-2027");
        Assert.True(JsonNode.Parse(storedPlan.Json)!.AsObject().ContainsKey("tracks"));

        var readOnlyStore = new FaultInjectingDataStore(store);
        readOnlyStore.FailAllWrites(StoreFailureKind.Unexpected);
        var reopened = await AppData.OpenAsync(readOnlyStore);
        Assert.Equal(StartupState.Ready, reopened.Startup.State);
        Assert.Equal("HYROX 2027 Updated", reopened.Snapshot.Plans.Single(p => p.Id == "hyrox-2027").Name);
    }

    [Fact]
    public async Task Restoring_a_v1_backup_into_a_v2_store_writes_track_shaped_plans()
    {
        var store = await StoreHolding(TestData.Minimal(), V1Metadata() with { RecordFormatVersion = 2 });
        var app = await AppData.OpenAsync(store);
        var v1BackupJson = ToV1DocumentJson(RichV1Equivalent() with
        {
            ExportedAt = new DateTimeOffset(2026, 11, 18, 10, 0, 0, TimeSpan.Zero),
            ExportedFrom = "fitness-app 0.1",
        });

        var inspected = ImportInput.Inspect(v1BackupJson);
        Assert.True(inspected.IsAccepted, string.Join("; ", inspected.Rejections));
        var now = new DateTimeOffset(2026, 11, 20, 9, 0, 0, TimeSpan.Zero);
        var preview = ImportPreviewer.Preview(app, inspected.Inspected!, ImportKind.Restore, ImportDecisions.None, now);
        Assert.True(preview.IsReady, string.Join("; ", preview.Items.Select(i => i.Text)));

        var outcome = await ImportApplier.ApplyAsync(app, preview, new RecordingBackupSink(), now);

        Assert.IsType<ApplyOutcome.Applied>(outcome);
        Assert.Equal(["hyrox-2027", "old-plan"], app.Snapshot.Plans.Select(p => p.Id));
        Assert.Equal("hyrox-2027-main", Assert.Single(app.Snapshot.Plans[0].Tracks).Id);
        Assert.Equal("old-plan-main", Assert.Single(app.Snapshot.Plans[1].Tracks).Id);

        var stored = await Stored(store);
        Assert.Equal(2, stored.Metadata.RecordFormatVersion);
        Assert.All(stored.Records.Where(r => r.Kind == RecordKind.Plan),
            r => Assert.True(JsonNode.Parse(r.Json)!.AsObject().ContainsKey("tracks")));

        var readOnlyStore = new FaultInjectingDataStore(store);
        readOnlyStore.FailAllWrites(StoreFailureKind.Unexpected);
        var reopened = await AppData.OpenAsync(readOnlyStore);
        Assert.Equal(StartupState.Ready, reopened.Startup.State);
        Assert.Equal("hyrox-2027-main", Assert.Single(reopened.Snapshot.Plans[0].Tracks).Id);
    }
}
