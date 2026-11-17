using Core.Time;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Web;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<Ui.IBuildInfo, AssemblyBuildInfo>();
builder.Services.AddSingleton<Web.Storage.IndexedDbInterop>();
builder.Services.AddSingleton<Web.Storage.IIndexedDbInterop>(sp => sp.GetRequiredService<Web.Storage.IndexedDbInterop>());
builder.Services.AddSingleton<Core.Storage.IDataStore>(sp => new Web.Storage.IndexedDbDataStore(sp.GetRequiredService<Web.Storage.IIndexedDbInterop>()));
builder.Services.AddSingleton<Web.Storage.StorageEnvironmentInterop>();
builder.Services.AddSingleton<Web.Storage.IStorageEnvironmentInterop>(sp => sp.GetRequiredService<Web.Storage.StorageEnvironmentInterop>());
builder.Services.AddSingleton<Core.Storage.IStorageEnvironment, Web.Storage.BrowserStorageEnvironment>();

// AppData is opened asynchronously (it needs the JavaScript runtime), so a holder carries it from startup to injection.
// Ui injects AppData; its storage health is AppData.Health and a blocked open is AppData.Startup.Failure.IsBlockedByAnotherWindow.
builder.Services.AddSingleton<Web.Storage.AppDataHolder>();
builder.Services.AddSingleton(sp => sp.GetRequiredService<Web.Storage.AppDataHolder>().AppData);

var host = builder.Build();
await host.Services.GetRequiredService<Web.Storage.AppDataHolder>().OpenAsync();
await host.RunAsync();
