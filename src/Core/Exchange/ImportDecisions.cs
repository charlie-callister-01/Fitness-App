using System.Collections.Immutable;

namespace Core.Exchange;

/// <summary>The owner's answer to a near-match question: use one of the suggested library Exercises, or add the file's Exercise as new.</summary>
public sealed record ExerciseChoice
{
    private ExerciseChoice(string? useExerciseId) => UseExerciseId = useExerciseId;

    /// <summary>The library Exercise to use, or null for "add as a new Exercise".</summary>
    public string? UseExerciseId { get; }

    public static ExerciseChoice AddAsNew { get; } = new((string?)null);

    public static ExerciseChoice Use(string libraryExerciseId) => new(libraryExerciseId);
}

/// <summary>What to do with a record the library already has when the file has a different version. Keep is the default everywhere.</summary>
public enum KeepOrReplace
{
    Keep,
    Replace,
}

/// <summary>
/// The owner's answers to the questions a preview asks. Passed back into the preview each time, so there is no hidden state.
/// Exercise choices are keyed by the Exercise ID as it appears in the file. Guide choices are keyed by the Exercise ID the Guide ends up
/// under (the library's, after matching). Workout choices are keyed by Workout ID. A missing key means <see cref="KeepOrReplace.Keep"/>.
/// </summary>
public sealed record ImportDecisions
{
    public static ImportDecisions None { get; } = new();

    public ImmutableDictionary<string, ExerciseChoice> Exercises { get; init; } = ImmutableDictionary<string, ExerciseChoice>.Empty;

    public ImmutableDictionary<string, KeepOrReplace> Guides { get; init; } = ImmutableDictionary<string, KeepOrReplace>.Empty;

    public ImmutableDictionary<string, KeepOrReplace> Workouts { get; init; } = ImmutableDictionary<string, KeepOrReplace>.Empty;

    public ImportDecisions WithExercise(string fileExerciseId, ExerciseChoice choice) =>
        this with { Exercises = Exercises.SetItem(fileExerciseId, choice) };

    public ImportDecisions WithGuide(string exerciseId, KeepOrReplace choice) =>
        this with { Guides = Guides.SetItem(exerciseId, choice) };

    public ImportDecisions WithWorkout(string workoutId, KeepOrReplace choice) =>
        this with { Workouts = Workouts.SetItem(workoutId, choice) };

    public KeepOrReplace GuideChoice(string exerciseId) => Guides.GetValueOrDefault(exerciseId);

    public KeepOrReplace WorkoutChoice(string workoutId) => Workouts.GetValueOrDefault(workoutId);
}
