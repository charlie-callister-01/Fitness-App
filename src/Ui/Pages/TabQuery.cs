namespace Ui.Pages;

/// <summary>Reads the optional <c>?tab=</c> query value so Home can open a screen on a given tab (orbs: progress?tab=calendar).</summary>
internal static class TabQuery
{
    /// <summary>The tab named by the query (case-insensitive) when it is one of <paramref name="tabs"/>, else <paramref name="fallback"/>.</summary>
    public static string Pick(string uri, IReadOnlyList<string> tabs, string fallback)
    {
        var requested = new Uri(uri).Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2)).Where(p => p.Length == 2 && p[0] == "tab").Select(p => Uri.UnescapeDataString(p[1])).FirstOrDefault();
        return tabs.FirstOrDefault(t => string.Equals(t, requested, StringComparison.OrdinalIgnoreCase)) ?? fallback;
    }
}
