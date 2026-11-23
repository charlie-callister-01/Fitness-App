using System.Text.RegularExpressions;
using Core.Model;

namespace Ui.Tests.Theming;

// Reads the colour tokens of each Theme × Mode set from tokens.css (copied next to the test assembly).
internal sealed partial class TokenSheet
{
    public static readonly (Theme Theme, Mode Mode)[] Sets =
        [(Theme.Keep, Mode.Dark), (Theme.Keep, Mode.Light), (Theme.Clean, Mode.Dark), (Theme.Clean, Mode.Light)];

    private readonly Dictionary<(Theme, Mode), IReadOnlyDictionary<string, string>> _sets;

    private TokenSheet(Dictionary<(Theme, Mode), IReadOnlyDictionary<string, string>> sets) => _sets = sets;

    public static TokenSheet Load()
    {
        var css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "tokens.css"));
        var sets = new Dictionary<(Theme, Mode), IReadOnlyDictionary<string, string>>();

        foreach (Match block in RuleBlock().Matches(css))
        {
            var selector = SetSelector().Match(block.Groups["selector"].Value);
            if (!selector.Success)
            {
                continue;
            }

            var theme = Enum.Parse<Theme>(selector.Groups["theme"].Value, ignoreCase: true);
            var mode = Enum.Parse<Mode>(selector.Groups["mode"].Value, ignoreCase: true);
            sets[(theme, mode)] = ColourDeclaration().Matches(block.Groups["body"].Value)
                .ToDictionary(d => d.Groups["token"].Value, d => d.Groups["value"].Value.ToUpperInvariant());
        }

        return new TokenSheet(sets);
    }

    public IReadOnlyDictionary<string, string> Set(Theme theme, Mode mode) =>
        _sets.TryGetValue((theme, mode), out var tokens)
            ? tokens
            : throw new KeyNotFoundException($"tokens.css has no {theme} {mode} set.");

    [GeneratedRegex(@"(?<selector>[^{}]+)\{(?<body>[^{}]*)\}")]
    private static partial Regex RuleBlock();

    [GeneratedRegex(@"\[data-theme=""(?<theme>\w+)""\]\[data-mode=""(?<mode>\w+)""\]")]
    private static partial Regex SetSelector();

    [GeneratedRegex(@"--(?<token>[\w-]+):\s*(?<value>#[0-9A-Fa-f]{6})\s*;")]
    private static partial Regex ColourDeclaration();
}