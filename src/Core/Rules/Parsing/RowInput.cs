using Core.Model;
using Core.Rules.LogKinds;

namespace Core.Rules.Parsing;

/// <summary>What <see cref="RowInput.Check"/> made of a row: a Set to store, or a message and the field to blame.</summary>
public abstract record RowCheck
{
    private RowCheck()
    {
    }

    public sealed record Valid(ParsedSet Set) : RowCheck;

    /// <param name="Error">The parser's plain-language message.</param>
    /// <param name="Field">The field to mark: the first when it fails even beside a valid second field.</param>
    public sealed record Invalid(string Error, RowField Field) : RowCheck;
}

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
    /// The parser's text for a row. A pair row (weight x reps, bodyweight or added weight x reps, weight x metres) joins
    /// <paramref name="first"/> and <paramref name="second"/> with an 'x'; every other kind passes <paramref name="first"/> unchanged.
    /// </summary>
    public static string Join(LogKind kind, string? first, string? second) =>
        RowSpec.For(kind) is { Shape: RowShape.Pair } ? $"{first}x{second}" : first ?? "";

    /// <summary>Parses a row once: the Set to store, or the parser's message with the field it is about.</summary>
    public static RowCheck Check(LogKind kind, string? first, string? second, InputParser parser) =>
        parser.Parse(Join(kind, first, second), kind) switch
        {
            ParseResult.Success success => new RowCheck.Valid(success.Set),
            ParseResult.Failure failure => new RowCheck.Invalid(failure.Error, FaultyField(kind, first, parser)),
            _ => throw new InvalidOperationException("ParseResult is either a success or a failure."),
        };

    /// <summary>The first field is the culprit when the row still fails with a valid second field; otherwise the second.</summary>
    private static RowField FaultyField(LogKind kind, string? first, InputParser parser) =>
        parser.Parse(Join(kind, first, "1"), kind) is ParseResult.Failure ? RowField.First : RowField.Second;
}
