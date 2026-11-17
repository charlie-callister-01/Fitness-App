using System.Text.Json;
using System.Text.RegularExpressions;
using Core.Model;
using Core.Serialisation;

namespace Core.Storage;

/// <summary>
/// The one place that maps record kinds to the exchange format: which top-level property holds each kind and which JSON field is its key.
/// Stored records are the exchange format's own JSON, so the same reader that reads a Backup reads storage.
/// </summary>
public static partial class StoredRecords
{
    /// <summary>One record kind: its top-level property in the document, and the field holding its key (null for the Settings singleton).</summary>
    private sealed record Shape(RecordKind Kind, string Property, string? KeyField);

    private static readonly Shape[] Shapes =
    [
        new(RecordKind.Exercise, "exercises", "id"),
        new(RecordKind.Settings, "settings", null),
        new(RecordKind.Plan, "plans", "id"),
        new(RecordKind.WeighIn, "weighIns", "id"),
        new(RecordKind.Break, "breaks", "id"),
        new(RecordKind.Badge, "badges", "id"),
        new(RecordKind.Guide, "guides", "exerciseId"),
        new(RecordKind.Workout, "workouts", "id"),
        new(RecordKind.WeekSchedule, "weekSchedules", "weekStart"),
    ];

    /// <summary>Splits a document into one stored record per top-level record, each keyed by its key rule. Collections that are absent store nothing.</summary>
    public static IReadOnlyList<StoredRecord> FromDocument(ExchangeDocument document)
    {
        using var parsed = JsonDocument.Parse(ExchangeFormat.Write(document));
        var records = new List<StoredRecord>();
        foreach (var shape in Shapes)
        {
            if (!parsed.RootElement.TryGetProperty(shape.Property, out var value))
            {
                continue;
            }

            if (shape.KeyField is null)
            {
                records.Add(new StoredRecord(shape.Kind, RecordKeys.SettingsKey, value.GetRawText()));
                continue;
            }

            records.AddRange(value.EnumerateArray().Select(item =>
                new StoredRecord(shape.Kind, item.GetProperty(shape.KeyField).GetString()!, item.GetRawText())));
        }

        return records;
    }

    /// <summary>Joins stored records back into one exchange document, remembering which record each position came from.</summary>
    internal static AssembledDocument Assemble(IEnumerable<StoredRecord> records, int formatVersion)
    {
        var byKind = records.ToLookup(record => record.Kind);
        var malformed = new List<StoredRecord>();
        var placed = new Dictionary<string, List<StoredRecord>>();

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("formatVersion", formatVersion);
            foreach (var shape in Shapes)
            {
                var readable = byKind[shape.Kind].Where(record => IsJson(record.Json)).ToList();
                malformed.AddRange(byKind[shape.Kind].Except(readable));
                // A collection is always written, even empty: the store is the whole dataset, so references into it are checked.
                if (readable.Count == 0 && shape.KeyField is null)
                {
                    continue;
                }

                placed[shape.Property] = readable;
                WriteProperty(writer, shape, readable);
            }

            writer.WriteEndObject();
        }

        return new AssembledDocument(System.Text.Encoding.UTF8.GetString(stream.ToArray()), placed, malformed);
    }

    private static void WriteProperty(Utf8JsonWriter writer, Shape shape, List<StoredRecord> records)
    {
        writer.WritePropertyName(shape.Property);
        if (shape.KeyField is null)
        {
            writer.WriteRawValue(records[0].Json);
            return;
        }

        writer.WriteStartArray();
        foreach (var record in records)
        {
            writer.WriteRawValue(record.Json);
        }

        writer.WriteEndArray();
    }

    private static bool IsJson(string json)
    {
        try
        {
            using var _ = JsonDocument.Parse(json);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>The assembled JSON text, plus a way back from a reader error path (such as <c>plans[1].phases[0].startWeek</c>) to the stored record it names.</summary>
    internal sealed class AssembledDocument(string json, Dictionary<string, List<StoredRecord>> placed, IReadOnlyList<StoredRecord> malformed)
    {
        public string Json { get; } = json;

        /// <summary>Records whose text is not JSON at all; they are left out of <see cref="Json"/>.</summary>
        public IReadOnlyList<StoredRecord> Malformed { get; } = malformed;

        /// <summary>The record an error path points inside, or null when the path names the document or a whole collection.</summary>
        public StoredRecord? RecordAt(string path)
        {
            var match = PathStart().Match(path);
            if (!match.Success || !placed.TryGetValue(match.Groups["property"].Value, out var records))
            {
                return null;
            }

            if (match.Groups["index"].Success)
            {
                return int.Parse(match.Groups["index"].Value) is var index && index < records.Count ? records[index] : null;
            }

            return match.Groups["property"].Value == "settings" ? records[0] : null;
        }
    }

    [GeneratedRegex(@"^(?<property>[A-Za-z]+)(\[(?<index>\d+)\])?")]
    private static partial Regex PathStart();
}
