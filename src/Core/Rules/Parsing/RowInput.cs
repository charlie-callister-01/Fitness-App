using Core.Model;

namespace Core.Rules.Parsing;

/// <summary>
/// Joins the one or two fields of a log row into the string <see cref="InputParser"/> already understands, so the
/// parser stays the only place that reads typed text. A blank field is left blank, so the parser's own plain-language
/// failure comes back rather than a new message. Pure; never throws.
/// </summary>
public static class RowInput
{
    /// <summary>The literal typed (or shown) in the first field of a bodyweight row that adds no weight.</summary>
    public const string Bodyweight = "bw";

    /// <summary>
    /// The parser's text for a row. Weight x reps, bodyweight (or added weight) x reps and weight x metres kinds join
    /// <paramref name="first"/> and <paramref name="second"/> with an 'x'; every other kind passes <paramref name="first"/> unchanged.
    /// </summary>
    public static string Join(LogKind kind, string? first, string? second) => kind switch
    {
        LogKind.Load or LogKind.BodyweightLoad or LogKind.Carry => $"{first}x{second}",
        _ => first ?? "",
    };
}
