using Core.Model;

namespace LegacyConverter;

internal static class GuideBuilder
{
    public static IReadOnlyList<Guide> Build(
        IReadOnlyDictionary<string, string> guideFiles,
        IReadOnlySet<string> knownExerciseIds,
        List<ConversionNote> notes)
    {
        var guidesByExerciseId = new Dictionary<string, Guide>(StringComparer.Ordinal);

        foreach (var fileName in guideFiles.Keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            var markdown = guideFiles[fileName];
            var (exerciseNames, template) = ParseGuide(fileName, markdown, notes);

            foreach (var exerciseName in exerciseNames)
            {
                var exerciseId = LegacyIds.ToId(exerciseName);
                if (exerciseId == "bodyweight")
                {
                    continue;
                }

                if (!knownExerciseIds.Contains(exerciseId))
                {
                    notes.Add(new ConversionNote(
                        ConversionNoteKind.Warning,
                        $"Guide: {fileName} -> {exerciseName}",
                        "Referenced Exercise is not in the Exercise library."));
                    continue;
                }

                guidesByExerciseId[exerciseId] = template with { ExerciseId = exerciseId };
            }
        }

        return guidesByExerciseId.Values
            .OrderBy(g => g.ExerciseId, StringComparer.Ordinal)
            .ToList();
    }

    internal static (IReadOnlyList<string> ExerciseNames, Guide Template) ParseGuide(
        string fileName,
        string markdown,
        List<ConversionNote> notes)
    {
        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var frontMatter = LegacyParsing.ParseFrontMatter(lines);
        var exerciseNames = frontMatter.Lists.TryGetValue("exercises", out var list) ? list : [];
        var bodyStart = frontMatter.BodyStartLineIndex;

        string? works = null;
        string? whyInPlan = null;
        string? setup = null;
        string? howTo = null;
        IReadOnlyList<string> cues = [];
        IReadOnlyList<string> commonMistakes = [];
        string? easier = null;
        string? harder = null;
        IReadOnlyList<string> watchOuts = [];
        string? personalNotes = null;

        var inCodeFence = false;
        string? currentSection = null;
        var sectionLines = new List<string>();

        void FlushSection()
        {
            if (currentSection is null)
            {
                return;
            }

            var content = JoinSection(sectionLines);
            switch (currentSection.ToLowerInvariant())
            {
                case "setup":
                    setup = content;
                    break;
                case "how to do it":
                    howTo = content;
                    break;
                case "cues":
                    cues = ParseBulletList(sectionLines);
                    break;
                case "common mistakes":
                    commonMistakes = ParseBulletList(sectionLines);
                    break;
                case "easier / harder":
                    (easier, harder) = ParseEasierHarder(sectionLines);
                    break;
                case "watch-outs for you":
                    watchOuts = ParseBulletList(sectionLines);
                    break;
                case "my notes":
                    personalNotes = ParsePersonalNotes(sectionLines);
                    break;
                default:
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        notes.Add(new ConversionNote(
                            ConversionNoteKind.Warning,
                            $"Guide: {fileName} — ## {currentSection}",
                            "Unknown Guide section (not mapped to a Guide field)."));
                    }

                    break;
            }

            sectionLines.Clear();
        }

        for (var i = bodyStart; i < lines.Length; i++)
        {
            var rawLine = lines[i];
            var trimmed = rawLine.Trim();

            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                inCodeFence = !inCodeFence;
                continue;
            }

            if (inCodeFence)
            {
                continue;
            }

            if (currentSection is null && trimmed.StartsWith("# ", StringComparison.Ordinal))
            {
                continue;
            }

            if (trimmed.StartsWith("## ", StringComparison.Ordinal))
            {
                FlushSection();
                currentSection = trimmed[3..].Trim();
                continue;
            }

            if (currentSection is not null)
            {
                sectionLines.Add(rawLine);
                continue;
            }

            if (TryExtractBoldPrefix(trimmed, "**Works:**", out var worksText))
            {
                works = worksText;
            }
            else if (TryExtractBoldPrefix(trimmed, "**Why it's in your plan:**", out var whyText))
            {
                whyInPlan = whyText;
            }
            else if (trimmed.StartsWith("**", StringComparison.Ordinal) && trimmed.Contains(":**", StringComparison.Ordinal))
            {
                var labelEnd = trimmed.IndexOf(":**", StringComparison.Ordinal);
                var label = trimmed[2..labelEnd].Trim();
                notes.Add(new ConversionNote(
                    ConversionNoteKind.Warning,
                    $"Guide: {fileName} — **{label}**",
                    "Unknown Guide preamble field (not mapped to a Guide field)."));
            }
        }

        FlushSection();

        var template = new Guide(
            ExerciseId: string.Empty,
            Works: works,
            WhyInPlan: whyInPlan,
            Setup: setup,
            HowTo: howTo,
            Cues: cues,
            CommonMistakes: commonMistakes,
            Easier: easier,
            Harder: harder,
            WatchOuts: watchOuts,
            VideoUrl: null,
            PersonalNotes: personalNotes);

        return (exerciseNames, template);
    }

    private static bool TryExtractBoldPrefix(string line, string prefix, out string value)
    {
        if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            value = line[prefix.Length..].Trim();
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static string? JoinSection(List<string> lines)
    {
        var text = string.Join('\n', lines).Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static IReadOnlyList<string> ParseBulletList(List<string> lines)
    {
        var items = new List<string>();
        foreach (var raw in lines)
        {
            var trimmed = raw.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (trimmed.StartsWith("- ", StringComparison.Ordinal))
            {
                var body = trimmed[2..].Trim();
                if (body.Length > 0)
                {
                    items.Add(body);
                }
            }
            else if (trimmed != "-")
            {
                items.Add(trimmed);
            }
        }

        return items;
    }

    private static (string? Easier, string? Harder) ParseEasierHarder(List<string> lines)
    {
        string? easier = null;
        string? harder = null;

        foreach (var raw in lines)
        {
            var trimmed = raw.Trim();
            if (trimmed.StartsWith("- ", StringComparison.Ordinal))
            {
                trimmed = trimmed[2..].Trim();
            }

            if (TryExtractBoldPrefix(trimmed, "**Easier:**", out var e))
            {
                easier = e;
            }
            else if (TryExtractBoldPrefix(trimmed, "**Harder:**", out var h))
            {
                harder = h;
            }
        }

        return (easier, harder);
    }

    private static string? ParsePersonalNotes(List<string> lines)
    {
        var nonEmpty = new List<string>();
        foreach (var raw in lines)
        {
            var trimmed = raw.Trim();
            if (trimmed.Length == 0 || trimmed == "-")
            {
                continue;
            }

            if (trimmed.StartsWith("- ", StringComparison.Ordinal))
            {
                var item = trimmed[2..].Trim();
                if (item.Length > 0)
                {
                    nonEmpty.Add(item);
                }
            }
            else
            {
                nonEmpty.Add(trimmed);
            }
        }

        return nonEmpty.Count == 0 ? null : string.Join('\n', nonEmpty);
    }
}
