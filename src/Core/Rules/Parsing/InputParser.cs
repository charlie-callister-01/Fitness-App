using System.Globalization;
using System.Text.RegularExpressions;
using Core.Model;
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
}

/// <summary>Either a parsed Set or a plain-language reason saying what was expected.</summary>
public sealed record ParseResult(ParsedSet? Set, string? Error)
{
    public bool IsSuccess => Set is not null;

    public static ParseResult Success(ParsedSet set) => new(set, null);

    public static ParseResult Failure(string expected) => new(null, expected);
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

    private const string LoadExpected = "Expected weight x reps, like 20x10";
    private const string BodyweightExpected = "Expected bwxreps or added weight x reps, like bwx8 or 10x8";
    private const string CarryExpected = "Expected weight x metres, like 32x40";
    private const string TimeExpected = "Expected a time like 4:32, 1:04:10 or seconds";
    private const string SecondsExpected = "Expected seconds, like 45 or 45s";
    private const string MinutesExpected = "Expected minutes, like 3 or 3min";
    private const string KmExpected = "Expected a distance in km, like 5 or 5km";
    private const string MetresExpected = "Expected a distance in metres, like 400 or 400m";
    private const string RepsExpected = "Expected a whole number of reps, like 12";
    private const string BodyKgExpected = "Expected a weight, like 82.3";
    private const string BodyCmExpected = "Expected a length in cm, like 92.5";
    private const string TextExpected = "Expected some text";

    private static readonly char[] Separators = ['x', 'X', '×', '*'];

    private readonly UnitConverter _units;

    public InputParser(UnitConverter units) => _units = units;

    public ParseResult Parse(string? text, LogKind kind) => kind switch
    {
        LogKind.Load => ParseLoad(text),
        LogKind.BodyweightLoad => ParseBodyweightLoad(text),
        LogKind.Carry => ParseCarry(text),
        LogKind.Reps => ParseRepsOnly(text),
        LogKind.Seconds => ParseDuration(text, SecondsExpected, bareUnitSeconds: 1),
        LogKind.Minutes => ParseDuration(text, MinutesExpected, bareUnitSeconds: SecondsPerMinute),
        LogKind.DistanceKm => ParseDistanceKm(text),
        LogKind.DistanceM => ParseDistanceM(text),
        LogKind.Time => ParseTime(text),
        LogKind.BodyMeasurementKg => ParseBodyKg(text),
        LogKind.BodyMeasurementCm => ParseBodyCm(text),
        LogKind.Text => ParseFreeText(text),
        LogKind.TickOnly => ParseResult.Success(ParsedSet.Empty),
        _ => ParseResult.Failure("This kind of Exercise can't be logged from typed input"),
    };

    private ParseResult ParseLoad(string? text)
    {
        if (!TrySplitPair(text, out var left, out var right)
            || !TryNumber(left, out var weight) || weight <= 0
            || !TryReps(right, out var reps))
        {
            return ParseResult.Failure(LoadExpected);
        }

        return ParseResult.Success(Empty with { WeightKg = _units.WeightFromDisplay(weight), Reps = reps });
    }

    private ParseResult ParseBodyweightLoad(string? text)
    {
        if (!TrySplitPair(text, out var left, out var right) || !TryReps(right, out var reps))
        {
            return ParseResult.Failure(BodyweightExpected);
        }

        if (left.Equals("bw", StringComparison.OrdinalIgnoreCase))
        {
            return ParseResult.Success(Empty with { Reps = reps });
        }

        return TryNumber(left, out var added) && added > 0
            ? ParseResult.Success(Empty with { WeightKg = _units.WeightFromDisplay(added), Reps = reps })
            : ParseResult.Failure(BodyweightExpected);
    }

    private ParseResult ParseCarry(string? text)
    {
        if (!TrySplitPair(text, out var left, out var right)
            || !TryNumber(left, out var weight) || weight <= 0
            || !TryNumber(right, out var metres) || metres <= 0)
        {
            return ParseResult.Failure(CarryExpected);
        }

        return ParseResult.Success(Empty with { WeightKg = _units.WeightFromDisplay(weight), DistanceM = metres });
    }

    private static ParseResult ParseRepsOnly(string? text) =>
        TryReps(StripWhitespace(text), out var reps)
            ? ParseResult.Success(Empty with { Reps = reps })
            : ParseResult.Failure(RepsExpected);

    private static ParseResult ParseDuration(string? text, string expected, double bareUnitSeconds)
    {
        if (!TryNumberWithSuffix(StripWhitespace(text), out var value, out var suffix))
        {
            return ParseResult.Failure(expected);
        }

        double? seconds = suffix switch
        {
            "" => value * bareUnitSeconds,
            "s" or "sec" => value,
            "min" => value * SecondsPerMinute,
            _ => null,
        };

        return seconds is { } s && ToSeconds(s) is { } whole
            ? ParseResult.Success(Empty with { DurationSeconds = whole })
            : ParseResult.Failure(expected);
    }

    private static ParseResult ParseTime(string? text)
    {
        var compact = StripWhitespace(text);
        var seconds = compact.Contains(':') ? ClockSeconds(compact) : PlainSeconds(compact);
        return seconds is { } whole
            ? ParseResult.Success(Empty with { DurationSeconds = whole })
            : ParseResult.Failure(TimeExpected);
    }

    private ParseResult ParseDistanceKm(string? text)
    {
        if (!TryNumberWithSuffix(StripWhitespace(text), out var value, out var suffix))
        {
            return ParseResult.Failure(KmExpected);
        }

        double? km = suffix switch
        {
            "" => _units.DistanceFromDisplay(value),
            "km" => value,
            "m" => value / MetresPerKm,
            _ => null,
        };

        return km is > 0 and var distance
            ? ParseResult.Success(Empty with { DistanceKm = distance })
            : ParseResult.Failure(KmExpected);
    }

    private static ParseResult ParseDistanceM(string? text)
    {
        if (!TryNumberWithSuffix(StripWhitespace(text), out var value, out var suffix))
        {
            return ParseResult.Failure(MetresExpected);
        }

        double? metres = suffix switch
        {
            "" or "m" => value,
            "km" => value * MetresPerKm,
            _ => null,
        };

        return metres is > 0 and var distance
            ? ParseResult.Success(Empty with { DistanceM = distance })
            : ParseResult.Failure(MetresExpected);
    }

    private ParseResult ParseBodyKg(string? text) =>
        TryNumber(StripWhitespace(text), out var value) && value > 0
            ? ParseResult.Success(Empty with { WeightKg = _units.WeightFromDisplay(value) })
            : ParseResult.Failure(BodyKgExpected);

    private static ParseResult ParseBodyCm(string? text) =>
        TryNumber(StripWhitespace(text), out var value) && value > 0
            ? ParseResult.Success(Empty with { MeasurementCm = value })
            : ParseResult.Failure(BodyCmExpected);

    private static ParseResult ParseFreeText(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? ParseResult.Failure(TextExpected)
            : ParseResult.Success(Empty with { Text = text.Trim() });

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
