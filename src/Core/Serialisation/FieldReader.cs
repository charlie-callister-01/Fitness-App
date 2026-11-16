using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Core.Serialisation;

/// <summary>The result of a group of reads: what they produced, and whether they all came out without errors.</summary>
internal readonly record struct ReadAttempt<T>(T Value, bool IsValid);

/// <summary>
/// Reads the fields of one JSON object, recording field-level errors against their path.
/// Each getter returns a default value after reporting an error. To know whether a record read cleanly, wrap it in <see cref="ReadRecord{T}"/>
/// (or <see cref="Attempt{T}"/> for a group of fields inside a record) instead of counting errors by hand.
/// Fields that were never asked for are reported as warnings by <see cref="WarnAboutUnknownFields"/>.
/// </summary>
internal sealed partial class FieldReader
{
    private static readonly string[] TimestampFormats =
        ["yyyy-MM-dd'T'HH:mm:ssK", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK"];

    private readonly JsonElement _object;
    private readonly HashSet<string> _asked = [];

    private FieldReader(JsonElement obj, string path, ReadContext context)
    {
        _object = obj;
        Path = path;
        Context = context;
    }

    public string Path { get; }

    /// <summary>The state shared by every reader of this document: issue sink, reference scopes and unique-ID tracking.</summary>
    public ReadContext Context { get; }

    private IssueSink Sink => Context.Sink;

    /// <summary>Opens a reader on an object, or reports a wrong-type error and returns null.</summary>
    public static FieldReader? Open(JsonElement element, string path, ReadContext context)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            return new FieldReader(element, path, context);
        }

