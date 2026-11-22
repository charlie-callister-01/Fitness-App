using Core.Ids;
using Core.Time;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Ui;
using Ui.Browser;
using Ui.Log;
using Web.Browser;
using Web.WakeLock;
using Ui.Preferences;
using Ui.Saving;
using Ui.Theming;
using Web;
using Web.Storage;
using Web.Preferences;
using Web.Theming;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<IIdGenerator, GuidIdGenerator>();
builder.Services.AddSingleton<IBuildInfo, AssemblyBuildInfo>();
builder.Services.AddStorage();
builder.Services.AddSingleton<IThemeInterop, ThemeInterop>();
builder.Services.AddSingleton<SaveFailureNotice>();
builder.Services.AddSingleton<ThemeService>();
builder.Services.AddSingleton<IDevicePreferences, LocalStorageDevicePreferences>();
builder.Services.AddSingleton<RestTimerSetting>();
builder.Services.AddSingleton<InAppHistory>();
builder.Services.AddSingleton<LogEditStates>();
builder.Services.AddSingleton<ISecondTicker, SystemSecondTicker>();
builder.Services.AddSingleton<IWakeLock, BrowserWakeLock>();
builder.Services.AddSingleton<IBrowserCapabilities, BrowserCapabilities>();

// Ui injects AppData; its storage health is AppData.Health and a blocked open is AppData.Startup.Failure.IsBlockedByAnotherWindow.
var host = builder.Build();
await host.Services.OpenStorageAsync();
// Reconcile the page's Theme and Mode (and their mirror) with the stored Settings before the first render.
await host.Services.GetRequiredService<ThemeService>().InitialiseAsync();
// Created now so it sees every navigation from the start (the pushed pages' back arrow asks it).
host.Services.GetRequiredService<InAppHistory>();
await host.RunAsync();
