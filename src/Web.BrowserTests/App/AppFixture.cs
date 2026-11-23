using Microsoft.Playwright;

namespace Web.BrowserTests.App;

/// <summary>
/// The whole-app host: the published app (one Release publish per run) and one headless Chromium. Each <see cref="OpenAsync"/> is a new
/// browser context serving the published files from the fake origin, so every test starts with its own empty storage.
/// </summary>
public sealed class AppFixture : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private string _wwwroot = string.Empty;

    public async Task InitializeAsync()
    {
        _wwwroot = await PublishedApp.WwwrootAsync();
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    public async Task DisposeAsync()
    {
        if (_browser is not null) await _browser.CloseAsync();
        _playwright?.Dispose();
    }

    /// <summary>Opens the app at <paramref name="route"/> (for example <c>/</c> or <c>/plans</c>) once it has rendered.</summary>
    public Task<AppPage> OpenAsync(string route, AppPageOptions? options = null) =>
        AppPage.OpenAsync(_browser!, _wwwroot, route, options ?? new AppPageOptions());
}

[CollectionDefinition(Name)]
public sealed class AppCollection : ICollectionFixture<AppFixture>
{
    public const string Name = "whole app";
}
