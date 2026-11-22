namespace LegacyConverter;

internal static class LegacyFolderLoader
{
    public static LegacyFolder Load(string fitnessFolder)
    {
        if (!Directory.Exists(fitnessFolder))
        {
            throw new DirectoryNotFoundException($"Fitness folder not found: '{fitnessFolder}'.");
        }

        var configPath = Path.Combine(fitnessFolder, "Config.md");
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException("Config.md not found in fitness folder.", configPath);
        }

        var config = File.ReadAllText(configPath);
        var guides = ReadMarkdownFolder(Path.Combine(fitnessFolder, "Exercises"));
        var logs = ReadMarkdownFolder(Path.Combine(fitnessFolder, "Log"));

        var lifeSettings = FindOptionalFile(
            Path.Combine(fitnessFolder, "Life Settings.md"),
            Path.GetFullPath(Path.Combine(fitnessFolder, "..", "_System", "Life Settings.md")),
            Path.GetFullPath(Path.Combine(fitnessFolder, "..", "Life Settings.md")));

        var weightsCsv = FindOptionalFile(Path.Combine(fitnessFolder, "weights.csv"));

        return new LegacyFolder(config, guides, logs, lifeSettings, weightsCsv);
    }

    private static IReadOnlyDictionary<string, string> ReadMarkdownFolder(string folder)
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!Directory.Exists(folder))
        {
            return files;
        }

        foreach (var path in Directory.GetFiles(folder, "*.md").OrderBy(p => p, StringComparer.Ordinal))
        {
            files[Path.GetFileName(path)] = File.ReadAllText(path);
        }

        return files;
    }

    private static string? FindOptionalFile(params string[] candidatePaths)
    {
        foreach (var path in candidatePaths)
        {
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }
        }

        return null;
    }
}