        context.Sink.Error(path, "wrong_type", $"{path} must be an object.");
        return null;
    }

    [GeneratedRegex("^[A-Za-z0-9_-]+$")]
    private static partial Regex IdPattern();

    public string ChildPath(string name) => Path == "$" ? name : $"{Path}.{name}";

    /// <summary>Reports a problem at a field of this object.</summary>
    public void Report(string field, string code, string message) => Sink.Error(ChildPath(field), code, message);

    /// <summary>Marks fields as known without reading them (for fields that are documentation only).</summary>
    public void Ignore(params string[] names) => _asked.UnionWith(names);

    /// <summary>True when the field is present and not null. Counts as asking for it.</summary>
    public bool Has(string name) => TryAskFor(name, out _);

    /// <summary>
    /// Runs a record-reading function in this object's scope: warns about unknown fields afterwards, and returns the record,
    /// or null when the function returned null or reported any new error.
    /// </summary>
    public T? ReadRecord<T>(Func<FieldReader, T?> read) where T : class
    {
        var attempt = Attempt(() => read(this));
        WarnAboutUnknownFields();
        return attempt.IsValid ? attempt.Value : null;
    }

    /// <summary>Runs a group of reads and says whether they all came out clean. The value is returned even when they didn't. Doesn't warn about unknown fields.</summary>
    public ReadAttempt<T> Attempt<T>(Func<T> read)
    {
        var errorsBefore = Sink.Errors.Count;
        var value = read();
        return new ReadAttempt<T>(value, Sink.Errors.Count == errorsBefore);
    }

    /// <summary>Reports <c>duplicate_id</c> when <paramref name="id"/> was already used by another record of <paramref name="kind"/> in this document.</summary>
    public void CheckUnique(string kind, string field, string id)
    {
        if (id.Length > 0 && !Context.SeenIds(kind).Add(id))
        {
            var path = ChildPath(field);
            var what = field == "id" ? "ID" : field;
            Sink.Error(path, "duplicate_id", $"{path} \"{id}\" is used by more than one {kind}. Every {kind} needs its own {what}.");
        }
    }

    public string RequiredString(string name)
    {
        var path = ChildPath(name);
        if (!TryAskFor(name, out var value) || (value.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(value.GetString())))
        {
            Sink.Error(path, "missing_field", $"{path} is required and can't be blank.");
            return "";
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            Sink.Error(path, "wrong_type", $"{path} must be text.");
            return "";
        }

        return value.GetString()!;
    }

    public string RequiredId(string name)
    {
        var id = RequiredString(name);
        return id.Length > 0 ? ValidId(name, id) ?? "" : "";
    }

    /// <summary>An optional ID; null when absent or invalid (invalid is reported).</summary>
    public string? OptionalId(string name)
    {
        var id = OptionalString(name);
        return id is null ? null : ValidId(name, id);
    }

    /// <summary>A required ID that must name something in <paramref name="scope"/>; an unknown one is reported.</summary>
    public string RequiredReference(string name, ReferenceScope scope)
    {
        var id = RequiredId(name);
        if (id.Length > 0)
        {
            scope.Check(Sink, ChildPath(name), id);
        }

        return id;
    }

    /// <summary>An optional ID that, when present, must name something in <paramref name="scope"/>.</summary>
    public string? OptionalReference(string name, ReferenceScope scope)
    {
        var id = OptionalId(name);
        if (id is not null)
        {
            scope.Check(Sink, ChildPath(name), id);
        }

        return id;
    }

    public string RequiredExercise(string name) => RequiredReference(name, Context.Exercises);

    public string? OptionalExercise(string name) => OptionalReference(name, Context.Exercises);

    public string? OptionalSession(string name) => OptionalReference(name, Context.Sessions);

    /// <summary>The one ID rule: returns <paramref name="id"/> when it is valid, otherwise reports <c>invalid_id</c> and returns null.</summary>
    private string? ValidId(string name, string id)
    {
        if (IdPattern().IsMatch(id))
        {
            return id;
        }

        var path = ChildPath(name);
        Sink.Error(path, "invalid_id", $"{path} \"{id}\" is not a valid ID. Use only letters, digits, hyphens and underscores.");
        return null;
    }

    public string? OptionalString(string name)
    {
        if (!TryAskFor(name, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }

        Sink.Error(ChildPath(name), "wrong_type", $"{ChildPath(name)} must be text.");
        return null;
    }

    public bool OptionalBool(string name, bool defaultValue)
    {
        if (!TryAskFor(name, out var value))
        {
            return defaultValue;
        }

        if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return value.GetBoolean();
        }

        Sink.Error(ChildPath(name), "wrong_type", $"{ChildPath(name)} must be true or false.");
        return defaultValue;
    }

    public bool RequiredBool(string name)
    {
        if (!TryAskFor(name, out var value))
        {
            Sink.Error(ChildPath(name), "missing_field", $"{ChildPath(name)} is required.");
            return false;
        }

        if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return value.GetBoolean();
        }

        Sink.Error(ChildPath(name), "wrong_type", $"{ChildPath(name)} must be true or false.");
        return false;
    }

    public int RequiredInt(string name, int min)
    {
        var path = ChildPath(name);
        if (!TryAskFor(name, out var value))
        {
            Sink.Error(path, "missing_field", $"{path} is required.");
            return 0;
        }

        return ParseInt(path, value, min) ?? 0;
    }

    /// <summary>An optional whole number; null when absent or invalid (invalid is reported).</summary>
    public int? OptionalInt(string name, int min) =>
        TryAskFor(name, out var value) ? ParseInt(ChildPath(name), value, min) : null;

    private int? ParseInt(string path, JsonElement value, int min)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var number))
        {
            Sink.Error(path, "wrong_type", $"{path} must be a whole number.");
            return null;
        }

        if (number < min)
        {
            Sink.Error(path, "out_of_range", $"{path} must be {min} or more (it was {number}).");
            return null;
        }

        return number;
    }

    /// <summary>A required number that must be greater than zero.</summary>
    public double RequiredPositiveDouble(string name)
    {
        var path = ChildPath(name);
        if (!TryAskFor(name, out var value))
        {
            Sink.Error(path, "missing_field", $"{path} is required.");
            return 0;
        }

        return PositiveDouble(path, value);
    }

    /// <summary>An optional number that, when present, must be greater than zero.</summary>
    public double? OptionalPositiveDouble(string name)
    {
        if (!TryAskFor(name, out var value))
        {
            return null;
        }

        var number = PositiveDouble(ChildPath(name), value);
        return number > 0 ? number : null;
    }

    public T RequiredEnum<T>(string name) where T : struct, Enum
    {
        var path = ChildPath(name);
        if (!TryAskFor(name, out var value))
        {
            Sink.Error(path, "missing_field", $"{path} is required.");
            return default;
        }

        return ParseEnum<T>(path, value) ?? default;
    }

    /// <summary>Reads an enum written as its camelCase name; reports and returns null when it isn't one.</summary>
    private T? ParseEnum<T>(string path, JsonElement value) where T : struct, Enum
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            Sink.Error(path, "wrong_type", $"{path} must be one of: {AllowedValues<T>()}.");
            return null;
        }

        var text = value.GetString();
        foreach (var candidate in Enum.GetValues<T>())
        {
            if (JsonNamingPolicy.CamelCase.ConvertName(candidate.ToString()) == text)
            {
                return candidate;
            }
        }

        Sink.Error(path, "unknown_value", $"{path} \"{text}\" is not recognised. Use one of: {AllowedValues<T>()}.");
        return null;
    }

    public DateTimeOffset? OptionalTimestamp(string name)
    {
        if (!TryAskFor(name, out var value))
        {
            return null;
        }

        var path = ChildPath(name);
        if (value.ValueKind == JsonValueKind.String
            && DateTimeOffset.TryParseExact(value.GetString(), TimestampFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed;
        }

        Sink.Error(path, "invalid_value", $"{path} must be an ISO 8601 timestamp with a time zone offset, like 2026-11-16T09:30:00+00:00.");
        return null;
    }

    /// <summary>An optional array of non-blank strings.</summary>
    public IReadOnlyList<string> OptionalStringList(string name)
    {
        if (!TryAskFor(name, out var value))
        {
            return [];
        }

        var path = ChildPath(name);
        if (value.ValueKind != JsonValueKind.Array)
        {
            Sink.Error(path, "wrong_type", $"{path} must be a list of text values.");
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
                Sink.Error($"{path}[{index}]", "wrong_type", $"{path}[{index}] must be non-blank text.");
            }

            index++;
        }

        return items;
    }

    /// <summary>Opens an optional nested object; null when absent or invalid (invalid is reported).</summary>
    public FieldReader? OptionalObject(string name) =>
        TryAskFor(name, out var value) ? Open(value, ChildPath(name), Context) : null;

    public FieldReader? RequiredObject(string name)
    {
        if (TryAskFor(name, out var value))
        {
            return Open(value, ChildPath(name), Context);
        }

        Sink.Error(ChildPath(name), "missing_field", $"{ChildPath(name)} is required.");
        return null;
    }

    /// <summary>The elements of an optional array of objects, or null when the array is absent (or not an array, which is reported).</summary>
    public IReadOnlyList<(FieldReader Reader, int Index)>? OptionalObjectArray(string name)
    {
        if (!TryAskFor(name, out var value))
        {
            return null;
        }

        var path = ChildPath(name);
        if (value.ValueKind != JsonValueKind.Array)
        {
            Sink.Error(path, "wrong_type", $"{path} must be a list.");
            return null;
        }

        var items = new List<(FieldReader, int)>();
        var index = 0;
        foreach (var item in value.EnumerateArray())
        {
            var reader = Open(item, $"{path}[{index}]", Context);
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
                Sink.Warnings.Add(new FormatIssue(path, "unknown_field", $"{path} is not a known field and was ignored."));
            }
        }
    }

    private double PositiveDouble(string path, JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out var number))
        {
            Sink.Error(path, "wrong_type", $"{path} must be a number.");
            return 0;
        }

        if (number <= 0)
        {
            Sink.Error(path, "out_of_range", $"{path} must be greater than 0 (it was {number.ToString(CultureInfo.InvariantCulture)}).");
            return 0;
        }

        return number;
    }

    private static string AllowedValues<T>() where T : struct, Enum =>
        string.Join(", ", Enum.GetValues<T>().Select(v => JsonNamingPolicy.CamelCase.ConvertName(v.ToString())));

    /// <summary>Looks up a field and records that it was asked for (so it isn't reported as unknown). JSON null counts as absent.</summary>
    private bool TryAskFor(string name, out JsonElement value)
    {
        _asked.Add(name);
        return _object.TryGetProperty(name, out value) && value.ValueKind != JsonValueKind.Null;
    }
}
