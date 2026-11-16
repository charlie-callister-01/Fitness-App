using Core.Model;

namespace Core.Serialisation;

/// <summary>What <see cref="BodyAndGuideReader"/> found; a null list means that collection was absent from the document.</summary>
internal sealed record BodyAndGuideData(
    IReadOnlyList<WeighIn>? WeighIns,
    IReadOnlyList<Break>? Breaks,
    IReadOnlyList<Badge>? Badges,
    IReadOnlyList<Guide>? Guides);

/// <summary>Reads the Weigh-in, Break, Badge and Guide collections of a version 1 document (structure only; IDs and references are checked by the validator).</summary>
internal static class BodyAndGuideReader
{
    public static BodyAndGuideData Read(FieldReader root) => new(
        ReadRecords(root, "weighIns",
            r => new WeighIn(r.RequiredId("id"), r.RequiredDate("date"), r.RequiredPositiveDouble("weightKg"))),
        ReadRecords(root, "breaks", ReadBreak),
        ReadRecords(root, "badges",
            r => new Badge(r.RequiredId("id"), r.RequiredString("name"), r.RequiredString("metric"), r.RequiredPositiveDouble("target"))),
        ReadRecords(root, "guides", ReadGuide));

    private static List<T>? ReadRecords<T>(FieldReader root, string collection, Func<FieldReader, T> read) where T : class =>
        root.OptionalObjectArray(collection)?
            .Select(item => item.Reader.ReadRecord(read))
            .OfType<T>()
            .ToList();

    private static Break ReadBreak(FieldReader r)
    {
        var id = r.RequiredId("id");
        var start = r.RequiredDate("startDate");
        var end = r.RequiredDate("endDate");
        var reason = r.RequiredString("reason");
        r.CheckDateOrder("startDate", "endDate", start, end);
        return new Break(id, start, end, reason);
    }

    private static Guide ReadGuide(FieldReader r) => new(
        r.RequiredId("exerciseId"),
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
        r.OptionalString("personalNotes"));
}
