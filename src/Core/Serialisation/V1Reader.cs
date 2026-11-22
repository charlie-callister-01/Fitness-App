using System.Text.Json;
using Core.Model;

namespace Core.Serialisation;

/// <summary>Reads and validates a format version 1 document, upgrading its Plans into Tracks via <see cref="V1ToV2Upgrader"/>.</summary>
internal static class V1Reader
{
    public static ReadResult Read(JsonElement root) => DocumentReader.Read(root, PlanReader.ReadV1);
}

/// <summary>Reads and validates a format version 2 document.</summary>
internal static class V2Reader
{
    public static ReadResult Read(JsonElement root) => DocumentReader.Read(root, PlanReader.ReadV2);
}

/// <summary>
/// Shared envelope reader for exchange documents: reads common collections using one <see cref="ReadContext"/>, delegates <c>plans</c>
/// to the version-specific reader, then runs <see cref="DocumentValidator"/>.
/// </summary>
internal static class DocumentReader
{
    public static ReadResult Read(JsonElement root, Func<FieldReader, List<Plan>?> readPlans)
    {
        var context = new ReadContext();
        var fields = FieldReader.OpenRoot(root, context);

        _ = fields.RequiredInt("formatVersion", 1);
        fields.Ignore(ExchangeReadMe.Field);
        var exportedAt = fields.OptionalTimestamp("exportedAt");
        var exportedFrom = fields.OptionalString("exportedFrom");
        var exercises = ReadExercises(fields);
        var settings = ReadSettings(fields);
        var bodyAndGuides = BodyAndGuideReader.Read(fields);
        var plans = readPlans(fields);
        var workouts = WorkoutReader.Read(fields);
        var weekSchedules = WeekScheduleReader.Read(fields);
        fields.WarnAboutUnknownFields();

        var document = new ExchangeDocument(ExchangeFormat.CurrentVersion, exportedAt, exportedFrom, exercises, settings,
            bodyAndGuides.WeighIns, bodyAndGuides.Breaks, bodyAndGuides.Badges, bodyAndGuides.Guides, plans, workouts, weekSchedules);
        context.Sink.ReportAll(DocumentValidator.Validate(document, context.Findings));
        return ReadResult.From(document, context.Sink);
    }

    private static List<Exercise>? ReadExercises(FieldReader root) =>
        root.ReadRecords("exercises", reader =>
        {
            var id = reader.RequiredId("id");
            var name = reader.RequiredString("name");
            var logKind = reader.RequiredEnum<LogKind>("logKind");
            var isBenchmark = reader.OptionalBool("isBenchmark", defaultValue: false);
            var tags = ReadTags(reader);
            return new Exercise(id, name, logKind, tags, isBenchmark);
        });

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
            reader.RequiredBool("streakCountsCustomWorkouts"),
            reader.EnumOrDefault("theme", Theme.Keep),
            reader.EnumOrDefault("mode", Mode.System)));
}
