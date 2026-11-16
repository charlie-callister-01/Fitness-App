using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Core.Model;

namespace Core.Serialisation;

/// <summary>
/// The single public entry point for the exchange format: write a dataset to JSON, or read JSON text into a validated dataset.
/// Bad input never throws; it comes back as field-level errors in <see cref="ReadResult"/>.
/// </summary>
public static class ExchangeFormat
{
    /// <summary>The format version this build writes and fully understands.</summary>
    public const int CurrentVersion = 1;

    private const string VersionField = "formatVersion";

    /// <summary>Readers by format version. To support a new version, add its reader; old readers stay so old Backups still import.</summary>
    private static readonly Dictionary<int, Func<JsonElement, ReadResult>> Readers = new()
    {
        [1] = V1Reader.Read,
    };

    public static string Write(ExchangeDocument document, bool indented = false)
    {
        var body = (JsonObject)JsonSerializer.SerializeToNode(document, ExchangeJsonContext.Default.ExchangeDocument)!;
        var withReadMe = WithReadMeAfterVersion(body);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
        {
            Indented = indented,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        }))
        {
            withReadMe.WriteTo(writer);
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>Puts the "how to read this" block straight after <c>formatVersion</c>, ahead of the data.</summary>
    private static JsonObject WithReadMeAfterVersion(JsonObject body)
    {
        var result = new JsonObject();
        foreach (var (name, value) in body.ToList())
        {
            body.Remove(name);
            result.Add(name, value);
            if (name == VersionField)
            {
                result.Add(ExchangeReadMe.Field, ExchangeReadMe.Build());
            }
        }

        return result;
    }

    /// <summary>
    /// Checks every cross-record rule (unique IDs, references, the single active Plan, Phase week coverage, planned rows) on a document that
    /// is already in memory. Returns the problems found, empty when the document is valid. <see cref="Read"/> runs this after parsing.
    /// </summary>
    public static IReadOnlyList<FormatIssue> Validate(ExchangeDocument document) => DocumentValidator.Validate(document);

    public static ReadResult Read(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Fail("$", "empty_input", "The input is empty. Expected a JSON document.");
        }

        JsonDocument parsed;
        try
        {
            parsed = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            return Fail("$", "malformed_json", $"The input is not valid JSON: {ex.Message}");
        }

        using (parsed)
        {
            var root = parsed.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return Fail("$", "wrong_type", "The document must be a JSON object at the top level.");
            }

            return ReadVersioned(root);
        }
    }

    private static ReadResult ReadVersioned(JsonElement root)
    {
        if (!root.TryGetProperty(VersionField, out var versionElement) || versionElement.ValueKind == JsonValueKind.Null)
        {
            return Fail(VersionField, "missing_field", "formatVersion is required. Add \"formatVersion\": 1 at the top of the document.");
        }

        if (versionElement.ValueKind != JsonValueKind.Number || !versionElement.TryGetInt32(out var version))
        {
            return Fail(VersionField, "wrong_type", "formatVersion must be a whole number such as 1.");
        }

        if (version < 1)
        {
            return Fail(VersionField, "invalid_version", $"formatVersion must be 1 or more (it was {version}).");
        }

        if (version > CurrentVersion)
        {
            return Fail(VersionField, "unsupported_version",
                $"This document uses format version {version}, but this app only understands up to version {CurrentVersion}. Please update the app and try again.");
        }

        return Readers[version](root);
    }

    private static ReadResult Fail(string path, string code, string message) =>
        ReadResult.Failure(new FormatIssue(path, code, message));
}
