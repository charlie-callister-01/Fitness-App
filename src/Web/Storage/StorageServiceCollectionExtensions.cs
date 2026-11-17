using Core.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Web.Storage;

public static class StorageServiceCollectionExtensions
{
    /// <summary>
    /// Registers browser storage: the IndexedDB data store, the browser environment and <see cref="AppData"/>. <c>AppData.OpenAsync</c>
    /// is asynchronous and needs the JavaScript runtime, which exists only once the host is built, so <see cref="AppData"/> comes
    /// from an <see cref="AppDataHolder"/> that <see cref="OpenStorageAsync"/> fills before the host runs.
    /// </summary>
    public static IServiceCollection AddStorage(this IServiceCollection services) => services
        .AddSingleton<IIndexedDbInterop, IndexedDbInterop>()
        .AddSingleton<IDataStore>(provider => new IndexedDbDataStore(provider.GetRequiredService<IIndexedDbInterop>()))
        .AddSingleton<IStorageEnvironmentInterop, StorageEnvironmentInterop>()
        .AddSingleton<IStorageEnvironment, BrowserStorageEnvironment>()
        .AddSingleton<AppDataHolder>()
        .AddSingleton(provider => provider.GetRequiredService<AppDataHolder>().AppData);

    /// <summary>Opens storage and loads the data. Call once, on the built host's services, after <c>Build()</c> and before <c>RunAsync()</c>, so nothing renders before the data is loaded.</summary>
    public static Task OpenStorageAsync(this IServiceProvider services) =>
        services.GetRequiredService<AppDataHolder>().OpenAsync();
}
