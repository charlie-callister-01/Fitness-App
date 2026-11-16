using Core.Model;

namespace Core.Serialisation;

/// <summary>What <see cref="BodyAndGuideReader"/> found; a null list means that collection was absent from the document.</summary>
internal sealed record BodyAndGuideData(
    IReadOnlyList<WeighIn>? WeighIns,
    IReadOnlyList<Break>? Breaks,
    IReadOnlyList<Badge>? Badges,
    IReadOnlyList<Guide>? Guides);

/// <summary>Reads the Weigh-in, Break, Badge and Guide collections of a version 1 document.</summary>
internal static class BodyAndGuideReader
{
    /// <param name="exercises">The document's Exercises when it has any; Guides are checked against them. Null skips the check (a Guides-only document).</param>
    public static BodyAndGuideData Read(FieldReader root, IssueSink sink, IReadOnlyList<Exercise>? exercises) => new(
        ReadWeighIns(root, sink),
        ReadBreaks(root, sink),
        ReadBadges(root, sink),
        ReadGuides(root, sink, exercises?.Select(e => e.Id).ToHashSet()));

    /// <summary>Reads each element of a collection, reports duplicate keys, and keeps only elements that were fully valid.</summary>
    private static List<T>? ReadRecords<T>(FieldReader root, IssueSink sink, string collection, string keyField, Func<FieldReader, T> read, Func<T, string> key)
    {
        var items = root.OptionalObjectArray(collection);
        if (items is null)
        {
            return null;
        }

        var records = new List<T>();
        var seen = new HashSet<string>();
        foreach (var (reader, _) in items)
        {
            var errorsBefore = sink.Errors.Count;
            var record = read(reader);
            reader.WarnAboutUnknownFields();
            var recordKey = key(record);
            if (recordKey.Length > 0 && !seen.Add(recordKey))
            {
                var path = reader.ChildPath(keyField);
                sink.Error(path, "duplicate_id", $"{path} \"{recordKey}\" is used more than once in {collection}. Each needs its own.");
            }

            if (sink.Errors.Count == errorsBefore)
            {
                records.Add(record);
            }
        }

        return records;
    }

    private static List<WeighIn>? ReadWeighIns(FieldReader root, IssueSink sink) =>
        ReadRecords(root, sink, "weighIns", "id",
            r => new WeighIn(r.RequiredId("id"), r.RequiredDate("date"), r.RequiredPositiveDouble("weightKg")),
            w => w.Id);

    private static List<Break>? ReadBreaks(FieldReader root, IssueSink sink) =>
        ReadRecords(root, sink, "breaks", "id",
            r =>
            {
                var id = r.RequiredId("id");
                var start = r.RequiredDate("startDate");
                var end = r.RequiredDate("endDate");
                var reason = r.RequiredString("reason");
                if (start != default && end != default && end < start)
                {
                    var path = r.ChildPath("endDate");
                    sink.Error(path, "end_before_start", $"{path} ({end:yyyy-MM-dd}) can't be before {r.ChildPath("startDate")} ({start:yyyy-MM-dd}).");
                }

                return new Break(id, start, end, reason);
            },
            b => b.Id);

    private static List<Badge>? ReadBadges(FieldReader root, IssueSink sink) =>
        ReadRecords(root, sink, "badges", "id",
            r => new Badge(r.RequiredId("id"), r.RequiredString("name"), r.RequiredString("metric"), r.RequiredPositiveDouble("target")),
            b => b.Id);

    private static List<Guide>? ReadGuides(FieldReader root, IssueSink sink, HashSet<string>? knownExerciseIds) =>
        ReadRecords(root, sink, "guides", "exerciseId",
            r =>
            {
                var exerciseId = r.RequiredId("exerciseId");
                if (exerciseId.Length > 0 && knownExerciseIds is not null && !knownExerciseIds.Contains(exerciseId))
                {
                    var path = r.ChildPath("exerciseId");
                    sink.Error(path, "unknown_exercise", $"{path} \"{exerciseId}\" is not an Exercise in this document. Add the Exercise or fix the ID.");
                }

                return new Guide(
                    exerciseId,
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
            },
            g => g.ExerciseId);
}
