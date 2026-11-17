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

/// <summary>
/// The owner's answers to the questions a preview asks. Passed back into the preview each time, so there is no hidden state.
/// Exercise choices are keyed by the Exercise ID as it appears in the file.
/// </summary>
public sealed record ImportDecisions
{
    public static ImportDecisions None { get; } = new();

    public ImmutableDictionary<string, ExerciseChoice> Exercises { get; init; } = ImmutableDictionary<string, ExerciseChoice>.Empty;

    public ImportDecisions WithExercise(string fileExerciseId, ExerciseChoice choice) =>
        this with { Exercises = Exercises.SetItem(fileExerciseId, choice) };
}
