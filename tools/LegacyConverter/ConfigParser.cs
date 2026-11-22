namespace LegacyConverter;

internal sealed record ConfigPhaseRow(string Focus, int FromWeek, int ToWeek);

internal sealed record ConfigSessionRow(string Code, string Name, string Day, string Type);

internal sealed record ConfigExerciseRow(
    IReadOnlyList<string> SessionCodes,
    string Name,
    string SetsReps,
    string LogAs,
    string Cue);

internal sealed record ConfigBadgeRow(string Name, string ExerciseOrMetric, string Target);

internal sealed record ParsedConfig(
    IReadOnlyDictionary<string, string> Settings,
    IReadOnlyList<ConfigPhaseRow> Phases,
    IReadOnlyList<ConfigSessionRow> Sessions,
    IReadOnlyList<ConfigExerciseRow> Exercises,
    IReadOnlyList<ConfigBadgeRow> Badges);

internal static class ConfigParser
{
    public static ParsedConfig Parse(string markdown)
    {
        var sections = SplitSections(markdown);

        var settings = ParseSettings(GetRequiredSection(sections, "Settings"));
        var phases = ParsePhases(GetRequiredSection(sections, "Phases"));
        var sessions = ParseSessions(GetRequiredSection(sections, "Sessions"));
        var exercises = ParseExercises(GetRequiredSection(sections, "Exercises"));
        var badges = ParseBadges(GetRequiredSection(sections, "Badges"));

        return new ParsedConfig(settings, phases, sessions, exercises, badges);
    }

    private static Dictionary<string, List<string>> SplitSections(string markdown)
    {
        var sections = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        string? current = null;

        foreach (var rawLine in markdown.Replace("\r\n", "\n").Split('\n'))
        {
            if (rawLine.StartsWith("## ", StringComparison.Ordinal))
            {
                current = rawLine[3..].Trim();
                sections[current] = [];
                continue;
            }

            if (current is not null)
            {
                sections[current].Add(rawLine);
            }
        }

        return sections;
    }

    private static List<string> GetRequiredSection(Dictionary<string, List<string>> sections, string name)
    {
        if (!sections.TryGetValue(name, out var lines))
        {
            throw new FormatException($"Config.md is missing required '## {name}' section.");
        }

        return lines;
    }

    private static Dictionary<string, string> ParseSettings(List<string> lines)
    {
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith("- ", StringComparison.Ordinal))
            {
                continue;
            }

            var body = trimmed[2..];
            var colon = body.IndexOf(':');
            if (colon <= 0)
            {
                continue;
            }

            var key = body[..colon].Trim();
            var value = body[(colon + 1)..].Trim();
            settings[key] = value;
        }

        return settings;
    }

    private static List<ConfigPhaseRow> ParsePhases(List<string> lines)
    {
        var rows = new List<ConfigPhaseRow>();
        foreach (var cells in ParseTableRows(lines, expectedColumns: 3))
        {
            rows.Add(new ConfigPhaseRow(
                cells[0],
                int.Parse(cells[1], System.Globalization.CultureInfo.InvariantCulture),
                int.Parse(cells[2], System.Globalization.CultureInfo.InvariantCulture)));
        }

        return rows;
    }

    private static List<ConfigSessionRow> ParseSessions(List<string> lines)
    {
        var rows = new List<ConfigSessionRow>();
        foreach (var cells in ParseTableRows(lines, expectedColumns: 4))
        {
            rows.Add(new ConfigSessionRow(cells[0], cells[1], cells[2], cells[3]));
        }

        return rows;
    }

    private static List<ConfigExerciseRow> ParseExercises(List<string> lines)
    {
        var rows = new List<ConfigExerciseRow>();
        foreach (var cells in ParseTableRows(lines, expectedColumns: 5))
        {
            var codes = cells[0]
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToArray();
            rows.Add(new ConfigExerciseRow(codes, cells[1], cells[2], cells[3], cells[4]));
        }

        return rows;
    }

    private static List<ConfigBadgeRow> ParseBadges(List<string> lines)
    {
        var rows = new List<ConfigBadgeRow>();
        foreach (var cells in ParseTableRows(lines, expectedColumns: 3))
        {
            rows.Add(new ConfigBadgeRow(cells[0], cells[1], cells[2]));
        }

        return rows;
    }

    internal static IReadOnlyList<string[]> ParseTableRows(IEnumerable<string> lines, int expectedColumns)
    {
        var rows = new List<string[]>();
        var headerSeen = false;
        var separatorSeen = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (!line.StartsWith('|') || !line.EndsWith('|'))
            {
                continue;
            }

            var inner = line[1..^1];
            var cells = inner.Split('|').Select(c => c.Trim()).ToArray();
            if (cells.Length != expectedColumns)
            {
                throw new FormatException(
                    $"Expected {expectedColumns} columns in markdown table row, found {cells.Length}: '{line}'.");
            }

            if (!headerSeen)
            {
                headerSeen = true;
                continue;
            }

            if (!separatorSeen)
            {
                separatorSeen = true;
                continue;
            }

            rows.Add(cells);
        }

        return rows;
    }
}
