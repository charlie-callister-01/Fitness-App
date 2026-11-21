using Core.Model;
using Core.Rules.Editing;
using Ui.Log;

namespace Ui.Components;

/// <summary>
/// Everything the Exercise menu needs for the shown Exercise. <c>RowId</c> is the planned row Skip, Swap and Un-skip act on;
/// <see cref="Facts"/> feed the Core availability rule.
/// </summary>
public sealed record ExerciseMenuState(
    string ExerciseId, string RowId, string ExerciseName, ExerciseMenuFacts Facts, Guide? Guide, ExercisePickerChoices? Picker)
{
    public ExerciseMenuAvailability Items => ExerciseMenuAvailability.Of(Facts);

    public static ExerciseMenuState For(ExerciseView shown, Guide? guide, bool canWrite) => new(
        shown.Info.ExerciseId,
        shown.Swap?.OriginalId ?? shown.Info.ExerciseId,
        shown.Info.Name,
        new ExerciseMenuFacts(
            IsSkipped: shown.Skip is not null,
            IsSwapped: shown.Swap is not null,
            IsAdded: shown.Added,
            HasSets: shown.Rows?.Items.Any(r => r.Marks.Stored) ?? false,
            IsBenchmark: shown.Info.IsBenchmark,
            CanWrite: canWrite,
            HasChoices: shown.Picker is not null),
        guide,
        shown.Picker);
}
