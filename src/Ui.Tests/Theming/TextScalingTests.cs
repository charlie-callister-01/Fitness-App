using System.Text.RegularExpressions;

namespace Ui.Tests.Theming;

/// <summary>Text grows with the browser and OS text size: no font size or line height in the Ui styles is fixed in px (design §9, Text scaling).</summary>
public sealed partial class TextScalingTests
{
    [Fact]
    public void No_font_size_or_line_height_in_the_Ui_styles_is_in_px()
    {
        var offenders = new List<string>();
        foreach (var file in UiStyleSheets())
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                foreach (Match declaration in TextSizeDeclaration().Matches(lines[i]))
                {
                    if (Px().IsMatch(declaration.Groups["value"].Value))
                    {
                        offenders.Add($"{Path.GetFileName(file)}:{i + 1}: {declaration.Value.Trim()}");
                    }
                }
            }
        }

        Assert.True(offenders.Count == 0, string.Join(Environment.NewLine, offenders));
    }

    [Fact]
    public void The_check_sees_the_Ui_stylesheets()
    {
        var names = UiStyleSheets().Select(Path.GetFileName).ToList();

        Assert.Contains("components.css", names);
        Assert.Contains("TabBar.razor.css", names);
    }

    private static IEnumerable<string> UiStyleSheets()
    {
        var ui = Path.Combine(RepoRoot(), "src", "Ui");
        return Directory.EnumerateFiles(ui, "*.css", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "FitnessApp.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new DirectoryNotFoundException("FitnessApp.sln not found above the test assembly.");
    }

    [GeneratedRegex(@"(?<![-\w])(?<prop>font-size|line-height|font)\s*:(?<value>[^;}]*)")]
    private static partial Regex TextSizeDeclaration();

    [GeneratedRegex(@"\d\s*px\b")]
    private static partial Regex Px();
}
