using System.Text.Json;
using Core.Model;

namespace Core.Serialisation;

/// <summary>
/// Reads and validates a format version 1 document. A future version N+1 adds a reader (or a migration from N) beside this one.
/// <para>
/// Flow: one <see cref="ReadContext"/> is built here and carried by every <see cref="FieldReader"/>. Collections are read in dependency order
/// (Exercises, Settings, Body and Guides, Plans, Workouts). After Exercises and Plans are read, their IDs become the context's reference
/// scopes, so later collections are checked against them. Each record is read through <see cref="FieldReader.ReadRecord{T}"/>, which drops
/// records that produced errors and always reports unknown fields.
/// </para>
/// </summary>
internal static class V1Reader
{
    public static ReadResult Read(JsonElement root)
    {
        var context = new ReadContext();
        var fields = FieldReader.Open(root, "$", context)!;

        var version = fields.RequiredInt("formatVersion", 1);
        fields.Ignore(ExchangeReadMe.Field);
        var exportedAt = fields.OptionalTimestamp("exportedAt");
        var exportedFrom = fields.OptionalString("exportedFrom");
        var exercises = ReadExercises(fields);
        context.Exercises = ReferenceScope.ForExercises(exercises);
        var settings = ReadSettings(fields);
        var bodyAndGuides = BodyAndGuideReader.Read(fields);
        var plans = PlanReader.Read(fields);
        context.Sessions = ReferenceScope.ForSessions(plans);
        var workouts = WorkoutReader.Read(fields);
        fields.WarnAboutUnknownFields();

        return ReadResult.From(
            new ExchangeDocument(version, exportedAt, exportedFrom, exercises, settings,
                bodyAndGuides.WeighIns, bodyAndGuides.Breaks, bodyAndGuides.Badges, bodyAndGuides.Guides, plans, workouts),
            context.Sink);
    }

    private static List<Exercise>? ReadExercises(FieldReader root)
    {
        var items = root.OptionalObjectArray("exercises");
        if (items is null)
        {
            return null;
        }

        return items
            .Select(item => item.Reader.ReadRecord(reader =>
            {
                var id = reader.RequiredId("id");
                var name = reader.RequiredString("name");
                var logKind = reader.RequiredEnum<LogKind>("logKind");
                var isBenchmark = reader.OptionalBool("isBenchmark", defaultValue: false);
                var tags = ReadTags(reader);
                reader.CheckUnique("Exercise", "id", id);
                return new Exercise(id, name, logKind, tags, isBenchmark);
            }))
            .OfType<Exercise>()
            .ToList();
    }

    private static ExerciseTags? ReadTags(FieldReader exercise) =>
        exercise.OptionalObject("tags")?.ReadRecord(reader => new ExerciseTags(
            reader.OptionalStringList("muscleGroups"),
            reader.OptionalStringList("equipment"),
            reader.OptionalStringList("movementPatterns")));

    private static Settings? ReadSettings(FieldReader root) =>
        root.OptionalObject("settings")?.ReadRecord(reader => new Settings(
            reader.RequiredEnum<WeightUnit>("displayWeightUnit"),
            reader.RequiredEnum<DistanceUnit>("displayDistanceUnit"),
            reader.RequiredPositiveDouble("weightIncrementKg"),
            reader.RequiredInt("restTimerSeconds", min: 0),
            reader.OptionalPositiveDouble("goalWeightKg"),
            reader.RequiredInt("streakMinWorkoutsPerWeek", min: 1),
            reader.RequiredBool("streakCountsCustomWorkouts")));
}
