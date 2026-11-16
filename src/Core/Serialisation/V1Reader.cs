using System.Text.Json;
using Core.Model;

namespace Core.Serialisation;

/// <summary>
/// Reads and validates a format version 1 document. A future version N+1 adds a reader (or a migration from N) beside this one.
/// <para>
/// Two phases. Parse: one <see cref="ReadContext"/> is carried by every <see cref="FieldReader"/>, each record is read through
/// <see cref="FieldReader.ReadRecord{T}"/>, and only structural errors are collected; a record with a bad field is kept, with a placeholder
/// in that field. Validate: <see cref="DocumentValidator"/> then checks every cross-record rule over the parsed document.
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
        var settings = ReadSettings(fields);
        var bodyAndGuides = BodyAndGuideReader.Read(fields);
        var plans = PlanReader.Read(fields);
        var workouts = WorkoutReader.Read(fields);
        fields.WarnAboutUnknownFields();

        var document = new ExchangeDocument(version, exportedAt, exportedFrom, exercises, settings,
            bodyAndGuides.WeighIns, bodyAndGuides.Breaks, bodyAndGuides.Badges, bodyAndGuides.Guides, plans, workouts);
        context.Sink.Errors.AddRange(CrossRecordIssues(document, context.Sink.Errors));
        return ReadResult.From(document, context.Sink);
    }

    /// <summary>
    /// The validate phase, minus what the parse phase already explained. A field that failed to read holds a placeholder, so two kinds of
    /// follow-on issue are dropped: one at a path the parse phase already reported, and "no active Plan" when a Plan's <c>isActive</c> was unreadable.
    /// </summary>
    private static IEnumerable<FormatIssue> CrossRecordIssues(ExchangeDocument document, IReadOnlyList<FormatIssue> parseErrors)
    {
        var parsePaths = parseErrors.Select(e => e.Path).ToHashSet();
        var isActiveUnreadable = parsePaths.Any(p => p.StartsWith("plans[", StringComparison.Ordinal) && p.EndsWith("].isActive", StringComparison.Ordinal));
        return DocumentValidator.Validate(document)
            .Where(issue => !parsePaths.Contains(issue.Path) && !(isActiveUnreadable && issue.Code == "no_active_plan"));
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
