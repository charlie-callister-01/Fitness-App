using Core.Model;

namespace Core.Serialisation;

/// <summary>What <see cref="BodyAndGuideReader"/> found; a null list means that collection was absent from the document.</summary>
internal sealed record BodyAndGuideData(
    IReadOnlyList<WeighIn>? WeighIns,
    IReadOnlyList<Break>? Breaks,
    IReadOnlyList<Badge>? Badges,
    IReadOnlyList<Guide>? Guides);

/// <summary>Reads the Weigh-in, Break, Badge and Guide collections of a version 1 document. Guides are checked against the context's Exercise scope.</summary>
internal static class BodyAndGuideReader
{
    public static BodyAndGuideData Read(FieldReader root) => new(
        ReadWeighIns(root),
        ReadBreaks(root),
        ReadBadges(root),
        ReadGuides(root));

    /// <summary>Reads each element of a collection, reports duplicate keys, and keeps only elements that were fully valid.</summary>
    private static List<T>? ReadRecords<T>(FieldReader root, string collection, string kind, string keyField, Func<FieldReader, T> read, Func<T, string> key)
        where T : class
    {
        var items = root.OptionalObjectArray(collection);
        if (items is null)
        {
            return null;
        }

        return items
            .Select(item => item.Reader.ReadRecord(reader =>
            {
                var record = read(reader);
                reader.CheckUnique(kind, keyField, key(record));
                return record;
            }))
            .OfType<T>()
            .ToList();
    }

    private static List<WeighIn>? ReadWeighIns(FieldReader root) =>
        ReadRecords(root, "weighIns", "Weigh-in", "id",
            r => new WeighIn(r.RequiredId("id"), r.RequiredDate("date"), r.RequiredPositiveDouble("weightKg")),
            w => w.Id);

    private static List<Break>? ReadBreaks(FieldReader root) =>
        ReadRecords(root, "breaks", "Break", "id",
            r =>
            {
                var id = r.RequiredId("id");
                var start = r.RequiredDate("startDate");
                var end = r.RequiredDate("endDate");
                var reason = r.RequiredString("reason");
                r.CheckDateOrder("startDate", "endDate", start, end);
                return new Break(id, start, end, reason);
            },
            b => b.Id);

    private static List<Badge>? ReadBadges(FieldReader root) =>
        ReadRecords(root, "badges", "Badge", "id",
            r => new Badge(r.RequiredId("id"), r.RequiredString("name"), r.RequiredString("metric"), r.RequiredPositiveDouble("target")),
            b => b.Id);

    private static List<Guide>? ReadGuides(FieldReader root) =>
        ReadRecords(root, "guides", "Guide", "exerciseId",
            r => new Guide(
                r.RequiredExercise("exerciseId"),
                r.OptionalString("works"),
                r.OptionalString("whyInPlan"),
                r.OptionalString("setup"),
                r.OptionalString("howTo"),
                r.OptionalStringList("cues"),
                r.OptionalStringList("commonMistakes"),
                r.OptionalString("easier"),
                r.OptionalString("harder"),
                r.OptionalStringList("watchOuts"),
                r.OptionalWebUrl("videoUrl"),
                r.OptionalString("personalNotes")),
            g => g.ExerciseId);
}
