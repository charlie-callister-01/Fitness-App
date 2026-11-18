using Microsoft.Playwright;

namespace Web.BrowserTests;

/// <summary>
/// The real <c>wwwroot/js/theme.js</c> in headless Chromium: the pre-paint application of the mirrored choice, System following the
/// phone's colour scheme, the chrome colour, and a blocked mirror. The Blazor side (WebAssembly) is not run here; see docs/pwa-manual-checks.md.
/// </summary>
[Collection(BrowserCollection.Name)]
public sealed class ThemeScriptBrowserTests(BrowserFixture browser) : IAsyncLifetime
{
    private const string ThemePage = "theme-test.html";

    private BrowserSession _session = null!;
    private readonly List<string> _pageErrors = [];

    public async Task InitializeAsync()
    {
        _session = await browser.NewSessionAsync();
        // Same shape as index.html's head: a meta tag, then the synchronous script, then a body the script has not seen yet.
        await _session.Context.RouteAsync(BrowserSession.Origin + ThemePage, route => route.FulfillAsync(new RouteFulfillOptions
        {
            Status = 200,
            ContentType = "text/html",
            Body = "<!doctype html><html><head><meta name=\"theme-color\" content=\"#000000\"><script src=\"js/theme.js\"></script></head><body></body></html>",
        }));
        _session.Page.PageError += (_, message) => _pageErrors.Add(message);
    }

    public async Task DisposeAsync() => await _session.DisposeAsync();

    private async Task OpenAsync() => await _session.Page.GotoAsync(BrowserSession.Origin + ThemePage);

    private Task ApplyAsync(string theme, string mode) =>
        _session.Page.EvaluateAsync($"window.FitnessTheme.apply('{theme}', '{mode}')");

    private Task<string?> AttributeAsync(string name) => _session.Page.Locator("html").GetAttributeAsync(name);

    private Task<string?> ChromeColourAsync() => _session.Page.Locator("meta[name=theme-color]").GetAttributeAsync("content");

    [Fact]
    public async Task With_nothing_mirrored_the_page_is_Keep_and_follows_the_system_scheme()
    {
        await _session.Page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Dark });
        await OpenAsync();

        Assert.Equal("keep", await AttributeAsync("data-theme"));
        Assert.Equal("dark", await AttributeAsync("data-mode"));
        Assert.Equal("#4A4237", await ChromeColourAsync());
    }

    [Theory]
    [InlineData("keep", "dark", "#4A4237")]
    [InlineData("keep", "light", "#E8DFC8")]
    [InlineData("clean", "dark", "#111315")]
    [InlineData("clean", "light", "#F4F5F7")]
    public async Task A_choice_sets_both_attributes_and_the_chrome_colour_and_is_there_again_before_the_app_starts(string theme, string mode, string colour)
    {
        await OpenAsync();
        await ApplyAsync(theme, mode);

        Assert.Equal(theme, await AttributeAsync("data-theme"));
        Assert.Equal(mode, await AttributeAsync("data-mode"));
        Assert.Equal(colour, await ChromeColourAsync());

        // A new load: the synchronous script alone (no app) restores it from the mirror.
        await OpenAsync();
        Assert.Equal(theme, await AttributeAsync("data-theme"));
        Assert.Equal(mode, await AttributeAsync("data-mode"));
        Assert.Equal(colour, await ChromeColourAsync());
    }

    [Fact]
    public async Task System_follows_a_changed_colour_scheme_but_a_fixed_mode_does_not()
    {
        await _session.Page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Dark });
        await OpenAsync();
        await ApplyAsync("clean", "system");
        Assert.Equal("dark", await AttributeAsync("data-mode"));

        await _session.Page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Light });

        // The change event is delivered asynchronously, so wait for it.
        await Assertions.Expect(_session.Page.Locator("html")).ToHaveAttributeAsync("data-mode", "light");
        Assert.Equal("#F4F5F7", await ChromeColourAsync());

        await ApplyAsync("clean", "dark");
        await _session.Page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Dark });
        await _session.Page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Light });
        await _session.Page.EvaluateAsync("new Promise(resolve => setTimeout(resolve, 200))");
        Assert.Equal("dark", await AttributeAsync("data-mode"));
    }

    [Fact]
    public async Task An_unknown_choice_behaves_as_the_default()
    {
        await _session.Page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Dark });
        await OpenAsync();

        await ApplyAsync("neon", "sepia");

        Assert.Equal("keep", await AttributeAsync("data-theme"));
        Assert.Equal("dark", await AttributeAsync("data-mode"));
    }

    [Fact]
    public async Task A_blocked_mirror_falls_back_to_the_default_without_an_error()
    {
        await _session.Page.AddInitScriptAsync(
            "Storage.prototype.getItem = () => { throw new Error('blocked'); }; Storage.prototype.setItem = () => { throw new Error('blocked'); };");
        await _session.Page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Dark });

        await OpenAsync();
        await ApplyAsync("clean", "light");

        Assert.Equal("clean", await AttributeAsync("data-theme"));
        Assert.Equal("light", await AttributeAsync("data-mode"));
        Assert.Empty(_pageErrors);
    }

    [Fact]
    public async Task A_cleared_or_corrupt_mirror_is_the_default_without_an_error()
    {
        await _session.Page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Dark });
        await OpenAsync();
        await ApplyAsync("clean", "light");
        await _session.Page.EvaluateAsync("localStorage.setItem(Object.keys(localStorage)[0], '{not json')");

        await OpenAsync();

        Assert.Equal("keep", await AttributeAsync("data-theme"));
        Assert.Equal("dark", await AttributeAsync("data-mode"));
        Assert.Empty(_pageErrors);
    }

    [Fact]
    public void The_host_page_loads_the_script_synchronously_before_any_stylesheet()
    {
        var html = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "wwwroot", "index.html"));

        var script = html.IndexOf("<script src=\"js/theme.js\"></script>", StringComparison.Ordinal);
        var firstStylesheet = html.IndexOf("rel=\"stylesheet\"", StringComparison.Ordinal);

        Assert.True(script >= 0, "index.html must load js/theme.js as a plain synchronous script.");
        Assert.True(script < firstStylesheet, "theme.js must come before the first stylesheet so the attributes exist before first paint.");
    }
}
