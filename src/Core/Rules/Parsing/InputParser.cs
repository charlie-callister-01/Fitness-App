using System.Globalization;
using System.Text.RegularExpressions;
using Core.Model;
using Core.Rules.LogKinds;
using Core.Rules.Units;

namespace Core.Rules.Parsing;

/// <summary>
/// A parsed Set value in canonical units (kg, km, m, seconds, cm). Only the fields the log kind uses are filled.
/// A null <see cref="WeightKg"/> with reps for a bodyweight-load kind means plain bodyweight.
/// </summary>
public sealed record ParsedSet(
    double? WeightKg,
    int? Reps,
    double? DistanceKm,
    int? DurationSeconds,
    double? DistanceM,
    double? MeasurementCm,
    string? Text)
{
    /// <summary>The value of a tick-only entry: done, with nothing measured.</summary>
    public static ParsedSet Empty { get; } = new(null, null, null, null, null, null, null);

    /// <summary>The Set to store for this value. Free <see cref="Text"/> is not part of a Set and is not carried over.</summary>
    public WorkoutSet ToWorkoutSet(string id, bool isMax) =>
        new(id, WeightKg, Reps, DistanceKm, DurationSeconds, DistanceM, isMax, MeasurementCm);
}

/// <summary>Either a parsed Set or a plain-language reason saying what was expected. Never both, never neither.</summary>
public abstract record ParseResult
{
    private ParseResult()
    {
    }

    /// <summary>The input fitted the kind.</summary>
    public sealed record Success(ParsedSet Set) : ParseResult;

    /// <summary>The input did not fit; <see cref="Error"/> says what was expected.</summary>
    public sealed record Failure(string Error) : ParseResult;
}

/// <summary>
/// Turns what the owner typed into canonical units for a log kind. Pure and culture-invariant; never throws.
/// Whitespace is ignored (except inside free text), '.' and ',' are both decimal marks, and values typed in
/// display units are converted to canonical before returning.
/// </summary>
public sealed partial class InputParser
{
    private const double SecondsPerMinute = 60;
    private const double MetresPerKm = 1000;

    private static readonly char[] Separators = ['x', 'X', '×', '*'];

    private readonly UnitConverter _units;
    private readonly Dictionary<LogKind, Scalar> _scalars;

    public InputParser(UnitConverter units)
    {
        _units = units;
        _scalars = new()
        {
            [LogKind.Seconds] = Duration(bareUnitSeconds: 1),
            [LogKind.Minutes] = Duration(bareUnitSeconds: SecondsPerMinute),
            [LogKind.DistanceKm] = new(
                new() { [""] = units.DistanceFromDisplay, ["km"] = v => v, ["m"] = v => v / MetresPerKm },
                v => Positive(v, d => Empty with { DistanceKm = d })),
            [LogKind.DistanceM] = new(
                new() { [""] = v => v, ["m"] = v => v, ["km"] = v => v * MetresPerKm },
                v => Positive(v, d => Empty with { DistanceM = d })),
            [LogKind.BodyMeasurementKg] = new(
                new() { [""] = units.WeightFromDisplay },
                v => Positive(v, kg => Empty with { WeightKg = kg })),
            [LogKind.BodyMeasurementCm] = new(
                new() { [""] = v => v },
                v => Positive(v, cm => Empty with { MeasurementCm = cm })),
        };
    }

    /// <summary>
    /// A kind typed as one number and an optional unit suffix: <see cref="Suffixes"/> turns the number into the canonical
    /// value (the empty suffix is the bare number), and <see cref="Build"/> makes the Set, or null when the value is not valid.
    /// </summary>
    private sealed record Scalar(Dictionary<string, Func<double, double>> Suffixes, Func<double, ParsedSet?> Build);

    public ParseResult Parse(string? text, LogKind kind) => kind switch
    {
        LogKind.Load => ParseLoad(text),
        LogKind.BodyweightLoad => ParseBodyweightLoad(text),
        LogKind.Carry => ParseCarry(text),
        LogKind.Reps => ParseRepsOnly(text),
        LogKind.Time => ParseTime(text),
        LogKind.Text => ParseFreeText(text),
        LogKind.TickOnly => new ParseResult.Success(ParsedSet.Empty),
        _ when _scalars.TryGetValue(kind, out var scalar) => ParseScalar(text, kind, scalar),
        _ => Fail(kind),
    };

    private static Scalar Duration(double bareUnitSeconds) => new(
        new() { [""] = v => v * bareUnitSeconds, ["s"] = v => v, ["sec"] = v => v, ["min"] = v => v * SecondsPerMinute },
        v => ToSeconds(v) is { } whole ? Empty with { DurationSeconds = whole } : null);

    private static ParsedSet? Positive(double value, Func<double, ParsedSet> build) => value > 0 ? build(value) : null;

    private static ParseResult Fail(LogKind kind) => new ParseResult.Failure(LogKindRules.For(kind).Expected);

    private ParseResult ParseScalar(string? text, LogKind kind, Scalar scalar) =>
        TryNumberWithSuffix(StripWhitespace(text), out var value, out var suffix)
            && scalar.Suffixes.TryGetValue(suffix, out var toCanonical)
            && scalar.Build(toCanonical(value)) is { } set
                ? new ParseResult.Success(set)
                : Fail(kind);

