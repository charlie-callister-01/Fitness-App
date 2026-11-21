using Core.Model;

namespace Core.Rules.Editing;

/// <summary>Changes the Exercise menu makes to a library Exercise.</summary>
public static class ExerciseEdits
{
    /// <summary>The Exercise with only its benchmark flag set to <paramref name="isBenchmark"/>.</summary>
    public static Exercise WithBenchmark(Exercise exercise, bool isBenchmark) => exercise with { IsBenchmark = isBenchmark };
}
