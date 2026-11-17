using Core.Storage;
using Core.Tests.Storage;
using Web.Storage;

namespace Web.BrowserTests;

/// <summary>
/// The shared <see cref="DataStoreContract"/> (compiled once, in Core.Tests) run against the real IndexedDB module in
/// headless Chromium. Every contract case runs here: SHARED, not mirrored. Cases that inject faults through
/// <c>FaultInjectingDataStore</c> exercise the wrapper over the real store; the genuine browser rollbacks are in
/// <see cref="IndexedDbBrowserBehaviourTests"/>.
/// </summary>
[Collection(BrowserCollection.Name)]
public sealed class IndexedDbDataStoreContractTests(BrowserFixture browser) : DataStoreContract, IAsyncLifetime
{
    private BrowserSession? _session;

    public async Task InitializeAsync() => _session = await browser.NewSessionAsync();

    public async Task DisposeAsync()
    {
        if (_session is not null) await _session.DisposeAsync();
    }

    protected override int? StampedLayoutVersion => 1;

    protected override IDataStore CreateEmptyStore()
    {
        var interop = _session!.NewInterop();
        var name = BrowserSession.NewDatabaseName();
        _session.DestroyOnDispose(name);
        return new IndexedDbDataStore(interop, name);
    }
}