    private ParseResult ParseLoad(string? text)
    {
        if (!TrySplitPair(text, out var left, out var right)
            || !TryNumber(left, out var weight) || weight <= 0
            || !TryReps(right, out var reps))
        {
            return Fail(LogKind.Load);
        }

        return new ParseResult.Success(Empty with { WeightKg = _units.WeightFromDisplay(weight), Reps = reps });
    }

    private ParseResult ParseBodyweightLoad(string? text)
    {
        if (!TrySplitPair(text, out var left, out var right) || !TryReps(right, out var reps))
        {
            return Fail(LogKind.BodyweightLoad);
        }

        if (left.Equals("bw", StringComparison.OrdinalIgnoreCase))
        {
            return new ParseResult.Success(Empty with { Reps = reps });
        }

        return TryNumber(left, out var added) && added > 0
            ? new ParseResult.Success(Empty with { WeightKg = _units.WeightFromDisplay(added), Reps = reps })
            : Fail(LogKind.BodyweightLoad);
    }

    private ParseResult ParseCarry(string? text)
    {
        if (!TrySplitPair(text, out var left, out var right)
            || !TryNumber(left, out var weight) || weight <= 0
            || !TryNumber(right, out var metres) || metres <= 0)
        {
            return Fail(LogKind.Carry);
        }

        return new ParseResult.Success(Empty with { WeightKg = _units.WeightFromDisplay(weight), DistanceM = metres });
    }

    private static ParseResult ParseRepsOnly(string? text) =>
        TryReps(StripWhitespace(text), out var reps)
            ? new ParseResult.Success(Empty with { Reps = reps })
            : Fail(LogKind.Reps);

    private static ParseResult ParseTime(string? text)
    {
        var compact = StripWhitespace(text);
        var seconds = compact.Contains(':') ? ClockSeconds(compact) : PlainSeconds(compact);
        return seconds is { } whole
            ? new ParseResult.Success(Empty with { DurationSeconds = whole })
            : Fail(LogKind.Time);
    }

    private static ParseResult ParseFreeText(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? Fail(LogKind.Text)
            : new ParseResult.Success(Empty with { Text = text.Trim() });

    private static ParsedSet Empty => ParsedSet.Empty;

    private static string StripWhitespace(string? text) =>
        text is null ? "" : string.Concat(text.Where(c => !char.IsWhiteSpace(c)));

    /// <summary>Splits "20x10" on its single separator. Both halves must be non-empty.</summary>
    private static bool TrySplitPair(string? text, out string left, out string right)
    {
        var parts = StripWhitespace(text).Split(Separators);
        (left, right) = parts.Length == 2 ? (parts[0], parts[1]) : ("", "");
        return left.Length > 0 && right.Length > 0;
    }

    private static bool TryNumber(string text, out double value)
    {
        value = 0;
        return DecimalNumber().IsMatch(text)
            && double.TryParse(text.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value)
            && double.IsFinite(value);
    }

    private static bool TryNumberWithSuffix(string text, out double value, out string suffix)
    {
        var match = NumberThenLetters().Match(text);
        value = 0;
        suffix = "";
        if (!match.Success || !TryNumber(match.Groups["number"].Value, out value))
        {
            return false;
        }

        suffix = match.Groups["suffix"].Value.ToLowerInvariant();
        return true;
    }

    private static bool TryReps(string text, out int reps) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out reps) && reps > 0;

    private static int? ToSeconds(double seconds)
    {
        var rounded = Math.Round(seconds, MidpointRounding.AwayFromZero);
        return rounded is >= 1 and <= int.MaxValue ? (int)rounded : null;
    }

    private static int? PlainSeconds(string text) =>
        TryNumber(text, out var seconds) ? ToSeconds(seconds) : null;

    /// <summary>"m:ss" or "h:mm:ss". Minutes and seconds after the first part are exactly two digits, 00 to 59.</summary>
    private static int? ClockSeconds(string text)
    {
        var match = ClockTime().Match(text);
        if (!match.Success)
        {
            return null;
        }

        var hours = match.Groups["h"].Success ? int.Parse(match.Groups["h"].Value, CultureInfo.InvariantCulture) : 0;
        var minutes = int.Parse(match.Groups["m"].Value, CultureInfo.InvariantCulture);
        var seconds = int.Parse(match.Groups["s"].Value, CultureInfo.InvariantCulture);
        if (seconds > 59 || (match.Groups["h"].Success && minutes > 59))
        {
            return null;
        }

        var total = (long)hours * 3600 + minutes * 60 + seconds;
        return total is >= 1 and <= int.MaxValue ? (int)total : null;
    }

    [GeneratedRegex("^([0-9]+([.,][0-9]+)?|[.,][0-9]+)$")]
    private static partial Regex DecimalNumber();

    [GeneratedRegex("^(?<number>[0-9.,]+)(?<suffix>[A-Za-z]*)$")]
    private static partial Regex NumberThenLetters();

    [GeneratedRegex(@"^(?:(?<h>[0-9]{1,3}):)?(?<m>[0-9]{1,3}):(?<s>[0-9]{2})$")]
    private static partial Regex ClockTime();
}
