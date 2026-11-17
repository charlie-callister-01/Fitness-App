using Core.Storage;
using Web.Storage;

namespace Web.BrowserTests;

/// <summary>Behaviour only a real browser can show: real transaction rollback, reload, upgrade, the writer lock.</summary>
[Collection(BrowserCollection.Name)]
public sealed class IndexedDbBrowserBehaviourTests(BrowserFixture browser) : IAsyncLifetime
{
    private BrowserSession _session = null!;

    public async Task InitializeAsync() => _session = await browser.NewSessionAsync();

    public async Task DisposeAsync() => await _session.DisposeAsync();

    private (IndexedDbInterop Interop, IndexedDbDataStore Store, string Name) NewStore(string? name = null)
    {
        name ??= BrowserSession.NewDatabaseName();
        var interop = _session.NewInterop();
        _session.DestroyOnDispose(name);
        return (interop, new IndexedDbDataStore(interop, name), name);
    }

    private static async Task<StoredData> Load(IDataStore store) =>
        Assert.IsType<StoreResult<StoredData>.Success>(await store.LoadAllAsync()).Value;

    private static StoredRecord Plan(string key, string json) => new(RecordKind.Plan, key, json);

    private static async Task Seed(IDataStore store, StoreMetadata metadata)
    {
        Assert.IsType<StoreResult<Unit>.Success>(await store.ApplyAsync([new StoreChange.Put(Plan("plan_1", "before"))], metadata));
    }

    private static readonly StoreMetadata Before = new(null, 1, 1, true);

    [Fact]
    public async Task A_batch_with_a_bad_last_operation_aborts_the_whole_transaction()
    {
        var (interop, store, _) = NewStore();
        await Seed(store, Before);

        var result = await interop.ApplyAsync(
            [IndexedDbOperation.Put("plans", "plan_1", "after"), IndexedDbOperation.Put("plans", "plan_2", "new"), new IndexedDbOperation("explode", "plans", "x", null)],
            new Dictionary<string, string?> { ["layoutVersion"] = "9" });

        Assert.NotNull(result.Failure);
        var data = await Load(store);
        Assert.Equal([Plan("plan_1", "before")], data.Records);
        Assert.Equal(Before, data.Metadata);
    }

    [Fact]
    public async Task A_batch_with_an_unknown_kind_is_rejected_before_anything_is_written()
    {
        var (interop, store, _) = NewStore();
        await Seed(store, Before);

        var result = await interop.ApplyAsync([IndexedDbOperation.Put("plans", "plan_1", "after"), IndexedDbOperation.Put("nonsense", "k", "{}")]);

        Assert.NotNull(result.Failure);
        Assert.Equal([Plan("plan_1", "before")], (await Load(store)).Records);
    }

    [Fact]
    public async Task A_replace_all_that_fails_midway_keeps_the_previous_records_and_metadata()
    {
        var (interop, store, _) = NewStore();
        await Seed(store, Before);
        // The bad record (no key) is in the last store, after the earlier stores were cleared and rewritten.
        var records = new Dictionary<string, IReadOnlyList<RawRecord>>
        {
            ["plans"] = [new RawRecord("plan_new", "new")],
            ["weekSchedules"] = [new RawRecord(null!, "no key")],
        };

        var result = await interop.ReplaceAllAsync(records, new Dictionary<string, string?> { ["layoutVersion"] = "9" });

        Assert.NotNull(result.Failure);
        var data = await Load(store);
        Assert.Equal([Plan("plan_1", "before")], data.Records);
        Assert.Equal(Before, data.Metadata);
    }

    [Fact]
    public async Task Data_survives_a_page_reload()
    {
        var (_, store, name) = NewStore();
        var metadata = new StoreMetadata(new DateTimeOffset(2026, 11, 17, 8, 30, 0, TimeSpan.Zero), 1, 1, true);
        await Seed(store, metadata);

        await _session.ReloadAsync();

        var reopened = new IndexedDbDataStore(_session.NewInterop(), name);
        var data = await Load(reopened);
        Assert.Equal([Plan("plan_1", "before")], data.Records);
        Assert.Equal(metadata, data.Metadata);
    }

