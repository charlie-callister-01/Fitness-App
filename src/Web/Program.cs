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

await builder.Build().RunAsync();
