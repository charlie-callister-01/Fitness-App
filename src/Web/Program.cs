using Core.Time;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Ui;
using Web;
using Web.Storage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<IBuildInfo, AssemblyBuildInfo>();
builder.Services.AddStorage();

// Ui injects AppData; its storage health is AppData.Health and a blocked open is AppData.Startup.Failure.IsBlockedByAnotherWindow.
var host = builder.Build();
await host.Services.OpenStorageAsync();
await host.RunAsync();
