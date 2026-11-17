using System.Text.Json;
using System.Text.RegularExpressions;
using Core.Model;
using Core.Serialisation;

namespace Core.Storage;

/// <summary>
/// Converts between an exchange document and stored records, through the per-kind <see cref="RecordDescriptors"/>.
/// Stored records are the exchange format's own JSON, so the same reader that reads a Backup reads storage.
/// </summary>
public static partial class StoredRecords
{
    /// <summary>Splits a document into one stored record per top-level record, each keyed by its key rule. Collections that are absent store nothing.</summary>
    public static IReadOnlyList<StoredRecord> FromDocument(ExchangeDocument document)
    {
        using var parsed = JsonDocument.Parse(ExchangeFormat.Write(document));
        var root = parsed.RootElement;
        return
        [
            .. RecordDescriptors.All
                .Where(descriptor => root.TryGetProperty(descriptor.Property, out _))
                .SelectMany(descriptor => descriptor.Split(root.GetProperty(descriptor.Property))),
        ];
    }

    /// <summary>Joins stored records back into one exchange document, remembering which record each position came from.</summary>
    internal static AssembledDocument Assemble(IEnumerable<StoredRecord> records, int formatVersion)
    {
        var byKind = records.ToLookup(record => record.Kind);
        var malformed = new List<StoredRecord>();
        var placed = new Dictionary<string, Placement>();

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("formatVersion", formatVersion);
            foreach (var descriptor in RecordDescriptors.All)
            {
                var readable = byKind[descriptor.Kind].Where(record => IsJson(record.Json)).ToList();
                malformed.AddRange(byKind[descriptor.Kind].Except(readable));
                // A collection is always written, even empty: the store is the whole dataset, so references into it are checked.
                if (readable.Count == 0 && descriptor.IsSingleton)
                {
                    continue;
                }

                placed[descriptor.Property] = new Placement(descriptor, readable);
                writer.WritePropertyName(descriptor.Property);
                descriptor.WriteValue(writer, readable);
            }

            writer.WriteEndObject();
        }

        return new AssembledDocument(System.Text.Encoding.UTF8.GetString(stream.ToArray()), placed, malformed);
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

    internal sealed record Placement(RecordDescriptor Descriptor, IReadOnlyList<StoredRecord> Records);

    /// <summary>The assembled JSON text, plus a way back from a reader error path (such as <c>plans[1].phases[0].startWeek</c>) to the stored record it names.</summary>
    internal sealed class AssembledDocument(string json, Dictionary<string, Placement> placed, IReadOnlyList<StoredRecord> malformed)
    {
        public string Json { get; } = json;

        /// <summary>Records whose text is not JSON at all; they are left out of <see cref="Json"/>.</summary>
        public IReadOnlyList<StoredRecord> Malformed { get; } = malformed;

        /// <summary>The record an error path points inside, or null when the path names the document or a whole collection.</summary>
        public StoredRecord? RecordAt(string path)
        {
            var match = PathStart().Match(path);
            if (!match.Success || !placed.TryGetValue(match.Groups["property"].Value, out var placement))
            {
                return null;
            }

            int? index = int.TryParse(match.Groups["index"].Value, out var parsedIndex) ? parsedIndex : null;
            return placement.Descriptor.Pick(placement.Records, index);
        }
    }

    [GeneratedRegex(@"^(?<property>[A-Za-z]+)(\[(?<index>\d+)\])?")]
    private static partial Regex PathStart();
}
