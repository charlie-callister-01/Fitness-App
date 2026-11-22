using System.Globalization;
using Core.Ids;
using Core.Model;
using Core.Rules.Insight;
using Core.Rules.Parsing;
using Core.Rules.Performance;
using Core.Rules.PlanEditing;
using Core.Rules.Units;

namespace Core.Rules.Badges;

/// <summary>What a Badge counts, as the editor offers it. Each maps to one of the supported metric names (Exercise best to an Exercise ID).</summary>
public enum BadgeMetricChoice
{
    Workouts,
    Streak,
    BestWeek,
    PrCount,
    ExerciseBest,
    BodyWeight,
}

/// <summary>
/// A Badge as the editor holds it while the owner types: the fields are text, the target is in the owner's units, and
/// <paramref name="Reps"/> is used only for a Load Exercise (weight x reps). <paramref name="DropBy"/> is used only for body weight.
/// </summary>
public sealed record BadgeDraft(
    string Name,
    BadgeMetricChoice Metric,
    string? ExerciseId,
    string Target,
    string Reps,
    bool DropBy);

/// <summary>
/// Creating, changing and deleting a Badge: pure rules in the same shape as the other editing rules, returning the plain reason when an edit is
/// refused. The art a Badge wears comes from its metric and target (<see cref="BadgeArt"/>), so it is not stored.
/// </summary>
public static class BadgeEditingRules
{
    private const string NameNeeded = "Give the Badge a name.";
    private const string MetricNeeded = "Choose what the Badge counts.";
    private const string ExerciseNeeded = "Choose an Exercise for this Badge.";
    private const string ExerciseMissing = "That Exercise is not in your library.";
    private const string TargetNeeded = "Enter a target above zero.";
    private const string RepsNeeded = "Enter the reps as a whole number above zero.";
    private const string Gone = "That Badge is no longer there.";
    private const string FillIn = "Fill in the target to see where you stand.";

    /// <summary>The supported metric name a choice stands for; <paramref name="exerciseId"/> is the metric for Exercise best.</summary>
    public static string MetricName(BadgeMetricChoice choice, string? exerciseId) => choice switch
    {
        BadgeMetricChoice.Workouts => "sessions",
        BadgeMetricChoice.Streak => "streak",
        BadgeMetricChoice.BestWeek => "bestweek",
        BadgeMetricChoice.PrCount => "prs",
        BadgeMetricChoice.BodyWeight => "bodyweight",
        _ => exerciseId ?? "",
    };

    /// <summary>True for an Exercise whose target is weight x reps (stored as an estimated one-rep max).</summary>
    public static bool UsesWeightAndReps(Exercise exercise) => exercise.LogKind == LogKind.Load;

    /// <summary>Checks a draft and builds the Badge it describes under <paramref name="id"/>, or says why not.</summary>
    public static PlanEditResult<Badge> Validate(BadgeDraft draft, string id, IReadOnlyList<Exercise> exercises, UnitConverter units)
    {
        if (string.IsNullOrWhiteSpace(draft.Name))
        {
            return Refuse(NameNeeded);
        }

        if (!Enum.IsDefined(draft.Metric))
        {
            return Refuse(MetricNeeded);
        }

        Exercise? exercise = null;
        if (draft.Metric == BadgeMetricChoice.ExerciseBest)
        {
            if (string.IsNullOrEmpty(draft.ExerciseId))
            {
                return Refuse(ExerciseNeeded);
            }

            exercise = exercises.FirstOrDefault(e => e.Id == draft.ExerciseId);
            if (exercise is null)
            {
                return Refuse(ExerciseMissing);
            }

            if (Unmeasurable(exercise))
            {
                return Refuse(CannotMeasure(exercise));
            }
        }

        if (!TryNumber(draft.Target, out var number))
        {
            return Refuse(TargetNeeded);
        }

        double target = number;
        if (exercise is not null && UsesWeightAndReps(exercise))
        {
            if (!int.TryParse(draft.Reps.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var reps) || reps <= 0)
            {
                return Refuse(RepsNeeded);
            }

            target = SetRanking.EstimatedOneRepMax(units.WeightFromDisplay(number), reps);
        }
        else if (draft.Metric == BadgeMetricChoice.BodyWeight)
        {
            target = units.WeightFromDisplay(number);
            if (draft.DropBy)
            {
                target = -target;
            }
        }

        return new PlanEditResult<Badge>.Valid(new Badge(id, draft.Name.Trim(), MetricName(draft.Metric, draft.ExerciseId), target));
    }

    /// <summary>A new Badge from a draft, under a new permanent ID.</summary>
    public static PlanEditResult<Badge> Add(BadgeDraft draft, IReadOnlyList<Exercise> exercises, UnitConverter units, IIdGenerator ids) =>
        Validate(draft, ids.NewId(), exercises, units);

    /// <summary>The Badge <paramref name="id"/> changed to the draft, keeping its ID. Refused when it is gone or the draft is not valid.</summary>
    public static PlanEditResult<Badge> Replace(
        IReadOnlyList<Badge> badges, string id, BadgeDraft draft, IReadOnlyList<Exercise> exercises, UnitConverter units) =>
        badges.FirstOrDefault(b => b.Id == id) is not { } existing ? Refuse(Gone) : Validate(draft, id, exercises, units) switch
        {
            // Opening a Load Badge shows its estimated 1RM as one rep, rounded for the eye. Saving that draft untouched must not move the target.
            PlanEditResult<Badge>.Valid { Value: var saved } when SameAsOpened(draft, existing, exercises, units) =>
                new PlanEditResult<Badge>.Valid(saved with { Target = existing.Target }),
            var result => result,
        };

