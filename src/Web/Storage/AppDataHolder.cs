using Core.Storage;

namespace Web.Storage;

/// <summary>
/// Carries the opened <see cref="AppData"/> from startup to dependency injection (see <c>AddStorage</c>). Reading <see cref="AppData"/>
/// before <see cref="OpenAsync"/> has finished is a wiring bug, not a runtime condition, so it throws; <c>OpenStorageAsync</c> runs
/// before <c>RunAsync</c>, so no component can be created earlier.
/// </summary>
public sealed class AppDataHolder(IDataStore store, IStorageEnvironment environment)
{
    private AppData? _appData;

    public AppData AppData => _appData ?? throw new InvalidOperationException("Storage has not been opened yet.");

    public async Task OpenAsync() => _appData = await AppData.OpenAsync(store, environment);
}
