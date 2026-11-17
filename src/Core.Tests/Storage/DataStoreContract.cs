using Core.Storage;

namespace Core.Tests.Storage;

/// <summary>
/// The behaviour every <see cref="IDataStore"/> must have. Subclass it, return a fresh empty store from
/// <see cref="CreateEmptyStore"/>, and the whole suite runs against that implementation.
/// </summary>
public abstract class DataStoreContract
{
    protected abstract IDataStore CreateEmptyStore();

    private static StoredRecord Rec(RecordKind kind, string key, string json = "{}") => new(kind, key, json);

    private static StoreChange Put(StoredRecord record) => new StoreChange.Put(record);

    private static StoreChange Delete(RecordKind kind, string key) => new StoreChange.Delete(kind, key);

    private static async Task<T> Ok<T>(Task<StoreResult<T>> operation)
    {
        var result = await operation;
        return Assert.IsType<StoreResult<T>.Success>(result).Value;
    }

    private static async Task<IReadOnlyList<StoredRecord>> RecordsOf(IDataStore store) => (await Ok(store.LoadAllAsync())).Records;

    private static async Task Apply(IDataStore store, params StoreChange[] changes) => await Ok(store.ApplyAsync(changes));

    // Representative JSON per kind. The store must hand it back byte for byte, so it includes nesting, unicode and odd whitespace.
    public static TheoryData<StoredRecord> OneRecordOfEveryKind() =>
    [
        Rec(RecordKind.Exercise, "ex_squat", """{"id":"ex_squat","name":"Back squat – paused","unit":"kg"}"""),
        Rec(RecordKind.Settings, RecordKeys.SettingsKey, """{"displayWeightUnit":"kg","restTimerSeconds":90}"""),
        Rec(RecordKind.Plan, "plan_1", """{"id":"plan_1","phases":[{"id":"ph_1","sessions":[{"id":"se_1","rows":[]}]}]}"""),
        Rec(RecordKind.WeighIn, "wi_1", """{"id":"wi_1","date":"2026-11-17","weightKg":82.4}"""),
        Rec(RecordKind.Break, "br_1", """{"id":"br_1","startDate":"2026-12-24","endDate":"2026-12-27","reason":"Holiday ☃"}"""),
        Rec(RecordKind.Badge, "bd_1", """{"id":"bd_1","name":"100 workouts","metric":"workouts","target":100}"""),
        Rec(RecordKind.Guide, "ex_squat", """{"exerciseId":"ex_squat","cues":["chest up","knees out"],"personalNotes":"  keep \"tight\"  "}"""),
        Rec(RecordKind.Workout, "wo_1", """{"id":"wo_1","entries":[{"sets":[{"reps":5,"weightKg":100},{"reps":5,"weightKg":100}]}],"painCheck":{"area":"knee","level":2}}"""),
        Rec(RecordKind.WeekSchedule, RecordKeys.ForWeekStart(new DateOnly(2026, 11, 16)), """{"weekStart":"2026-11-16","moves":[{"sessionId":"se_1","date":"2026-11-18"}]}"""),
    ];

    [Fact]
    public async Task An_empty_store_loads_empty()
    {
        var store = CreateEmptyStore();

        var data = await Ok(store.LoadAllAsync());

        Assert.Empty(data.Records);
        Assert.Equal(StoreMetadata.Empty, data.Metadata);
    }

    [Theory]
    [MemberData(nameof(OneRecordOfEveryKind))]
    public async Task Every_kind_round_trips_exactly(StoredRecord record)
    {
        var store = CreateEmptyStore();

        await Apply(store, Put(record));

        Assert.Equal([record], await RecordsOf(store));
    }

    [Fact]
    public async Task A_Guide_and_an_Exercise_with_the_same_key_are_separate_records()
    {
        var store = CreateEmptyStore();
        var exercise = Rec(RecordKind.Exercise, "ex_squat", "exercise");
        var guide = Rec(RecordKind.Guide, "ex_squat", "guide");

        await Apply(store, Put(exercise), Put(guide));

        Assert.Equivalent(new[] { exercise, guide }, await RecordsOf(store));
    }

