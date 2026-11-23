using System.Text.Json;
using Microsoft.Playwright;

namespace Web.BrowserTests.App;

/// <summary>
/// Runs axe-core in an open page against the WCAG 2.0 and 2.1 A and AA tags. The script is the pinned copy embedded in the
/// Deque.AxeCore.Commons package, injected at run time, so nothing is fetched from a CDN.
/// </summary>
public static class Axe
{
    private static readonly string[] Tags = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"];

    private static readonly Lazy<string> Script = new(() =>
    {
        var assembly = typeof(Deque.AxeCore.Commons.AxeResult).Assembly;
        using var stream = assembly.GetManifestResourceStream("Deque.AxeCore.Commons.Resources.axe.min.js")
            ?? throw new InvalidOperationException("axe.min.js is not embedded in Deque.AxeCore.Commons.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    });

    /// <summary>Scans the whole document once animations have settled and returns every violation, one entry per element.</summary>
    public static async Task<IReadOnlyList<AxeFinding>> ScanAsync(IPage page)
    {
        await page.EvaluateAsync("() => Promise.all(document.getAnimations().map(a => a.finished.catch(() => null)))");
        if (!await page.EvaluateAsync<bool>("() => typeof window.axe !== 'undefined'"))
        {
            await page.EvaluateAsync(Script.Value);
        }

        var json = await page.EvaluateAsync<string>(
            """
            async tags => {
              const results = await axe.run(document, { runOnly: { type: 'tag', values: tags }, resultTypes: ['violations'] });
              return JSON.stringify(results.violations.flatMap(v => v.nodes.map(n => ({
                rule: v.id, impact: n.impact || v.impact, selector: n.target.join(' '), html: n.html, summary: n.failureSummary || v.help }))));
            }
            """,
            Tags);
        return JsonSerializer.Deserialize<List<AxeFinding>>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? [];
    }
}

/// <summary>One axe violation on one element.</summary>
public sealed record AxeFinding(string Rule, string Impact, string Selector, string Html, string Summary)
{
    /// <summary>Serious and critical violations fail the scan; moderate and minor go to the refactor backlog.</summary>
    public bool IsBlocking => Impact is "serious" or "critical";

    public override string ToString() => $"[{Impact}] {Rule} at {Selector}: {Html}\n    {Summary.ReplaceLineEndings(" ")}";
}
