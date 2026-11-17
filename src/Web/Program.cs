using Core.Time;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Web;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<Ui.IBuildInfo, AssemblyBuildInfo>();
builder.Services.AddScoped<Web.Storage.IndexedDbInterop>(); // TODO(ticket 07): register IndexedDbDataStore as IDataStore and wire startup

await builder.Build().RunAsync();