    [Fact]
    public void A_Week_schedule_is_keyed_by_its_Monday_date()
    {
        var schedule = new Core.Model.WeekSchedule(new DateOnly(2026, 11, 16), []);

        Assert.Equal("2026-11-16", RecordKeys.ForWeekSchedule(schedule));
    }

    [Fact]
    public async Task Put_replaces_the_record_with_the_same_kind_and_key()
    {
        var store = CreateEmptyStore();
        await Apply(store, Put(Rec(RecordKind.Plan, "plan_1", "old")));

        await Apply(store, Put(Rec(RecordKind.Plan, "plan_1", "new")));

        Assert.Equal([Rec(RecordKind.Plan, "plan_1", "new")], await RecordsOf(store));
    }

    [Fact]
    public async Task Delete_removes_only_the_named_record()
    {
        var store = CreateEmptyStore();
        var kept = Rec(RecordKind.Workout, "wo_2");
        await Apply(store, Put(Rec(RecordKind.Workout, "wo_1")), Put(kept));

        await Apply(store, Delete(RecordKind.Workout, "wo_1"));

        Assert.Equal([kept], await RecordsOf(store));
    }

    [Fact]
    public async Task Deleting_a_missing_key_is_not_an_error()
    {
        var store = CreateEmptyStore();

        var result = await store.ApplyAsync([Delete(RecordKind.Workout, "nope")]);

        Assert.IsType<StoreResult<Unit>.Success>(result);
    }

    [Fact]
    public async Task A_batch_applies_its_puts_and_deletes_together_in_order()
    {
        var store = CreateEmptyStore();
        await Apply(store, Put(Rec(RecordKind.Plan, "old_plan")));

        await Apply(
            store,
            Delete(RecordKind.Plan, "old_plan"),
            Put(Rec(RecordKind.Plan, "new_plan")),
            Put(Rec(RecordKind.Plan, "temp")),
            Delete(RecordKind.Plan, "temp"));

        Assert.Equal([Rec(RecordKind.Plan, "new_plan")], await RecordsOf(store));
    }

    [Fact]
    public async Task A_failed_batch_changes_nothing()
    {
        var inner = CreateEmptyStore();
        await Apply(inner, Put(Rec(RecordKind.Plan, "plan_1", "before")));
        var store = new FaultInjectingDataStore(inner);
        store.FailLastOperationOfNextBatch(StoreFailureKind.StorageFull);

        var result = await store.ApplyAsync([Put(Rec(RecordKind.Plan, "plan_1", "after")), Put(Rec(RecordKind.Plan, "plan_2"))]);

        Assert.Equal(StoreFailureKind.StorageFull, Assert.IsType<StoreResult<Unit>.Failure>(result).Error.Kind);
        Assert.Equal([Rec(RecordKind.Plan, "plan_1", "before")], await RecordsOf(inner));
    }

    [Fact]
    public async Task A_batch_can_write_the_metadata_in_the_same_unit()
    {
        var store = CreateEmptyStore();
        var metadata = new StoreMetadata(null, null, null, true);

        await Ok(store.ApplyAsync([Put(Rec(RecordKind.Plan, "plan_1"))], metadata));

        var data = await Ok(store.LoadAllAsync());
        Assert.Equal(metadata, data.Metadata);
        Assert.Single(data.Records);
    }

    [Fact]
    public async Task A_failed_batch_does_not_write_the_metadata()
    {
        var inner = CreateEmptyStore();
        var store = new FaultInjectingDataStore(inner);
        store.FailLastOperationOfNextBatch(StoreFailureKind.StorageFull);

        await store.ApplyAsync([Put(Rec(RecordKind.Plan, "plan_1"))], new StoreMetadata(null, null, null, true));

        Assert.Equal(StoreMetadata.Empty, (await Ok(inner.LoadAllAsync())).Metadata);
    }

