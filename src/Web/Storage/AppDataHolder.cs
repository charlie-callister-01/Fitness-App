using Core.Storage;

namespace Web.Storage;

/// <summary>
/// Carries the opened <see cref="AppData"/> from startup to dependency injection. <c>AppData.OpenAsync</c> is asynchronous and needs
/// the JavaScript runtime, which exists only once the host is built, so the host registers a factory that reads this holder and
/// <c>Program.cs</c> calls <see cref="OpenAsync"/> before <c>RunAsync</c>: nothing renders until the data is loaded.
/// </summary>
public sealed class AppDataHolder(IDataStore store, IStorageEnvironment environment)
{
    private AppData? _appData;

    /// <summary>The opened data. Throws if read before <see cref="OpenAsync"/> finished (a wiring bug, not a runtime condition).</summary>
    public AppData AppData => _appData ?? throw new InvalidOperationException("Storage has not been opened yet.");

    public async Task OpenAsync() => _appData = await AppData.OpenAsync(store, environment);
}
