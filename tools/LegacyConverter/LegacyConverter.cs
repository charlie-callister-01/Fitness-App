using Core.Model;
using Core.Serialisation;

namespace LegacyConverter;

/// <summary>
/// Converts a legacy Obsidian Fitness folder into a format-2 <see cref="ExchangeDocument"/> and conversion notes.
/// </summary>
public static class LegacyConverter
{
    public static ConversionResult Convert(LegacyFolder folder)
    {
        ArgumentNullException.ThrowIfNull(folder);

        var notes = new List<ConversionNote>();
        var config = ConfigParser.Parse(folder.ConfigMarkdown);
        var exercises = ExerciseTagsTable.ApplyTags(
            ExerciseBuilder.Build(config.Exercises, notes),
            notes);
        var exercisesById = exercises.ToDictionary(e => e.Id, StringComparer.Ordinal);
        var knownExerciseIds = exercisesById.Keys.ToHashSet(StringComparer.Ordinal);
        var guides = GuideBuilder.Build(folder.Guides, knownExerciseIds, notes);
        var badges = BadgeBuilder.Build(config.Badges, exercisesById, notes);
        var settings = PlanBuilder.BuildSettings(config);
        var plan = PlanBuilder.BuildPlan(config, notes);
        var (weighIns, workouts) = HistoryBuilder.Build(folder, plan, exercisesById, notes);

        var document = new ExchangeDocument(
            FormatVersion: ExchangeFormat.CurrentVersion,
            ExportedAt: null,
            ExportedFrom: "legacy-converter",
            Exercises: exercises,
            Settings: settings,
            WeighIns: weighIns,
            Breaks: [],
            Badges: badges,
            Guides: guides,
            Plans: [plan],
            Workouts: workouts);

        return new ConversionResult(document, notes);
    }
}