    [Fact]
    public async Task Replace_all_removes_everything_not_in_the_new_set()
    {
        var store = CreateEmptyStore();
        await Apply(store, Put(Rec(RecordKind.Plan, "old")), Put(Rec(RecordKind.Workout, "wo_old")), Put(Rec(RecordKind.Settings, RecordKeys.SettingsKey, "old")));
        var replacement = new[] { Rec(RecordKind.Plan, "new"), Rec(RecordKind.Settings, RecordKeys.SettingsKey, "new") };

        await Ok(store.ReplaceAllAsync(replacement));

        Assert.Equivalent(replacement, await RecordsOf(store));
    }

    [Fact]
    public async Task Replace_all_with_nothing_empties_the_store()
    {
        var store = CreateEmptyStore();
        await Apply(store, Put(Rec(RecordKind.Plan, "old")));

        await Ok(store.ReplaceAllAsync([]));

        Assert.Empty(await RecordsOf(store));
    }

    [Fact]
    public async Task A_failed_replace_all_leaves_the_previous_data_and_metadata()
    {
        var inner = CreateEmptyStore();
        var metadata = new StoreMetadata(null, 1, 1, true);
        await Apply(inner, Put(Rec(RecordKind.Plan, "old")));
        await Ok(inner.WriteMetadataAsync(metadata));
        var store = new FaultInjectingDataStore(inner);
        store.FailAllWrites(StoreFailureKind.StorageFull);

        var result = await store.ReplaceAllAsync([Rec(RecordKind.Plan, "new")], StoreMetadata.Empty);

        Assert.IsType<StoreResult<Unit>.Failure>(result);
        var data = await Ok(inner.LoadAllAsync());
        Assert.Equal([Rec(RecordKind.Plan, "old")], data.Records);
        Assert.Equal(metadata, data.Metadata);
    }

    [Fact]
    public async Task Metadata_round_trips()
    {
        var store = CreateEmptyStore();
        var metadata = new StoreMetadata(new DateTimeOffset(2026, 11, 17, 8, 30, 0, TimeSpan.Zero), 2, 1, true);

        await Ok(store.WriteMetadataAsync(metadata));

        Assert.Equal(metadata, (await Ok(store.LoadAllAsync())).Metadata);
    }

    [Fact]
    public async Task Replace_all_writes_the_metadata_when_given_and_keeps_it_when_not()
    {
        var store = CreateEmptyStore();
        var first = new StoreMetadata(null, 1, 1, true);
        await Ok(store.WriteMetadataAsync(first));

        await Ok(store.ReplaceAllAsync([]));
        Assert.Equal(first, (await Ok(store.LoadAllAsync())).Metadata);

        var second = first with { RecordFormatVersion = 2 };
        await Ok(store.ReplaceAllAsync([], second));
        Assert.Equal(second, (await Ok(store.LoadAllAsync())).Metadata);
    }

    [Fact]
    public async Task Metadata_and_records_do_not_disturb_each_other()
    {
        var store = CreateEmptyStore();
        var metadata = new StoreMetadata(null, 1, 1, true);
        await Ok(store.WriteMetadataAsync(metadata));

        await Apply(store, Put(Rec(RecordKind.Plan, "plan_1")));

        Assert.Equal(metadata, (await Ok(store.LoadAllAsync())).Metadata);
    }

    [Fact]
    public async Task Changing_the_batch_list_after_applying_it_does_not_change_storage()
    {
        var store = CreateEmptyStore();
        var batch = new List<StoreChange> { Put(Rec(RecordKind.Plan, "plan_1")) };
        await Ok(store.ApplyAsync(batch));

        batch.Clear();
        batch.Add(Put(Rec(RecordKind.Plan, "plan_2")));

        Assert.Equal([Rec(RecordKind.Plan, "plan_1")], await RecordsOf(store));
    }

    [Fact]
    public async Task Changing_a_loaded_list_does_not_change_storage()
    {
        var store = CreateEmptyStore();
        await Apply(store, Put(Rec(RecordKind.Plan, "plan_1")));
        var loaded = await RecordsOf(store);

        if (loaded is IList<StoredRecord> { IsReadOnly: false } writable)
        {
            writable.Clear();
        }

        Assert.Equal([Rec(RecordKind.Plan, "plan_1")], await RecordsOf(store));
    }
}
