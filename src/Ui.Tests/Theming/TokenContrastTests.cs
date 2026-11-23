namespace Ui.Tests.Theming;

// Recomputes the design document §4.2 contrast table from tokens.css for all four Theme × Mode sets.
public class TokenContrastTests
{
    private const double Text = 4.5;
    private const double NonText = 3.0;

    // The §4.2 table, defined once. Several foregrounds means "whichever is higher" (the set input boundary row).
    private static readonly ContrastPair[] Pairs =
    [
        new(["text"], "surface", Text),
        new(["text-muted"], "surface", Text),
        new(["text-heading"], "surface", NonText),
        new(["text-label"], "surface", Text),
        new(["text-label"], "surface-sunken", Text),
        new(["input-text"], "input-bg", 7.0),
        new(["input-pending"], "input-bg", Text),
        new(["input-bg", "input-border"], "surface", NonText),
        new(["focus"], "surface", NonText),
        new(["on-primary"], "primary", Text),
        new(["on-done"], "done", Text),
        new(["done-text"], "surface", Text),
        new(["pr-text"], "surface", Text),
        new(["on-pr"], "pr", Text),
        new(["state-partial"], "surface", NonText),
        new(["state-skipped"], "surface", NonText),
        new(["state-swapped"], "surface", NonText),
        new(["state-missed"], "surface", NonText),
        new(["state-pain"], "surface", NonText),
        new(["session-strength"], "surface-sunken", NonText),
        new(["session-cardio"], "surface-sunken", NonText),
        new(["nudge-text"], "nudge-bg", Text),
        new(["nudge-label"], "nudge-bg", Text),
        new(["on-tab-active"], "tab-active", Text),
    ];

    public static TheoryData<string, string> SetsAndPairs()
    {
        var data = new TheoryData<string, string>();
        foreach (var set in TokenSheet.SetNames)
        {
            foreach (var pair in Pairs)
            {
                data.Add(set, pair.Name);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(SetsAndPairs))]
    public void Every_section_4_2_pair_meets_its_minimum(string set, string pairName)
    {
        var tokens = TokenSheet.Load().Set(set);
        var pair = Pairs.Single(p => p.Name == pairName);

        var ratio = pair.Foregrounds.Max(fg => Wcag.ContrastRatio(tokens[fg], tokens[pair.Background]));

        Assert.True(
            ratio >= pair.Min,
            $"{set}: {pair.Name} is {ratio:0.00}:1, needs {pair.Min}:1.");
    }

    [Fact]
    public void Stylesheet_resolves_all_four_sets()
    {
        var sheet = TokenSheet.Load();

        Assert.Equal(["Keep dark", "Keep light", "Clean dark", "Clean light"], TokenSheet.SetNames);
        Assert.Equal("#4A4237", sheet.Set("Keep dark")["surface"]);
        Assert.Equal("#E8DFC8", sheet.Set("Keep light")["surface"]);
        Assert.Equal("#1B1E22", sheet.Set("Clean dark")["surface"]);
        Assert.Equal("#FFFFFF", sheet.Set("Clean light")["surface"]);
    }

    private sealed record ContrastPair(string[] Foregrounds, string Background, double Min)
    {
        public string Name => $"{string.Join(" or ", Foregrounds)} on {Background}";
    }

    [Theory]
    [InlineData("#000000", "#FFFFFF", 21.0)]
    [InlineData("#777777", "#FFFFFF", 4.48)]
    [InlineData("#FFD83D", "#4A4237", 7.1)]
    public void Contrast_ratio_matches_the_WCAG_reference(string foreground, string background, double expected)
    {
        Assert.Equal(expected, Wcag.ContrastRatio(foreground, background), 1);
    }
}
