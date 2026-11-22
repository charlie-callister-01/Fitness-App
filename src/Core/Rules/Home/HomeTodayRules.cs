using Core.Rules.PlanEditing;
using Core.Model;
using Core.Rules.Badges;
using Core.Rules.Outcomes;
using Core.Rules.Phases;
using Core.Rules.Schedule;
using Core.Rules.Starting;
using Core.Rules.Summary;
using Core.Rules.Tracks;
using Core.Rules.Weeks;
using Core.Storage;

namespace Core.Rules.Home;

/// <summary>Decides what Home's TodayCard shows and lists undone Sessions for the week. Pure rules, no clock.</summary>
public static class HomeTodayRules
{
    public static TodayCard Compute(DataSnapshot snapshot, DateTimeOffset now, DateOnly today)
    {
        var plan = ActivePlan.Of(snapshot.Plans);
        if (plan is null)
            return TodayCard.NoPlan();

        if (BreakCalendar.IsBreakDay(snapshot.Breaks, today))
            return TodayCard.Break();

        var inProgress = WorkoutStart.InProgress(snapshot.Workouts);
        if (inProgress is not null)
        {
            var outcome = WorkoutOutcomes.Of(inProgress);
            var doneCount = outcome.Rows.Count(r => r.Kind is RowOutcomeKind.Done or RowOutcomeKind.Swapped);
            var plannedCount = inProgress.Planned.Count;
            var elapsed = inProgress is { LoggedAfterwards: false, StartedAt: { } startedAt } ? now - startedAt : (TimeSpan?)null;
            return TodayCard.InProgress(inProgress, doneCount, plannedCount, elapsed);
        }

        var finishedToday = snapshot.Workouts
            .Where(w => w.Date == today && w.EndedAt is not null)
            .OrderByDescending(w => w.EndedAt)
            .FirstOrDefault();
        if (finishedToday is not null)
        {
            var history = BadgeHistories.For(snapshot, finishedToday);
            var summary = WorkoutSummaryBuilder.Build(finishedToday, history, snapshot.Badges, today);
            return TodayCard.DoneToday(new TodayDoneSummary(
                finishedToday.Id,
                summary.Duration,
                summary.SetCount,
                summary.PrCount,
                summary.IsPartial));
        }

        var position = PhaseRules.Position(plan, TrackRules.Main(plan), today);
        if (position.State == PhaseState.AfterEnd)
            return TodayCard.PlanFinished();

        if (position.State == PhaseState.BeforeStart)
            return TodayCard.NotStarted(plan.StartDate);

        var main = TrackRules.Main(plan);
        var layout = WeekScheduleRules.Layout(plan, main, snapshot.WeekSchedules, snapshot.Breaks, today);
        var doneThisWeek = snapshot.Workouts
            .Where(w => WeekCalendar.InWeek(layout.Monday, w.Date))
            .Select(w => w.SessionId)
            .OfType<string>()
            .ToHashSet();

        var dueToday = layout.Sessions
            .FirstOrDefault(s => s.Date == today && !doneThisWeek.Contains(s.SessionId));
        if (dueToday is not null)
            return TodayCard.Start(dueToday.Session);

        var nextThisWeek = layout.Sessions
            .FirstOrDefault(s => s.Date > today && !doneThisWeek.Contains(s.SessionId));
        if (nextThisWeek is not null)
            return TodayCard.RestDay(nextThisWeek.Session, nextThisWeek.Date);

        var nextMonday = WeekCalendar.AddWeeks(layout.Monday, 1);
        var nextLayout = WeekScheduleRules.Layout(plan, main, snapshot.WeekSchedules, snapshot.Breaks, nextMonday);
        var nextScheduled = nextLayout.Sessions.FirstOrDefault();
        return TodayCard.RestDay(nextScheduled?.Session, nextScheduled?.Date);
    }

    public static IReadOnlyList<ScheduledSession> UndoneSessions(DataSnapshot snapshot, DateOnly today)
    {
        var plan = ActivePlan.Of(snapshot.Plans);
        if (plan is null)
            return [];

        var layout = WeekScheduleRules.Layout(plan, TrackRules.Main(plan), snapshot.WeekSchedules, snapshot.Breaks, today);
        var doneThisWeek = snapshot.Workouts
            .Where(w => WeekCalendar.InWeek(layout.Monday, w.Date))
            .Select(w => w.SessionId)
            .OfType<string>()
            .ToHashSet();

        return layout.Sessions
            .Where(s => !doneThisWeek.Contains(s.SessionId))
            .ToList();
    }
}
