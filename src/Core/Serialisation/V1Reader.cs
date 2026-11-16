using System.Text.Json;
using Core.Model;

namespace Core.Serialisation;

/// <summary>Reads and validates a format version 1 document. A future version N+1 adds a reader (or a migration from N) beside this one.</summary>
internal static class V1Reader
{
    public static ReadResult Read(JsonElement root)
    {
        var sink = new IssueSink();
        var fields = FieldReader.Open(root, "$", sink)!;

        var version = fields.RequiredInt("formatVersion", 1);
        var exportedAt = fields.OptionalTimestamp("exportedAt");
        var exportedFrom = fields.OptionalString("exportedFrom");
        var exercises = ReadExercises(fields, sink);
        var settings = ReadSettings(fields);
        var bodyAndGuides = BodyAndGuideReader.Read(fields, sink, exercises);
        var plans = PlanReader.Read(fields, exercises, sink);
        var workouts = WorkoutReader.Read(fields, exercises, plans, sink);
        fields.WarnAboutUnknownFields();

        return ReadResult.From(
            new ExchangeDocument(version, exportedAt, exportedFrom, exercises, settings,
                bodyAndGuides.WeighIns, bodyAndGuides.Breaks, bodyAndGuides.Badges, bodyAndGuides.Guides, plans, workouts),
            sink);
    }

    private static List<Exercise>? ReadExercises(FieldReader root, IssueSink sink)
    {
        var items = root.OptionalObjectArray("exercises");
        if (items is null)
        {
            return null;
        }

        var exercises = new List<Exercise>();
        var seenIds = new HashSet<string>();
        foreach (var (reader, _) in items)
        {
            var errorsBefore = sink.Errors.Count;
            var id = reader.RequiredId("id");
            var name = reader.RequiredString("name");
            var logKind = reader.RequiredEnum<LogKind>("logKind");
            var isBenchmark = reader.OptionalBool("isBenchmark", defaultValue: false);
            var tags = ReadTags(reader);
            reader.WarnAboutUnknownFields();

            if (id.Length > 0 && !seenIds.Add(id))
            {
                var path = reader.ChildPath("id");
                sink.Error(path, "duplicate_id", $"{path} \"{id}\" is used by more than one Exercise. Every Exercise needs its own ID.");
            }

            if (sink.Errors.Count == errorsBefore)
            {
                exercises.Add(new Exercise(id, name, logKind, tags, isBenchmark));
            }
        }

        return exercises;
    }

    private static ExerciseTags? ReadTags(FieldReader exercise)
    {
        var reader = exercise.OptionalObject("tags");
        if (reader is null)
        {
            return null;
        }

        var tags = new ExerciseTags(
            reader.OptionalStringList("muscleGroups"),
            reader.OptionalStringList("equipment"),
            reader.OptionalStringList("movementPatterns"));
        reader.WarnAboutUnknownFields();
        return tags;
    }

    private static Settings? ReadSettings(FieldReader root)
    {
        var reader = root.OptionalObject("settings");
        if (reader is null)
        {
            return null;
        }

        var settings = new Settings(
            reader.RequiredEnum<WeightUnit>("displayWeightUnit"),
            reader.RequiredEnum<DistanceUnit>("displayDistanceUnit"),
            reader.RequiredPositiveDouble("weightIncrementKg"),
            reader.RequiredInt("restTimerSeconds", min: 0),
            reader.OptionalPositiveDouble("goalWeightKg"),
            reader.RequiredInt("streakMinWorkoutsPerWeek", min: 1),
            reader.RequiredBool("streakCountsCustomWorkouts"));
        reader.WarnAboutUnknownFields();
        return settings;
    }
}
