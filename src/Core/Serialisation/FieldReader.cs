using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Core.Serialisation;

/// <summary>
/// Reads the fields of one JSON object, recording field-level errors against their path.
/// Each getter returns a default value after reporting an error; callers check the sink's error count to know whether to keep the result.
/// Fields that were never asked for are reported as warnings by <see cref="WarnAboutUnknownFields"/>.
/// </summary>
internal sealed partial class FieldReader
{
    private static readonly string[] TimestampFormats =
        ["yyyy-MM-dd'T'HH:mm:ssK", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK"];

    private readonly JsonElement _object;
    private readonly IssueSink _sink;
    private readonly HashSet<string> _asked = [];

    private FieldReader(JsonElement obj, string path, IssueSink sink)
    {
        _object = obj;
        Path = path;
        _sink = sink;
    }

    public string Path { get; }

    /// <summary>Opens a reader on an object, or reports a wrong-type error and returns null.</summary>
    public static FieldReader? Open(JsonElement element, string path, IssueSink sink)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            return new FieldReader(element, path, sink);
        }

        sink.Error(path, "wrong_type", $"{path} must be an object.");
        return null;
    }

    [GeneratedRegex("^[A-Za-z0-9_-]+$")]
    private static partial Regex IdPattern();

    public string ChildPath(string name) => Path == "$" ? name : $"{Path}.{name}";

    public string RequiredString(string name)
    {
        var path = ChildPath(name);
        if (!TryGet(name, out var value) || (value.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(value.GetString())))
        {
            _sink.Error(path, "missing_field", $"{path} is required and can't be blank.");
            return "";
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            _sink.Error(path, "wrong_type", $"{path} must be text.");
            return "";
        }

        return value.GetString()!;
    }

    public string RequiredId(string name)
    {
        var id = RequiredString(name);
        if (id.Length > 0 && !IdPattern().IsMatch(id))
        {
            var path = ChildPath(name);
            _sink.Error(path, "invalid_id", $"{path} \"{id}\" is not a valid ID. Use only letters, digits, hyphens and underscores.");
            return "";
        }

        return id;
    }

    public string? OptionalString(string name)
    {
        if (!TryGet(name, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }

        _sink.Error(ChildPath(name), "wrong_type", $"{ChildPath(name)} must be text.");
        return null;
    }

    public bool OptionalBool(string name, bool defaultValue)
    {
        if (!TryGet(name, out var value))
        {
            return defaultValue;
        }

        if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return value.GetBoolean();
        }

        _sink.Error(ChildPath(name), "wrong_type", $"{ChildPath(name)} must be true or false.");
        return defaultValue;
    }

    public bool RequiredBool(string name)
    {
        if (!TryGet(name, out var value))
        {
            _sink.Error(ChildPath(name), "missing_field", $"{ChildPath(name)} is required.");
            return false;
        }

        if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return value.GetBoolean();
        }

        _sink.Error(ChildPath(name), "wrong_type", $"{ChildPath(name)} must be true or false.");
        return false;
    }

    public int RequiredInt(string name, int min)
    {
        var path = ChildPath(name);
        if (!TryGet(name, out var value))
        {
            _sink.Error(path, "missing_field", $"{path} is required.");
            return 0;
        }

        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var number))
        {
            _sink.Error(path, "wrong_type", $"{path} must be a whole number.");
            return 0;
        }

        if (number < min)
        {
            _sink.Error(path, "out_of_range", $"{path} must be {min} or more (it was {number}).");
            return 0;
        }

        return number;
    }

    /// <summary>A required number that must be greater than <paramref name="exclusiveMin"/>.</summary>
    public double RequiredPositiveDouble(string name)
    {
        var path = ChildPath(name);
        if (!TryGet(name, out var value))
        {
            _sink.Error(path, "missing_field", $"{path} is required.");
            return 0;
        }

        return PositiveDouble(path, value);
    }

    /// <summary>An optional number that, when present, must be greater than zero.</summary>
    public double? OptionalPositiveDouble(string name)
    {
        if (!TryGet(name, out var value))
        {
            return null;
        }

        var number = PositiveDouble(ChildPath(name), value);
        return number > 0 ? number : null;
    }

    public T RequiredEnum<T>(string name) where T : struct, Enum
    {
        var path = ChildPath(name);
        if (!TryGet(name, out var value))
        {
            _sink.Error(path, "missing_field", $"{path} is required.");
            return default;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            _sink.Error(path, "wrong_type", $"{path} must be one of: {AllowedValues<T>()}.");
            return default;
        }

        var text = value.GetString();
        foreach (var candidate in Enum.GetValues<T>())
        {
            if (JsonNamingPolicy.CamelCase.ConvertName(candidate.ToString()) == text)
            {
                return candidate;
            }
        }

        _sink.Error(path, "unknown_value", $"{path} \"{text}\" is not recognised. Use one of: {AllowedValues<T>()}.");
        return default;
    }

    public DateOnly RequiredDate(string name)
    {
        var path = ChildPath(name);
        if (!TryGet(name, out var value))
        {
            _sink.Error(path, "missing_field", $"{path} is required.");
            return default;
        }

        return ParseDate(path, value) ?? default;
    }

    public DateOnly? OptionalDate(string name) =>
        TryGet(name, out var value) ? ParseDate(ChildPath(name), value) : null;

    /// <summary>An optional whole number; null when absent or invalid (invalid is reported).</summary>
    public int? OptionalInt(string name, int min)
    {
        if (!TryGet(name, out _))
        {
            return null;
        }

        var errorsBefore = _sink.Errors.Count;
        var number = RequiredInt(name, min);
        return _sink.Errors.Count == errorsBefore ? number : null;
    }

    public DateTimeOffset? OptionalTimestamp(string name)
    {
        if (!TryGet(name, out var value))
        {
            return null;
        }

        var path = ChildPath(name);
        if (value.ValueKind == JsonValueKind.String
            && DateTimeOffset.TryParseExact(value.GetString(), TimestampFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed;
        }

        _sink.Error(path, "invalid_value", $"{path} must be an ISO 8601 timestamp with a time zone offset, like 2026-11-16T09:30:00+00:00.");
        return null;
    }

    /// <summary>An optional array of non-blank strings.</summary>
    public IReadOnlyList<string> OptionalStringList(string name)
    {
        if (!TryGet(name, out var value))
        {
            return [];
        }

        var path = ChildPath(name);
        if (value.ValueKind != JsonValueKind.Array)
        {
            _sink.Error(path, "wrong_type", $"{path} must be a list of text values.");
            return [];
        }

        var items = new List<string>();
        var index = 0;
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
            {
                items.Add(item.GetString()!);
            }
            else
            {
                _sink.Error($"{path}[{index}]", "wrong_type", $"{path}[{index}] must be non-blank text.");
            }

            index++;
        }

        return items;
    }

    /// <summary>Opens an optional nested object; null when absent or invalid (invalid is reported).</summary>
    public FieldReader? OptionalObject(string name) =>
        TryGet(name, out var value) ? Open(value, ChildPath(name), _sink) : null;

    public FieldReader? RequiredObject(string name)
    {
        if (TryGet(name, out var value))
        {
            return Open(value, ChildPath(name), _sink);
        }

        _sink.Error(ChildPath(name), "missing_field", $"{ChildPath(name)} is required.");
        return null;
    }

    /// <summary>The elements of an optional array of objects, or null when the array is absent (or not an array, which is reported).</summary>
    public IReadOnlyList<(FieldReader Reader, int Index)>? OptionalObjectArray(string name)
    {
        if (!TryGet(name, out var value))
        {
            return null;
        }

        var path = ChildPath(name);
        if (value.ValueKind != JsonValueKind.Array)
        {
            _sink.Error(path, "wrong_type", $"{path} must be a list.");
            return null;
        }

        var items = new List<(FieldReader, int)>();
        var index = 0;
        foreach (var item in value.EnumerateArray())
        {
            var reader = Open(item, $"{path}[{index}]", _sink);
            if (reader is not null)
            {
                items.Add((reader, index));
            }

            index++;
        }

        return items;
    }

    public void WarnAboutUnknownFields()
    {
        foreach (var property in _object.EnumerateObject())
        {
            if (!_asked.Contains(property.Name))
            {
                var path = ChildPath(property.Name);
                _sink.Warnings.Add(new FormatIssue(path, "unknown_field", $"{path} is not a known field and was ignored."));
            }
        }
    }

    private DateOnly? ParseDate(string path, JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String
            && DateOnly.TryParseExact(value.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed;
        }

        _sink.Error(path, "invalid_value", $"{path} must be a calendar date written yyyy-MM-dd, like 2026-10-19.");
        return null;
    }

    private double PositiveDouble(string path, JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out var number))
        {
            _sink.Error(path, "wrong_type", $"{path} must be a number.");
            return 0;
        }

        if (number <= 0)
        {
            _sink.Error(path, "out_of_range", $"{path} must be greater than 0 (it was {number.ToString(CultureInfo.InvariantCulture)}).");
            return 0;
        }

        return number;
    }

    private static string AllowedValues<T>() where T : struct, Enum =>
        string.Join(", ", Enum.GetValues<T>().Select(v => JsonNamingPolicy.CamelCase.ConvertName(v.ToString())));

    /// <summary>Finds a field; JSON null counts as absent.</summary>
    private bool TryGet(string name, out JsonElement value)
    {
        _asked.Add(name);
        if (_object.TryGetProperty(name, out value) && value.ValueKind != JsonValueKind.Null)
        {
            return true;
        }

        return false;
    }
}
