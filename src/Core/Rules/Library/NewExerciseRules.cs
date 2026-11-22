using Core.Ids;
using Core.Model;
using Core.Rules.Guides;

namespace Core.Rules.Library;

/// <summary>What making a New Exercise came to: the Exercise to save, or the plain reason it was refused.</summary>
public abstract record NewExerciseResult
{
    /// <summary>
    /// The Exercise is valid. <see cref="Guide"/> is a Guide holding the cue (as its only Cue) when one was typed, else null: the cue is
    /// Guide content, not a field of the Exercise.
    /// </summary>
    public sealed record Valid(Exercise Exercise, Guide? Guide) : NewExerciseResult;

    public sealed record Invalid(string Message) : NewExerciseResult;
}

/// <summary>The rules for adding an Exercise to the Exercise library.</summary>
public static class NewExerciseRules
{
    /// <summary>The log kinds a New Exercise may use: every kind except the two body measurements, which belong to Weight and Body.</summary>
    public static IReadOnlyList<LogKind> OfferedKinds { get; } =
    [
        .. Enum.GetValues<LogKind>().Where(k => k is not (LogKind.BodyMeasurementKg or LogKind.BodyMeasurementCm)),
    ];

    /// <summary>
    /// Validates <paramref name="name"/> (not empty, and not the name of an Exercise already in the library, ignoring case and
    /// surrounding spaces), <paramref name="kind"/> (one of <see cref="OfferedKinds"/>), the optional tags and the optional cue.
    /// A valid Exercise gets a permanent id from <paramref name="ids"/>, is not a Benchmark exercise, and has no tags when none were given.
    /// </summary>
    public static NewExerciseResult Create(
        string? name, LogKind kind, ExerciseTags? tags, string? cue, IEnumerable<Exercise> library, IIdGenerator ids)
    {
        var trimmed = name?.Trim() ?? "";
        if (trimmed.Length == 0)
        {
            return new NewExerciseResult.Invalid("Give the Exercise a name.");
        }

        if (library.FirstOrDefault(e => string.Equals(e.Name.Trim(), trimmed, StringComparison.OrdinalIgnoreCase)) is { } existing)
        {
            return new NewExerciseResult.Invalid($"You already have an Exercise called \"{existing.Name}\".");
        }

        if (!OfferedKinds.Contains(kind))
        {
            return new NewExerciseResult.Invalid("Choose how this Exercise is logged.");
        }

        var exercise = new Exercise(ids.NewId(), trimmed, kind, Clean(tags), IsBenchmark: false);
        var cueText = cue?.Trim();
        var guide = string.IsNullOrEmpty(cueText) ? null : GuideEdits.Empty(exercise.Id) with { Cues = [cueText] };
        return new NewExerciseResult.Valid(exercise, guide);
    }

    private static ExerciseTags? Clean(ExerciseTags? tags)
    {
        if (tags is null)
        {
            return null;
        }

        var cleaned = new ExerciseTags(Words(tags.MuscleGroups), Words(tags.Equipment), Words(tags.MovementPatterns));
        return cleaned.MuscleGroups.Count + cleaned.Equipment.Count + cleaned.MovementPatterns.Count == 0 ? null : cleaned;
    }

    private static IReadOnlyList<string> Words(IReadOnlyList<string> tags) =>
        [.. tags.Select(t => t.Trim()).Where(t => t.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase)];
}
