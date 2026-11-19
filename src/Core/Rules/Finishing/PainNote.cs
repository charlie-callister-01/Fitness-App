namespace Core.Rules.Finishing;

/// <summary>A Pain note read back: the optional side phrase ("Left side") and the note text after it.</summary>
public sealed record ParsedPainNote(string? Side, string? Text);

/// <summary>
/// The one place that writes and reads the optional side of a Pain check note. The side leads the note ("Left side: tight knee"),
/// so the summary can read it back.
/// </summary>
public static class PainNote
{
    /// <summary>The sides offered, as written into the note.</summary>
    public static readonly IReadOnlyList<string> Sides = ["Left side", "Right side", "Both sides"];

    /// <summary>The note to store: the side first when there is one, then the trimmed text; null when there is neither.</summary>
    public static string? Compose(string? side, string? note)
    {
        var text = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        return side is null ? text : text is null ? side : $"{side}: {text}";
    }

    /// <summary>Reads a stored note back into its side (canonical phrase) and text.</summary>
    public static ParsedPainNote Parse(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return new ParsedPainNote(null, null);
        }

        var trimmed = note.Trim();
        foreach (var side in Sides)
        {
            if (!trimmed.StartsWith(side, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var rest = trimmed[side.Length..].TrimStart(':', ' ');
            return new ParsedPainNote(side, rest.Length == 0 ? null : rest);
        }

        return new ParsedPainNote(null, trimmed);
    }
}
