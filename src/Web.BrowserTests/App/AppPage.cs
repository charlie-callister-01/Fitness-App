using System.Globalization;
using Core.Model;
using Microsoft.Playwright;
using Ui.Theming;

namespace Web.BrowserTests.App;

/// <summary>
/// One open copy of the published app in its own browser context, already rendered with the chosen viewport, Theme, Mode, text scale,
/// reduced-motion preference and (optionally) the fixture Backup. Tests assert through <see cref="Page"/> and move between screens with
/// <see cref="GotoAsync"/>, which navigates inside the app as a link would (no reload).
/// </summary>
public sealed class AppPage : IAsyncDisposable
{
    /// <summary>The database name the production code opens; this host renames it per test.</summary>
    private const string ProductionDatabaseName = "fitness-app";

    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".html"] = "text/html",
        [".js"] = "text/javascript",
        [".mjs"] = "text/javascript",
        [".css"] = "text/css",
        [".json"] = "application/json",
        [".webmanifest"] = "application/manifest+json",
        [".wasm"] = "application/wasm",
        [".svg"] = "image/svg+xml",
        [".png"] = "image/png",
        [".woff2"] = "font/woff2",
        [".txt"] = "text/plain",
    };

    private readonly IBrowserContext _context;
    private readonly List<string> _errors = [];

    private AppPage(IBrowserContext context, IPage page, string databaseName)
    {
        _context = context;
        Page = page;
        DatabaseName = databaseName;
        page.PageError += (_, message) => _errors.Add(message);
        page.Console += (_, message) =>
        {
            if (message.Type == "error") _errors.Add(message.Text);
        };
    }

    public IPage Page { get; }

    /// <summary>The IndexedDB database this test's app uses (unique per test).</summary>
    public string DatabaseName { get; }

    /// <summary>Uncaught page errors and console errors seen so far.</summary>
    public IReadOnlyList<string> Errors => _errors;

    internal static async Task<AppPage> OpenAsync(IBrowser browser, string wwwroot, string route, AppPageOptions options)
    {
        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = options.Width, Height = options.Height },
            ReducedMotion = options.ReducedMotion ? ReducedMotion.Reduce : ReducedMotion.NoPreference,
            ColorScheme = ColorScheme.Dark,
            // The offline worker is not under test, and its fetches would bypass the routing below.
            ServiceWorkers = ServiceWorkerPolicy.Block,
        });
        await context.RouteAsync(BrowserSession.Origin + "**", route => ServeAsync(route, wwwroot));

        var databaseName = BrowserSession.NewDatabaseName();
        await context.AddInitScriptAsync(InitScript(databaseName, options.TextScale));

        var page = new AppPage(context, await context.NewPageAsync(), databaseName);
        try
        {
            await page.LoadAsync(options.WithFixture ? "/backup?tab=import&kind=restore" : "/settings");
            if (options.WithFixture)
            {
                await page.RestoreFixtureAsync();
                await page.GotoAsync("/settings");
            }

            await page.ChooseAppearanceAsync(options.Theme, options.Mode);
            await page.GotoAsync(route);
            return page;
        }
        catch
        {
            await page.DisposeAsync();
            throw;
        }
    }

    /// <summary>Moves to another screen inside the app (as a link would) and waits until it has rendered.</summary>
    public async Task GotoAsync(string route)
    {
        if (CurrentRoute() == route)
        {
            return;
        }

        // The screen being left is marked, so the wait cannot mistake it for the new one.
        await Page.EvaluateAsync("route => { const h = document.querySelector('#main-content h1'); if (h) h.__leaving = true; Blazor.navigateTo(route); }", route);
        await Page.WaitForFunctionAsync(
            "route => { const h = document.querySelector('#main-content h1'); return h !== null && !h.__leaving && location.pathname + location.search === route; }",
            route);
    }

    /// <summary>Reloads the page (the app starts again from stored data) and waits until it has rendered.</summary>
    public async Task ReloadAsync()
    {
        await Page.ReloadAsync();
        await WaitForRenderAsync();
    }

    public async ValueTask DisposeAsync() => await _context.CloseAsync();

    private string CurrentRoute()
    {
        var uri = new Uri(Page.Url);
        return uri.AbsolutePath + uri.Query;
    }

    private async Task LoadAsync(string route)
    {
        await Page.GotoAsync(new Uri(new Uri(BrowserSession.Origin), route).ToString());
        await WaitForRenderAsync();
    }

    // Every screen has an h1 in the layout's main region once Blazor has rendered (the loading placeholder has none).
    private Task WaitForRenderAsync() => Page.Locator("#main-content h1").First.WaitForAsync();

    /// <summary>The real Restore path: paste the Backup on the Import tab, Check, Apply.</summary>
    private async Task RestoreFixtureAsync()
    {
        await Page.Locator("#import-paste").FillAsync(FixtureBackup.Json);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Check", Exact = true }).ClickAsync();
        var apply = Page.GetByTestId("import-preview").GetByRole(AriaRole.Button, new() { Name = "Apply", Exact = true });
        try
        {
            await Assertions.Expect(apply).ToBeEnabledAsync(new() { Timeout = 10_000 });
        }
        catch (PlaywrightException)
        {
            throw new InvalidOperationException($"The fixture Backup cannot be applied: {await Page.Locator("#main-content").InnerTextAsync()}");
        }

        await apply.ClickAsync();

        var applied = Page.GetByTestId("import-applied");
        var errors = Page.GetByTestId("import-errors");
        await applied.Or(errors).First.WaitForAsync();
        if (await errors.IsVisibleAsync())
        {
            throw new InvalidOperationException($"The fixture Backup did not restore: {await errors.InnerTextAsync()}");
        }
    }

    /// <summary>The real preference mechanism: the Theme and Mode choices on the Settings screen.</summary>
    private async Task ChooseAppearanceAsync(Theme theme, Mode mode)
    {
        await Page.GetByRole(AriaRole.Group, new() { Name = "Theme", Exact = true })
            .GetByRole(AriaRole.Button, new() { Name = AppearanceLabels.Themes[theme], Exact = true }).ClickAsync();
        await Page.GetByRole(AriaRole.Group, new() { Name = "Mode", Exact = true })
            .GetByRole(AriaRole.Button, new() { Name = AppearanceLabels.Modes[mode], Exact = true }).ClickAsync();

        var html = Page.Locator("html");
        await Assertions.Expect(html).ToHaveAttributeAsync("data-theme", theme.ToString().ToLowerInvariant());
        if (mode != Mode.System)
        {
            await Assertions.Expect(html).ToHaveAttributeAsync("data-mode", mode.ToString().ToLowerInvariant());
        }
    }

    // Runs before the page's own scripts on every load: a per-test database name, and the root text scale once the document exists.
    private static string InitScript(string databaseName, double textScale)
    {
        var percent = (textScale * 100).ToString(CultureInfo.InvariantCulture);
        return $$"""
            (() => {
              const rename = name => name === '{{ProductionDatabaseName}}' ? '{{databaseName}}' : name;
              const open = IDBFactory.prototype.open;
              IDBFactory.prototype.open = function (name, version) { return version === undefined ? open.call(this, rename(name)) : open.call(this, rename(name), version); };
              const remove = IDBFactory.prototype.deleteDatabase;
              IDBFactory.prototype.deleteDatabase = function (name) { return remove.call(this, rename(name)); };
              if ({{percent}} !== 100) {
                document.addEventListener('DOMContentLoaded', () => { document.documentElement.style.fontSize = '{{percent}}%'; });
              }
            })();
            """;
    }

    // Serves the published wwwroot. A path with no extension is one of the app's routes, so it gets index.html (as the deployed 404
    // fallback does); a missing file is a 404.
    private static async Task ServeAsync(IRoute route, string wwwroot)
    {
        var path = Uri.UnescapeDataString(new Uri(route.Request.Url).AbsolutePath).TrimStart('/');
        var file = Path.HasExtension(path) ? Path.GetFullPath(Path.Combine(wwwroot, path)) : Path.Combine(wwwroot, "index.html");
        if (!file.StartsWith(wwwroot, StringComparison.Ordinal) || !File.Exists(file))
        {
            await route.FulfillAsync(new RouteFulfillOptions { Status = 404, Body = "not found" });
            return;
        }

        await route.FulfillAsync(new RouteFulfillOptions
        {
            Status = 200,
            ContentType = ContentTypes.GetValueOrDefault(Path.GetExtension(file), "application/octet-stream"),
            BodyBytes = await File.ReadAllBytesAsync(file),
        });
    }
}
