using System.Globalization;
using System.Text.Json;

namespace Core.Serialisation;

internal sealed partial class FieldReader
{
    /// <summary>A required calendar date written <c>yyyy-MM-dd</c>.</summary>
    public DateOnly RequiredDate(string name)
    {
        var path = ChildPath(name);
        if (!TryGet(name, out var value))
        {
            _sink.Error(path, "missing_field", $"{path} is required.");
            return default;
        }

        if (value.ValueKind == JsonValueKind.String
            && DateOnly.TryParseExact(value.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }

        _sink.Error(path, "invalid_value", $"{path} must be a calendar date written yyyy-MM-dd, like 2026-11-16.");
        return default;
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
        _sink.Error(path, "invalid_value", $"{path} must be a web address starting with http:// or https://.");
        return null;
    }
}
