using Core.Model;
using Core.Rules.Weeks;

namespace Core.Rules.Schedule;

/// <summary>When a benchmark Session is scheduled. Reusable by Benchmarks (ticket 12).</summary>
public static class BenchmarkWeeks
{
    /// <summary>True for a Test Session with a positive interval, i.e. a benchmark Session. A missing or zero interval is not a benchmark.</summary>
    public static bool IsBenchmark(Session session) =>
        session is { Type: SessionType.Test, BenchmarkIntervalWeeks: > 0 };

    /// <summary>
    /// Whether the Session is scheduled in the week starting on <paramref name="monday"/>. A benchmark Session
    /// is scheduled in Plan weeks 1, 1+N, 1+2N and so on (a baseline in the first week) and never before the
    /// Plan starts. Any other Session is always scheduled.
    /// </summary>
    public static bool IsScheduledInWeek(Plan plan, Session session, DateOnly monday)
    {
        if (!IsBenchmark(session)) return true;
        var week = WeekRules.PlanWeekNumber(plan, WeekCalendar.SundayOf(monday));
        return week is { } n && (n - 1) % session.BenchmarkIntervalWeeks!.Value == 0;
    }
}
