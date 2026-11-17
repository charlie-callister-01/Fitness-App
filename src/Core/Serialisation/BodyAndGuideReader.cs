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
        root.ReadRecords("weighIns",
            r => new WeighIn(r.RequiredId("id"), r.RequiredDate("date"), r.RequiredPositiveDouble("weightKg"))),
        root.ReadRecords("breaks", ReadBreak),
        root.ReadRecords("badges",
            r => new Badge(r.RequiredId("id"), r.RequiredString("name"), r.RequiredString("metric"), r.RequiredNonZeroDouble("target"))),
        root.ReadRecords("guides", ReadGuide));

    private static Break ReadBreak(FieldReader r)
    {
        var id = r.RequiredId("id");
        var start = r.RequiredDateOrNull("startDate");
        var end = r.RequiredDateOrNull("endDate");
        var reason = r.RequiredString("reason");
        r.CheckDateOrder("startDate", "endDate", start, end);
        return new Break(id, start ?? default, end ?? default, reason);
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
