using System.Globalization;
using System.Text.Json;

namespace Core.Serialisation;

internal sealed partial class FieldReader
{
    /// <summary>A required calendar date written <c>yyyy-MM-dd</c>; <c>default</c> (after reporting) when absent or invalid.</summary>
    public DateOnly RequiredDate(string name) => RequiredDateOrNull(name) ?? default;

    /// <summary>A required calendar date; null (after reporting) when absent or invalid, so a failed read can't be mistaken for a real date.</summary>
    public DateOnly? RequiredDateOrNull(string name)
    {
        return TryTakeRequired(name, out var value) ? ParseDate(ChildPath(name), value) : null;
    }

    /// <summary>An optional calendar date written <c>yyyy-MM-dd</c>; null when absent or invalid (invalid is reported).</summary>
    public DateOnly? OptionalDate(string name) =>
        TryTakeField(name, out var value) ? ParseDate(ChildPath(name), value) : null;

    private DateOnly? ParseDate(string path, JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String
            && DateOnly.TryParseExact(value.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }

        Sink.Error(path, "invalid_value", $"{path} must be a calendar date written yyyy-MM-dd, like 2026-11-16.");
        return null;
    }

    /// <summary>
    /// The one "this must not come before that" rule. Reports <paramref name="code"/> at <paramref name="laterField"/> when both values are present and
    /// the later one is earlier. A value that failed to read is null, so it is never compared.
    /// </summary>
    private void CheckOrder<T>(string earlierField, string laterField, T? earlier, T? later, string code, string format)
        where T : struct, IComparable<T>, IFormattable
    {
        if (earlier is { } from && later is { } to && to.CompareTo(from) < 0)
        {
            var provider = CultureInfo.InvariantCulture;
            Report(laterField, code,
                $"{ChildPath(laterField)} ({to.ToString(format, provider)}) can't be before {ChildPath(earlierField)} ({from.ToString(format, provider)}).");
        }
    }

    /// <summary>Reports <paramref name="code"/> (default <c>end_before_start</c>) when both dates are present and the second is earlier.</summary>
    public void CheckDateOrder(string startField, string endField, DateOnly? start, DateOnly? end, string code = "end_before_start") =>
        CheckOrder(startField, endField, start, end, code, "yyyy-MM-dd");

    /// <summary>Reports <c>end_before_start</c> when both timestamps are present and the end is earlier.</summary>
    public void CheckTimestampOrder(string startField, string endField, DateTimeOffset? start, DateTimeOffset? end) =>
        CheckOrder(startField, endField, start, end, "end_before_start", "O");

    /// <summary>An optional absolute http or https URL.</summary>
    public string? OptionalWebUrl(string name)
    {
        var text = OptionalString(name);
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (Uri.TryCreate(text, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
        {
            return text;
        }

        var path = ChildPath(name);
        Sink.Error(path, "invalid_value", $"{path} must be a web address starting with http:// or https://.");
        return null;
    }
}
