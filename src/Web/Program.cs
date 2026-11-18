using Core.Time;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Ui;
using Ui.Theming;
using Web;
using Web.Storage;
using Web.Theming;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<IBuildInfo, AssemblyBuildInfo>();
builder.Services.AddStorage();
builder.Services.AddSingleton<IThemeInterop, ThemeInterop>();
builder.Services.AddSingleton<ThemeService>();

// Ui injects AppData; its storage health is AppData.Health and a blocked open is AppData.Startup.Failure.IsBlockedByAnotherWindow.
var host = builder.Build();
await host.Services.OpenStorageAsync();
// Reconcile the page's Theme and Mode (and their mirror) with the stored Settings before the first render.
await host.Services.GetRequiredService<ThemeService>().InitialiseAsync();
await host.RunAsync();
