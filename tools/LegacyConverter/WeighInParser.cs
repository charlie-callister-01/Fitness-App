using System.Globalization;
using Core.Model;

namespace LegacyConverter;

internal static class WeighInParser
{
    public static void AddWeighIn(
        SortedDictionary<DateOnly, WeighIn> weighInsByDate,
        DateOnly date,
        double kg)
    {
        weighInsByDate[date] = new WeighIn($"w-{date:yyyy-MM-dd}", date, kg);
    }

    public static void AddFromLifeSettings(
        string? lifeSettingsMarkdown,
        SortedDictionary<DateOnly, WeighIn> weighInsByDate)
    {
        if (string.IsNullOrWhiteSpace(lifeSettingsMarkdown))
        {
            return;
        }

        var lifeFm = LegacyParsing.ParseFrontMatter(lifeSettingsMarkdown);
        if (lifeFm.Scalars.TryGetValue("start_weight", out var weightStr)
            && lifeFm.Scalars.TryGetValue("start_weight_date", out var dateStr)
            && double.TryParse(weightStr, CultureInfo.InvariantCulture, out var kg)
            && DateOnly.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            AddWeighIn(weighInsByDate, date, kg);
        }
    }

    public static void AddFromWeightsCsv(
        string? weightsCsv,
        SortedDictionary<DateOnly, WeighIn> weighInsByDate)
    {
        if (string.IsNullOrWhiteSpace(weightsCsv))
        {
            return;
        }

        foreach (var rawLine in weightsCsv.Replace("\r\n", "\n").Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("date,", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var parts = line.Split(',');
            if (parts.Length < 2)
            {
                throw new FormatException($"Invalid weights.csv row: '{line}'.");
            }

            var date = DateOnly.ParseExact(parts[0].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var kg = double.Parse(parts[1].Trim(), CultureInfo.InvariantCulture);
            AddWeighIn(weighInsByDate, date, kg);
        }
    }
}