    private static bool SameAsOpened(BadgeDraft draft, Badge existing, IReadOnlyList<Exercise> exercises, UnitConverter units) =>
        draft with { Name = "" } == DraftFor(existing, exercises, units) with { Name = "" };

    /// <summary>
    /// For a Load Exercise, the plain line that its target is stored as an estimated 1RM, with the weight and reps typed; otherwise null
    /// (also null while the draft is incomplete).
    /// </summary>
    public static string? OneRepMaxLine(BadgeDraft draft, IReadOnlyList<Exercise> exercises, UnitConverter units)
    {
        if (draft.Metric != BadgeMetricChoice.ExerciseBest
            || exercises.FirstOrDefault(e => e.Id == draft.ExerciseId) is not { } exercise
            || !UsesWeightAndReps(exercise)
            || !Validate(draft with { Name = "draft" }, "draft", exercises, units).TryGet(out var badge, out _))
        {
            return null;
        }

        var shown = $"{Text(units.DisplayWeight(badge.Target, rounded: false), 1)} {units.WeightLabel}";
        return $"This target is stored as an estimated 1RM of {shown} ({Text(ParseNumber(draft.Target), 1)} {units.WeightLabel} for {draft.Reps.Trim()} reps).";
    }

    /// <summary>The Badge to delete, or the plain reason it cannot be (it is no longer there).</summary>
    public static PlanEditResult<Badge> Delete(IReadOnlyList<Badge> badges, string id) =>
        badges.FirstOrDefault(b => b.Id == id) is { } badge ? new PlanEditResult<Badge>.Valid(badge) : Refuse(Gone);

    /// <summary>
    /// The draft that opens when a stored Badge is edited. A Load Exercise's stored target is an estimated one-rep max, so it opens as
    /// that weight for 1 rep: saved unchanged it keeps the same target.
    /// </summary>
    public static BadgeDraft DraftFor(Badge badge, IReadOnlyList<Exercise> exercises, UnitConverter units)
    {
        var metric = badge.Metric.Trim();
        switch (metric.ToLowerInvariant())
        {
            case "sessions": return Simple(BadgeMetricChoice.Workouts);
            case "streak": return Simple(BadgeMetricChoice.Streak);
            case "bestweek" or "best week": return Simple(BadgeMetricChoice.BestWeek);
            case "prs": return Simple(BadgeMetricChoice.PrCount);
            case "bodyweight":
                return new BadgeDraft(badge.Name, BadgeMetricChoice.BodyWeight, null,
                    Text(units.DisplayWeight(Math.Abs(badge.Target), rounded: false)), "", badge.Target < 0);
        }

        var exercise = exercises.FirstOrDefault(e => e.Id == metric);
        if (exercise is null)
        {
            return new BadgeDraft(badge.Name, BadgeMetricChoice.ExerciseBest, null, Text(Math.Abs(badge.Target)), "", false);
        }

        return UsesWeightAndReps(exercise)
            ? new BadgeDraft(badge.Name, BadgeMetricChoice.ExerciseBest, exercise.Id,
                Text(units.DisplayWeight(SetRanking.WeightForOneRepMax(badge.Target, 1), rounded: false), 3), "1", false)
            : new BadgeDraft(badge.Name, BadgeMetricChoice.ExerciseBest, exercise.Id, Text(Math.Abs(badge.Target)), "", false);

        BadgeDraft Simple(BadgeMetricChoice choice) => new(badge.Name, choice, null, Text(Math.Abs(badge.Target)), "", false);
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
        if (exercise is not null && Unmeasurable(exercise))
        {
            return CannotMeasure(exercise);
        }

        // The name does not affect where a target stands, so a draft without one still gets its line.
        if (!Validate(draft with { Name = "draft" }, "draft", history.Exercises, units).TryGet(out var badge, out _))
        {
            return FillIn;
        }

        var progress = BadgeRules.Progress(badge, history, today);
        var view = TrophyViewBuilder.Build(history.Plan, history.Exercises, [badge], history.Workouts, history.Breaks, history.WeighIns, today, units);
        string unlocks;
        string now;
        if (exercise is not null && UsesWeightAndReps(exercise))
        {
            unlocks = $"you lift {Text(ParseNumber(draft.Target))} × {draft.Reps.Trim()} or better";
            now = ExercisePerformance.Best(exercise, history.Workouts) is { Set: { WeightKg: { } w, Reps: { } r } }
                ? $"{Text(units.DisplayWeight(w))} × {r}"
                : NothingLogged;
        }
        else
        {
            var needs = view.Badges[0].Needs;
            unlocks = char.ToLowerInvariant(needs[0]) + needs[1..];
            now = HasData(draft, exercise, history) ? Text(DisplayValue(draft, progress.Value, units)) : NothingLogged;
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

    /// <summary>False for an Exercise with nothing to measure (text or tick only), which cannot have a Badge.</summary>
    public static bool CanMeasure(Exercise exercise) => exercise.LogKind is not (LogKind.Text or LogKind.TickOnly);

    private static bool Unmeasurable(Exercise exercise) => !CanMeasure(exercise);

    private static string CannotMeasure(Exercise exercise) => $"{exercise.Name} cannot be measured, so it cannot have a Badge.";

    private static PlanEditResult<Badge> Refuse(string reason) => new PlanEditResult<Badge>.Invalid(reason);

    private static bool TryNumber(string? text, out double value)
    {
        value = ParseNumber(text);
        return value > 0 && double.IsFinite(value);
    }

    private static double ParseNumber(string? text) => DecimalText.TryParse(text, out var v) ? v : 0;

    private static string Text(double value, int decimals = 2) =>
        Math.Round(value, decimals).ToString("0." + new string('#', decimals), CultureInfo.InvariantCulture);
}