    [Fact]
    public async Task The_store_reopens_after_the_module_closed_its_connection()
    {
        var (interop, store, _) = NewStore();
        await Seed(store, Before);
        // What a versionchange or a browser-side close leaves behind: the module's connection is gone, the store still thinks it is open.
        Assert.Null((await interop.CloseAsync()).Failure);

        var write = await store.ApplyAsync([new StoreChange.Put(Plan("plan_2", "after"))]);
        Assert.Null((await interop.CloseAsync()).Failure);
        var data = await store.LoadAllAsync();

        Assert.IsType<StoreResult<Unit>.Success>(write);
        Assert.Equal([Plan("plan_1", "before"), Plan("plan_2", "after")], Assert.IsType<StoreResult<StoredData>.Success>(data).Value.Records.OrderBy(r => r.Key));
    }

    [Fact]
    public async Task A_new_database_records_its_layout_version_in_meta()
    {
        var (_, store, _) = NewStore();

        var data = await Load(store);

        Assert.Equal(1, data.Metadata.LayoutVersion);
    }

    [Fact]
    public async Task A_database_created_at_layout_version_1_opens_and_keeps_its_data()
    {
        // Layout version 1 is written out longhand on purpose: this is a frozen copy of what shipped, not the module's own code.
        // The module is still at version 1, so this proves reading a pre-existing v1 database; when version 2 exists this
        // same test becomes the real upgrade test (keep it, and add a v2 expectation).
        var name = BrowserSession.NewDatabaseName();
        await _session.Page.EvaluateAsync(
            """
            ([name]) => new Promise((resolve, reject) => {
                const open = indexedDB.open(name, 1);
                open.onupgradeneeded = () => {
                    for (const kind of ['exercises', 'settings', 'plans', 'weighIns', 'breaks', 'badges', 'guides', 'workouts', 'weekSchedules', 'meta'])
                        open.result.createObjectStore(kind);
                };
                open.onerror = () => reject(open.error);
                open.onsuccess = () => {
                    const db = open.result;
                    const tx = db.transaction(['plans', 'meta'], 'readwrite');
                    tx.objectStore('plans').put('{"id":"plan_1"}', 'plan_1');
                    tx.objectStore('meta').put('true', 'initialised');
                    tx.oncomplete = () => { db.close(); resolve(); };
                    tx.onerror = () => reject(tx.error);
                };
            })
            """,
            new object[] { name });

        var (_, store, _) = NewStore(name);
        var data = await Load(store);

        Assert.Equal([Plan("plan_1", """{"id":"plan_1"}""")], data.Records);
        Assert.True(data.Metadata.Initialised);
    }

    [Fact]
    public async Task A_second_page_in_the_same_browser_cannot_take_the_writer_lock_while_the_first_holds_it()
    {
        const string module = "./js/storage-environment.js";
        var first = _session.Page;
        var second = await _session.NewPageAsync();

        Assert.Equal("obtained", await BrowserSession.CallModuleAsync<string>(first, module, "tryTakeWriterLock"));
        Assert.Equal("heldElsewhere", await BrowserSession.CallModuleAsync<string>(second, module, "tryTakeWriterLock"));
        Assert.Equal("obtained", await BrowserSession.CallModuleAsync<string>(first, module, "tryTakeWriterLock"));
    }

    [Fact]
    public async Task A_browser_without_web_locks_answers_unsupported()
    {
        const string module = "./js/storage-environment.js";
        var page = await _session.NewPageAsync();
        await page.EvaluateAsync("Object.defineProperty(navigator, 'locks', { value: undefined, configurable: true })");

        Assert.Equal("unsupported", await BrowserSession.CallModuleAsync<string>(page, module, "tryTakeWriterLock"));
    }

    [Fact]
    public async Task The_estimate_is_a_structured_value()
    {
        var estimate = await BrowserSession.CallModuleAsync<System.Text.Json.JsonElement>(_session.Page, "./js/storage-environment.js", "estimate");

        Assert.True(estimate.GetProperty("usage").GetInt64() >= 0);
        Assert.True(estimate.GetProperty("quota").GetInt64() > 0);
    }
}
