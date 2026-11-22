using System.Globalization;
using Core.Ids;
using Core.Model;
using Core.Rules.Formatting;
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

    /// <summary>
    /// The Badge a draft describes, or why not, without needing a name: the name does not affect where a target stands, so the editor's
    /// live lines work before the owner has typed one.
    /// </summary>
    public static PlanEditResult<Badge> ValidateIgnoringName(BadgeDraft draft, IReadOnlyList<Exercise> exercises, UnitConverter units) =>
        Validate(draft with { Name = "Draft" }, "draft", exercises, units);

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
                    DisplayFormat.Rounded(units.DisplayWeight(Math.Abs(badge.Target), rounded: false)), "", badge.Target < 0);
        }

        var exercise = exercises.FirstOrDefault(e => e.Id == metric);
        if (exercise is null)
        {
            return new BadgeDraft(badge.Name, BadgeMetricChoice.ExerciseBest, null, DisplayFormat.Rounded(Math.Abs(badge.Target)), "", false);
        }

        return UsesWeightAndReps(exercise)
            ? new BadgeDraft(badge.Name, BadgeMetricChoice.ExerciseBest, exercise.Id,
                DisplayFormat.Rounded(units.DisplayWeight(SetRanking.WeightForOneRepMax(badge.Target, 1), rounded: false), 3), "1", false)
            : new BadgeDraft(badge.Name, BadgeMetricChoice.ExerciseBest, exercise.Id, DisplayFormat.Rounded(Math.Abs(badge.Target)), "", false);

        BadgeDraft Simple(BadgeMetricChoice choice) => new(badge.Name, choice, null, DisplayFormat.Rounded(Math.Abs(badge.Target)), "", false);
    }

    /// <summary>False for an Exercise with nothing to measure (text or tick only), which cannot have a Badge.</summary>
    public static bool CanMeasure(Exercise exercise) => exercise.LogKind is not (LogKind.Text or LogKind.TickOnly);

    internal static bool Unmeasurable(Exercise exercise) => !CanMeasure(exercise);

    internal static string CannotMeasure(Exercise exercise) => $"{exercise.Name} cannot be measured, so it cannot have a Badge.";

    private static PlanEditResult<Badge> Refuse(string reason) => new PlanEditResult<Badge>.Invalid(reason);

    private static bool TryNumber(string? text, out double value)
    {
        value = DecimalText.ParseOrZero(text);
        return value > 0 && double.IsFinite(value);
    }
}

