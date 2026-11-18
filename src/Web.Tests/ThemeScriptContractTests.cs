using System.Text.Json;
using System.Text.RegularExpressions;
using Core.Model;

namespace Web.Tests;

/// <summary>
/// theme.js repeats values that live elsewhere (the Theme and Mode names, the attribute selectors tokens.css switches on, the Keep dark
/// chrome colour). It cannot reference them, so these file-based checks stop the copies drifting apart silently.
/// </summary>
public class ThemeScriptContractTests
{
    private static readonly string Root = FindRepoRoot();
    private static readonly string Script = File.ReadAllText(Path.Combine(Root, "src", "Web", "wwwroot", "js", "theme.js"));

    [Fact]
    public void TheScriptsThemesAndModesAreTheCoreEnumNames_AndTheDefaultsAreTheSettingsDefaults()
    {
        Assert.Equal(EnumNames<Theme>(), StringList("THEMES"));
        Assert.Equal(EnumNames<Mode>(), StringList("MODES"));
        Assert.Equal(Settings.Default.Theme.ToString().ToLowerInvariant(), StringConstant("DEFAULT_THEME"));
        Assert.Equal(Settings.Default.Mode.ToString().ToLowerInvariant(), StringConstant("DEFAULT_MODE"));
    }

    [Fact]
    public void TokensCssSwitchesOnTheAttributeValuesTheScriptSets()
    {
        var tokens = File.ReadAllText(Path.Combine(Root, "src", "Ui", "wwwroot", "tokens.css"));
        var resolvedModes = StringList("MODES").Where(mode => mode != "system");

        foreach (var theme in StringList("THEMES"))
        {
            Assert.Contains($"[data-theme=\"{theme}\"]", tokens);
        }

        foreach (var mode in resolvedModes)
        {
            Assert.Contains($"[data-mode=\"{mode}\"]", tokens);
        }
    }

    [Fact]
    public void TheKeepDarkChromeColourIsTheOneInSplashJsonTheManifestAndTheHostPage()
    {
        var splash = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "docs", "design", "assets", "app-icon", "splash.json"))).RootElement;
        var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "src", "Web", "wwwroot", "manifest.webmanifest"))).RootElement;
        var host = File.ReadAllText(Path.Combine(Root, "src", "Web", "wwwroot", "index.html"));
        var expected = splash.GetProperty("theme_color").GetString();

        Assert.Equal(expected, ChromeColour("keep-dark"));
        Assert.Equal(expected, manifest.GetProperty("theme_color").GetString());
        Assert.Contains($"<meta name=\"theme-color\" content=\"{expected}\"", host);
        Assert.Equal(splash.GetProperty("theme_color_clean_dark").GetString(), ChromeColour("clean-dark"));
        Assert.Equal(splash.GetProperty("theme_color_clean_light").GetString(), ChromeColour("clean-light"));
    }

    [Fact]
    public void TheHostPageShipsTheChromeColourTagBeforeTheScriptThatUpdatesIt()
    {
        var host = File.ReadAllText(Path.Combine(Root, "src", "Web", "wwwroot", "index.html"));

        var meta = host.IndexOf("name=\"theme-color\"", StringComparison.Ordinal);
        var script = host.IndexOf("js/theme.js", StringComparison.Ordinal);

        Assert.InRange(meta, 0, script);
    }

    private static string[] EnumNames<T>() where T : struct, Enum => [.. Enum.GetNames<T>().Select(name => name.ToLowerInvariant())];

    private static string[] StringList(string constant)
    {
        var match = Regex.Match(Script, $@"const {constant} = \[(?<items>[^\]]*)\];");
        Assert.True(match.Success, $"{constant} not found in theme.js");
        return [.. match.Groups["items"].Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(item => item.Trim('\''))];
    }

    private static string StringConstant(string constant)
    {
        var match = Regex.Match(Script, $@"const {constant} = '(?<value>[^']*)';");
        Assert.True(match.Success, $"{constant} not found in theme.js");
        return match.Groups["value"].Value;
    }

    private static string ChromeColour(string key)
    {
        var match = Regex.Match(Script, $@"'{key}': '(?<colour>#[0-9A-Fa-f]{{6}})'");
        Assert.True(match.Success, $"Chrome colour {key} not found in theme.js");
        return match.Groups["colour"].Value;
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
