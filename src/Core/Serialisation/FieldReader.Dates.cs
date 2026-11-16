using System.Globalization;
using System.Text.Json;

namespace Core.Serialisation;

internal sealed partial class FieldReader
{
    /// <summary>A required calendar date written <c>yyyy-MM-dd</c>.</summary>
    public DateOnly RequiredDate(string name)
    {
        var path = ChildPath(name);
        if (!TryAskFor(name, out var value))
        {
            Sink.Error(path, "missing_field", $"{path} is required.");
            return default;
        }

        return ParseDate(path, value) ?? default;
    }

    /// <summary>An optional calendar date written <c>yyyy-MM-dd</c>; null when absent or invalid (invalid is reported).</summary>
    public DateOnly? OptionalDate(string name) =>
        TryAskFor(name, out var value) ? ParseDate(ChildPath(name), value) : null;

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

    /// <summary>Reports <c>end_before_start</c> when both dates are real (not read-failure defaults) and the end is earlier.</summary>
    public void CheckDateOrder(string startField, string endField, DateOnly start, DateOnly end)
    {
        if (start != default && end != default && end < start)
        {
            Report(endField, "end_before_start",
                $"{ChildPath(endField)} ({end:yyyy-MM-dd}) can't be before {ChildPath(startField)} ({start:yyyy-MM-dd}).");
        }
    }

    /// <summary>Reports <c>end_before_start</c> when both timestamps are present and the end is earlier.</summary>
    public void CheckTimestampOrder(string startField, string endField, DateTimeOffset? start, DateTimeOffset? end)
    {
        if (start is { } from && end is { } to && to < from)
        {
            Report(endField, "end_before_start", $"{ChildPath(endField)} ({to:O}) can't be before {ChildPath(startField)} ({from:O}).");
        }
    }

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
