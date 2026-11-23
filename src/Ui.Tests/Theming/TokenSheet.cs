using System.Text.RegularExpressions;

namespace Ui.Tests.Theming;

// Reads the colour tokens of each Theme × Mode set from tokens.css (copied next to the test assembly).
internal sealed partial class TokenSheet
{
    public static readonly string[] SetNames = ["Keep dark", "Keep light", "Clean dark", "Clean light"];

    private readonly Dictionary<string, IReadOnlyDictionary<string, string>> sets;

    private TokenSheet(Dictionary<string, IReadOnlyDictionary<string, string>> sets) => this.sets = sets;

    public static TokenSheet Load()
    {
        var css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "tokens.css"));
        var sets = new Dictionary<string, IReadOnlyDictionary<string, string>>();

        foreach (Match block in RuleBlock().Matches(css))
        {
            var selector = SetSelector().Match(block.Groups["selector"].Value);
            if (!selector.Success)
            {
                continue;
            }

            var name = $"{Capitalise(selector.Groups["theme"].Value)} {selector.Groups["mode"].Value}";
            sets[name] = ColourDeclaration().Matches(block.Groups["body"].Value)
                .ToDictionary(d => d.Groups["token"].Value, d => d.Groups["value"].Value.ToUpperInvariant());
        }

        return new TokenSheet(sets);
    }

    public IReadOnlyDictionary<string, string> Set(string name) =>
        sets.TryGetValue(name, out var tokens)
            ? tokens
            : throw new KeyNotFoundException($"tokens.css has no '{name}' set.");

    private static string Capitalise(string value) => char.ToUpperInvariant(value[0]) + value[1..];

    [GeneratedRegex(@"(?<selector>[^{}]+)\{(?<body>[^{}]*)\}")]
    private static partial Regex RuleBlock();

    [GeneratedRegex(@"\[data-theme=""(?<theme>\w+)""\]\[data-mode=""(?<mode>\w+)""\]")]
    private static partial Regex SetSelector();

    [GeneratedRegex(@"--(?<token>[\w-]+):\s*(?<value>#[0-9A-Fa-f]{6})\s*;")]
    private static partial Regex ColourDeclaration();
}
