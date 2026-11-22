using Core.Model;
using Core.Rules.Formatting;
using Core.Rules.Parsing;
using Core.Rules.Insight;
using Core.Rules.Performance;
using Core.Rules.Units;

namespace Core.Rules.Badges;

/// <summary>The words the Badge editor shows while the owner types: the "Now / Unlocks when" line, the estimated 1RM line and number text. The rules that decide are in <see cref="BadgeEditingRules"/>.</summary>
public static class BadgeEditingText
{
    private const string FillIn = "Fill in the target to see where you stand.";

    /// <summary>
    /// For a Load Exercise, the plain line that its target is stored as an estimated 1RM, with the weight and reps typed; otherwise null
    /// (also null while the draft is incomplete).
    /// </summary>
    public static string? OneRepMaxLine(BadgeDraft draft, IReadOnlyList<Exercise> exercises, UnitConverter units)
    {
        if (draft.Metric != BadgeMetricChoice.ExerciseBest
            || exercises.FirstOrDefault(e => e.Id == draft.ExerciseId) is not { } exercise
            || !BadgeEditingRules.UsesWeightAndReps(exercise)
            || !BadgeEditingRules.ValidateIgnoringName(draft, exercises, units).TryGet(out var badge, out _))
        {
            return null;
        }

        var shown = $"{DisplayFormat.Rounded(units.DisplayWeight(badge.Target, rounded: false), 1)} {units.WeightLabel}";
        return $"This target is stored as an estimated 1RM of {shown} ({DisplayFormat.Rounded(DecimalText.ParseOrZero(draft.Target), 1)} {units.WeightLabel} for {draft.Reps.Trim()} reps).";
    }

    /// <summary>
    /// "Now: 22 × 9. Unlocks when: you lift 24 × 8 or better." from the draft and the owner's history, so a target can be sanity-checked
    /// before saving. An incomplete draft asks for the target; an Exercise that cannot be measured says so.
    /// </summary>
    public static string LiveLine(BadgeDraft draft, BadgeHistory history, DateOnly today, UnitConverter units)
    {
        var exercise = draft.Metric == BadgeMetricChoice.ExerciseBest
            ? history.Exercises.FirstOrDefault(e => e.Id == draft.ExerciseId)
            : null;
        if (exercise is not null && BadgeEditingRules.Unmeasurable(exercise))
        {
            return BadgeEditingRules.CannotMeasure(exercise);
        }

        if (!BadgeEditingRules.ValidateIgnoringName(draft, history.Exercises, units).TryGet(out var badge, out _))
        {
            return FillIn;
        }

        var progress = BadgeRules.Progress(badge, history, today);
        var view = TrophyViewBuilder.Build(history.Plan, history.Exercises, [badge], history.Workouts, history.Breaks, history.WeighIns, today, units);
        string unlocks;
        string now;
        if (exercise is not null && BadgeEditingRules.UsesWeightAndReps(exercise))
        {
            unlocks = $"you lift {DisplayFormat.Rounded(DecimalText.ParseOrZero(draft.Target))} × {draft.Reps.Trim()} or better";
            now = ExercisePerformance.Best(exercise, history.Workouts) is { Set: { WeightKg: { } w, Reps: { } r } }
                ? $"{DisplayFormat.Rounded(units.DisplayWeight(w))} × {r}"
                : NothingLogged;
        }
        else
        {
            var needs = view.Badges[0].Needs;
            unlocks = char.ToLowerInvariant(needs[0]) + needs[1..];
            now = HasData(draft, exercise, history) ? DisplayFormat.Rounded(DisplayValue(draft, progress.Value, units)) : NothingLogged;
        }

        return $"Now: {now}. Unlocks when: {unlocks}.";
    }

    private const string NothingLogged = "nothing logged yet";

    private static double DisplayValue(BadgeDraft draft, double value, UnitConverter units) =>
        draft.Metric == BadgeMetricChoice.BodyWeight ? units.DisplayWeight(value) : value;

    private static bool HasData(BadgeDraft draft, Exercise? exercise, BadgeHistory history) => draft.Metric switch
    {
        BadgeMetricChoice.BodyWeight => history.WeighIns.Count > 0,
        BadgeMetricChoice.ExerciseBest => exercise is not null
            && (ExercisePerformance.Best(exercise, history.Workouts) is not null || ExercisePerformance.BodyChange(exercise, history.Workouts) is not null),
        _ => true,
    };
}
