using Microsoft.Playwright;
using Web.Storage;

namespace Web.BrowserTests;

/// <summary>
/// A browser context serving the real <c>wwwroot/js</c> modules from a fake secure origin (no web server, nothing
/// extra in the Web host). A fixed origin means IndexedDB, localStorage and Web Locks behave as on the deployed site.
/// </summary>
public sealed class BrowserSession : IAsyncDisposable
{
    public const string Origin = "https://fitness.test/";

    private const string BlankPage = "<!doctype html><html><head><title>storage test</title></head><body></body></html>";

    private readonly List<string> _databases = [];

    private BrowserSession(IBrowserContext context, IPage page)
    {
        Context = context;
        Page = page;
    }

    public IBrowserContext Context { get; }

    /// <summary>The first page; its module state (the open database, the writer lock) is its own.</summary>
    public IPage Page { get; private set; }

    public static async Task<BrowserSession> StartAsync(IBrowser browser)
    {
        var context = await browser.NewContextAsync();
        await context.RouteAsync(Origin + "**", ServeAsync);
        var session = new BrowserSession(context, await context.NewPageAsync());
        await session.Page.GotoAsync(Origin);
        return session;
    }

    /// <summary>Another page in the same context (same origin, same storage, same lock namespace).</summary>
    public async Task<IPage> NewPageAsync()
    {
        var page = await Context.NewPageAsync();
        await page.GotoAsync(Origin);
        return page;
    }

    /// <summary>Reloads the page: the module's in-memory state is lost, stored data is not.</summary>
    public Task ReloadAsync() => Page.ReloadAsync();

    /// <summary>A fresh interop over a page.</summary>
    public IndexedDbInterop NewInterop(IPage? page = null) => new(new PlaywrightJsRuntime(page ?? Page));

    public static string NewDatabaseName() => $"test-{Guid.NewGuid():N}";

    /// <summary>Registers a database (opened through the first page) for deletion when the session ends.</summary>
    public void DestroyOnDispose(string databaseName) => _databases.Add(databaseName);

    public async ValueTask DisposeAsync()
    {
        // Test-only cleanup, so it calls the module's destroy (which closes the connection first) directly, not through the production wrapper.
        foreach (var name in _databases)
        {
            await CallModuleAsync<string>(Page, IndexedDbInterop.ModulePath, "destroy", name);
        }
        await Context.CloseAsync();
    }

    /// <summary>Calls an exported function of a module on the page and returns its result.</summary>
    public static async Task<T> CallModuleAsync<T>(IPage page, string module, string function, params object?[] args) =>
        await page.EvaluateAsync<T>(
            "async ([url, fn, args]) => { const m = await import(url); return await m[fn](...args); }",
            new object?[] { new Uri(new Uri(page.Url), module).ToString(), function, args });

    private static async Task ServeAsync(IRoute route)
    {
        var path = new Uri(route.Request.Url).AbsolutePath;
        if (path == "/")
        {
            await route.FulfillAsync(new RouteFulfillOptions { Status = 200, ContentType = "text/html", Body = BlankPage });
            return;
        }
        var file = Path.Combine(AppContext.BaseDirectory, "wwwroot", path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(file))
        {
            await route.FulfillAsync(new RouteFulfillOptions { Status = 200, ContentType = "text/javascript", Body = await File.ReadAllTextAsync(file) });
        }
        else
        {
            await route.FulfillAsync(new RouteFulfillOptions { Status = 404, Body = "not found" });
        }
    }
}
