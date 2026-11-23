namespace Web.BrowserTests.App;

/// <summary>
/// The single list of axe findings the scans ignore. Only third-party markup or a proven false positive belongs here, each with
/// its rule, the element's selector as axe reports it, and a one-line reason. Everything else is fixed in the app.
/// </summary>
public static class AxeSuppressions
{
    public static IReadOnlyList<AxeSuppression> All { get; } =
    [
    ];

    public static bool Covers(AxeFinding finding) =>
        All.Any(s => s.Rule == finding.Rule && s.Selector == finding.Selector);
}

/// <param name="Rule">The axe rule id, for example <c>color-contrast</c>.</param>
/// <param name="Selector">The element's selector exactly as axe reports it.</param>
/// <param name="Reason">Why this is third-party or a false positive, in one line.</param>
public sealed record AxeSuppression(string Rule, string Selector, string Reason);
