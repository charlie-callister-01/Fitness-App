using System.Text.RegularExpressions;

namespace Web.Tests;

/// <summary>
/// Every static file type the Ui and Web projects ship must be matched by the published service worker's
/// include list, otherwise it works online and silently vanishes in airplane mode.
/// </summary>
public class OfflineCacheCoverageTests
{
    private static readonly string[] ExcludedFromScan = ["service-worker.js", "service-worker.published.js"];

    [Fact]
    public void EveryShippedStaticFileIsMatchedByTheServiceWorkerIncludeList()
    {
        var root = FindRepoRoot();
        var patterns = ReadIncludePatterns(Path.Combine(root, "src", "Web", "wwwroot", "service-worker.published.js"));

        var unmatched = new[] { "src/Ui/wwwroot", "src/Web/wwwroot" }
            .SelectMany(dir => Directory.EnumerateFiles(Path.Combine(root, dir), "*", SearchOption.AllDirectories))
            .Where(file => !ExcludedFromScan.Contains(Path.GetFileName(file)))
            .Where(file => !patterns.Any(p => p.IsMatch(file.Replace('\\', '/'))))
            .Select(file => Path.GetRelativePath(root, file).Replace('\\', '/'))
            .Order()
            .ToList();

        Assert.True(unmatched.Count == 0, "Not matched by offlineAssetsInclude: " + string.Join(", ", unmatched));
    }

    private static List<Regex> ReadIncludePatterns(string workerPath)
    {
        var line = File.ReadAllLines(workerPath).Single(l => l.TrimStart().StartsWith("const offlineAssetsInclude", StringComparison.Ordinal));
        var list = line[(line.IndexOf('[') + 1)..line.LastIndexOf(']')];
        var patterns = list.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(p => new Regex(p.Trim('/')))
            .ToList();
        Assert.NotEmpty(patterns);
        return patterns;
    }

    private static string FindRepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "FitnessApp.sln"))) return dir.FullName;
        }
        throw new InvalidOperationException("Repo root not found.");
    }
}
