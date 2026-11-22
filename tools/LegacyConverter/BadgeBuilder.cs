using System.Globalization;
using Core.Model;

namespace LegacyConverter;

internal static class BadgeBuilder
{
    public static IReadOnlyList<Badge> Build(
        IReadOnlyList<ConfigBadgeRow> rows,
        IReadOnlyDictionary<string, Exercise> exercisesById,
        List<ConversionNote> notes)
    {
        var badges = new List<Badge>();

        foreach (var row in rows)
        {
            if (string.Equals(row.Target, "bodyweight", StringComparison.OrdinalIgnoreCase))
            {
                notes.Add(new ConversionNote(
                    ConversionNoteKind.Dropped,
                    $"Badge: {row.Name}",
                    "Target 'bodyweight' is not a numeric target in the app."));
                continue;
            }

            var metric = MapMetric(row.ExerciseOrMetric, exercisesById);
            var target = ParseTarget(row.Target, metric, exercisesById, row.Name);

            badges.Add(new Badge(
                Id: LegacyIds.ToId(row.Name),
                Name: row.Name,
                Metric: metric,
                Target: target));
        }

        return badges;
    }

    private static string MapMetric(
        string raw,
        IReadOnlyDictionary<string, Exercise> exercisesById)
    {
        switch (raw.Trim().ToLowerInvariant())
        {
            case "sessions": return "sessions";
            case "best week" or "bestweek": return "bestweek";
            case "streak": return "streak";
            case "prs": return "prs";
            case "mobility streak": return "streak:mobility";
            case "mobility days": return "days:mobility";
        }

        var id = LegacyIds.ToId(raw);
        if (!exercisesById.ContainsKey(id))
        {
            throw new FormatException($"Badge references unknown exercise or metric '{raw}'.");
        }

        return id;
    }

    private static double ParseTarget(
        string raw,
        string metric,
        IReadOnlyDictionary<string, Exercise> exercisesById,
        string badgeName)
    {
        var trimmed = raw.Trim();

        // m:ss format (e.g. "4:30" -> 270 seconds)
        if (trimmed.Contains(':', StringComparison.Ordinal))
        {
            var parts = trimmed.Split(':');
            if (parts.Length != 2
                || !int.TryParse(parts[0], CultureInfo.InvariantCulture, out var mins)
                || !int.TryParse(parts[1], CultureInfo.InvariantCulture, out var secs)
                || secs is < 0 or >= 60)
            {
                throw new FormatException($"Unreadable time target '{raw}' on badge '{badgeName}'.");
            }

            return (mins * 60) + secs;
        }

        if (!double.TryParse(trimmed, CultureInfo.InvariantCulture, out var value))
        {
            throw new FormatException($"Unreadable numeric target '{raw}' on badge '{badgeName}'.");
        }

        // Minutes exercises store targets in canonical seconds
        if (exercisesById.TryGetValue(metric, out var ex) && ex.LogKind == LogKind.Minutes)
        {
            return value * 60;
        }

        return value;
    }
}
