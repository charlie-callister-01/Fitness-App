using System.Globalization;

namespace LegacyConverter;

internal sealed record ParsedFrontMatter(
    IReadOnlyDictionary<string, string> Scalars,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Lists,
    int BodyStartLineIndex);

internal static class LegacyParsing
{
    public static ParsedFrontMatter ParseFrontMatter(string markdown)
    {
        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        return ParseFrontMatter(lines);
    }

    public static ParsedFrontMatter ParseFrontMatter(string[] lines)
    {
        var scalars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lists = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        if (lines.Length == 0 || lines[0].Trim() != "---")
        {
            return new ParsedFrontMatter(
                scalars,
                new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase),
                0);
        }

        string? currentListKey = null;
        var i = 1;
        for (; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed == "---")
            {
                i++;
                break;
            }

            if (currentListKey is not null && trimmed.StartsWith("- ", StringComparison.Ordinal))
            {
                var item = trimmed[2..].Trim().Trim('"', '\'');
                if (item.Length > 0)
                {
                    lists[currentListKey].Add(item);
                }

                continue;
            }

            currentListKey = null;

            var colon = trimmed.IndexOf(':');
            if (colon <= 0)
            {
                continue;
            }

            var key = trimmed[..colon].Trim();
            var value = trimmed[(colon + 1)..].Trim();

            if (value.Length == 0)
            {
                currentListKey = key;
                if (!lists.ContainsKey(key))
                {
                    lists[key] = [];
                }

                continue;
            }

            scalars[key] = value.Trim('"', '\'');
        }

        var readOnlyLists = lists.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<string>)kvp.Value,
            StringComparer.OrdinalIgnoreCase);

        return new ParsedFrontMatter(scalars, readOnlyLists, i);
    }

    public static bool TryParseMinutesSeconds(string raw, out int totalSeconds)
    {
        totalSeconds = 0;
        var parts = raw.Trim().Split(':');
        if (parts.Length != 2
            || !int.TryParse(parts[0], CultureInfo.InvariantCulture, out var mins)
            || !int.TryParse(parts[1], CultureInfo.InvariantCulture, out var secs)
            || mins < 0
            || secs is < 0 or >= 60)
        {
            return false;
        }

        totalSeconds = (mins * 60) + secs;
        return true;
    }
}
