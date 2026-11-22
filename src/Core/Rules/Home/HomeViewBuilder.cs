using Core.Rules.PlanEditing;
using Core.Rules.Nudges;
using Core.Rules.Phases;
using Core.Storage;

namespace Core.Rules.Home;

/// <summary>Composes the Home rules into one <see cref="HomeView"/>. Pure: the clock, time zone and dismissals are arguments.</summary>
public static class HomeViewBuilder
{
    public static HomeView Build(
        DataSnapshot snapshot,
        DateTimeOffset now,
        DateOnly today,
        int localHour,
        IReadOnlySet<string> dismissedNudgeKeys,
        DateTimeOffset? lastBackup)
    {
        var plan = ActivePlan.Of(snapshot.Plans);
        var card = HomeTodayRules.Compute(snapshot, now, today);

        IReadOnlyList<Nudge> computed = plan is null
            ? []
            : NudgeRules.Compute(plan, snapshot.WeekSchedules, snapshot.Breaks, snapshot.Workouts, lastBackup, today);

        var hasAlternatives = plan is not null
            && card.Kind is not (TodayKind.Break or TodayKind.InProgress or TodayKind.NoPlan or TodayKind.PlanFinished or TodayKind.NotStarted);
        var showNudges = card.Kind is not (TodayKind.Break or TodayKind.NoPlan);

        return new HomeView(
            new HomeHeader(today, PhaseLabel(plan, today), Greeting(localHour)),
            card,
            HomeTodayRules.UndoneSessions(snapshot, today),
            HomeWeekRules.Strip(snapshot, today),
            HomeWeekRules.Line(snapshot, today),
            HomeOrbs.Compute(snapshot, today),
            HomeNudges.Select(computed, dismissedNudgeKeys, today),
            HomeNudges.KeysFor(computed, today),
            RecentPrs.Latest(snapshot, today),
            WeightPanelRules.Compute(snapshot.WeighIns, today),
            hasAlternatives,
            hasAlternatives,
            showNudges);
    }

    private static string Greeting(int localHour) => localHour switch
    {
        < 12 => "Good morning",
        < 18 => "Good afternoon",
        _ => "Good evening",
    };

    private static string PhaseLabel(Core.Model.Plan? plan, DateOnly today)
    {
        if (plan is null) return string.Empty;
        var position = PhaseRules.Position(plan, today);
        return position is { State: PhaseState.InPhase, Phase: { } phase, WeekInPhase: { } week, PhaseLengthWeeks: { } length }
            ? $"{phase.Focus} week {week} of {length}"
            : string.Empty;
    }
}
