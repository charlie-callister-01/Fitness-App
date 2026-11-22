using Core.Model;
using Core.Rules.Home;
using Core.Rules.PlanEditing;
using Core.Rules.Schedule;
using Core.Rules.Units;
using Core.Rules.Weeks;
using Core.Storage;

namespace Core.Rules.Insight;

/// <summary>The Weekly review's verdict. <see cref="OnABreak"/> is for a Break week: it is never called Behind.</summary>
public enum WeeklyVerdict
{
    OnTrack,
    Behind,
    TargetOutOfReach,
    TargetHit,
    OnABreak,
}

/// <summary>A Pain check reported in the week, as one plain line ("Lower back, knee, moderate").</summary>
public sealed record WeeklyPain(DateOnly Date, string Text);

/// <summary>
/// One week's review, prepared. <see cref="Monday"/> identifies the week; <see cref="PreviousMonday"/> and <see cref="NextMonday"/> are the
/// weeks the owner can step to (null at the Plan's first week and at this week). Weights are in the display unit.
/// </summary>
public sealed record WeeklyReviewView(
    DateOnly Monday,
    bool IsCurrentWeek,
    bool HasPlan,
    WeeklyVerdict Verdict,
    string Message,
    int Counted,
    int Target,
    int Streak,
    IReadOnlyList<MissedSession> Missed,
    IReadOnlyList<RecentPr> Prs,
    double? WeightAverage,
    double? LastWeekAverage,
    string UnitLabel,
    IReadOnlyList<WeeklyPain> PainChecks,
    DateOnly? PreviousMonday,
    DateOnly? NextMonday);

/// <summary>
/// The Weekly review (main Track), built from the existing counted-Workout, Streak, consistency, Missed, PR, weight and Break rules.
/// "Today" is passed in. A week after this one is clamped to this one.
/// </summary>
public static class WeeklyReviewBuilder
{
    public static WeeklyReviewView Build(DataSnapshot snapshot, DateOnly weekDate, DateOnly today)
    {
        var thisMonday = WeekCalendar.MondayOf(today);
        var monday = WeekCalendar.MondayOf(weekDate);
        if (monday > thisMonday) monday = thisMonday;
        var current = monday == thisMonday;
        var units = new UnitConverter(snapshot.Settings);

        var (weightAverage, lastWeekAverage) = WeightAverages(snapshot.WeighIns, monday, units);
        var pain = PainChecks(snapshot.Workouts, monday);

        if (ActivePlan.Of(snapshot.Plans) is not { } plan)
        {
            return new WeeklyReviewView(
                monday, current, false, WeeklyVerdict.OnTrack, "", 0, 0, 0, [], [], weightAverage, lastWeekAverage, units.WeightLabel, pain, null, null);
        }

        var counted = WeekRules.CountedWorkouts(plan, snapshot.Workouts, monday);
        var asOf = current ? today : WeekCalendar.SundayOf(monday);
        var streak = WeekRules.Streak(plan, snapshot.Workouts, snapshot.Breaks, asOf).Current;
        var (verdict, message) = Verdict(plan, snapshot, monday, current, counted, today);
        var missed = WeekScheduleRules.MissedInWeek(plan, snapshot.WeekSchedules, snapshot.Breaks, snapshot.Workouts, monday, today);
        var prs = RecentPrs.Latest(snapshot, today, int.MaxValue).Where(p => WeekCalendar.InWeek(monday, p.WorkoutDate)).ToList();

        DateOnly? previous = monday > WeekCalendar.MondayOf(plan.StartDate) ? WeekCalendar.AddWeeks(monday, -1) : null;
        DateOnly? next = current ? null : WeekCalendar.AddWeeks(monday, 1);

        return new WeeklyReviewView(
            monday, current, true, verdict, message, counted, plan.WeeklyTarget, streak, missed, prs,
            weightAverage, lastWeekAverage, units.WeightLabel, pain, previous, next);
    }

    private static (WeeklyVerdict, string) Verdict(
        Plan plan, DataSnapshot snapshot, DateOnly monday, bool current, int counted, DateOnly today)
    {
        if (counted >= plan.WeeklyTarget)
            return (WeeklyVerdict.TargetHit, $"Target hit: {counted} of {plan.WeeklyTarget} done.");
        if (BreakCalendar.TouchesWeek(snapshot.Breaks, monday))
            return (WeeklyVerdict.OnABreak, "On a Break: this week is not held against you.");
        if (!current)
            return (WeeklyVerdict.Behind, $"Behind: {counted} of {plan.WeeklyTarget} done that week.");

        var consistency = WeekRules.Consistency(plan, snapshot.Workouts, snapshot.Breaks, today);
        return (consistency.Kind switch
        {
            ConsistencyKind.TargetOutOfReach => WeeklyVerdict.TargetOutOfReach,
            ConsistencyKind.Behind => WeeklyVerdict.Behind,
            _ => WeeklyVerdict.OnTrack,
        }, consistency.Message);
    }

    private static (double?, double?) WeightAverages(IReadOnlyList<WeighIn> weighIns, DateOnly monday, UnitConverter units)
    {
        double? Average(DateOnly weekMonday)
        {
            return WeightPanelRules.WeekAverageKg(weighIns, weekMonday) is { } kg ? units.DisplayWeight(kg) : null;
        }

        return (Average(monday), Average(WeekCalendar.AddWeeks(monday, -1)));
    }

    private static IReadOnlyList<WeeklyPain> PainChecks(IReadOnlyList<Workout> workouts, DateOnly monday) =>
        workouts
            .Where(w => WeekCalendar.InWeek(monday, w.Date) && w.PainCheck is { Status: PainStatus.Reported })
            .OrderBy(w => w.Date)
            .Select(w => new WeeklyPain(w.Date, PainText(w.PainCheck!)))
            .ToList();

    private static string PainText(PainCheck pain)
    {
        var parts = pain.Areas.Select(Area).Concat(pain.Severity is { } s ? [s.ToString().ToLowerInvariant()] : []).ToList();
        if (parts.Count == 0) return "Pain reported";
        parts[0] = char.ToUpperInvariant(parts[0][0]) + parts[0][1..];
        return string.Join(", ", parts);
    }

    private static string Area(BodyArea area) =>
        string.Concat(area.ToString().Select((c, i) => i > 0 && char.IsUpper(c) ? $" {char.ToLowerInvariant(c)}" : c.ToString())).ToLowerInvariant();
}
