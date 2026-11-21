using System.Globalization;
using Core.Model;
using Core.Rules.Outcomes;
using Core.Rules.Schedule;
using Core.Rules.Weeks;
using Core.Storage;

namespace Core.Rules.Home;

public static class HomeWeekRules
{
    public static IReadOnlyList<WeekTile> Strip(DataSnapshot snapshot, DateOnly today)
    {
        var plan = snapshot.Plans.FirstOrDefault(p => p.IsActive);
        if (plan is null) return [];

        var monday = WeekCalendar.MondayOf(today);
        var layout = WeekScheduleRules.Layout(plan, snapshot.WeekSchedules, snapshot.Breaks, today);
        var missed = WeekScheduleRules.MissedInWeek(plan, snapshot.WeekSchedules, snapshot.Breaks, snapshot.Workouts, today, today);
        var missedIds = missed.Select(m => m.SessionId).ToHashSet();

        var workoutsThisWeek = snapshot.Workouts
            .Where(w => WeekCalendar.InWeek(monday, w.Date))
            .ToList();

        var tiles = new List<WeekTile>(7);
        foreach (var day in WeekCalendar.DaysOf(monday))
        {
            var isToday = day == today;
            var weekday = day.ToString("ddd", CultureInfo.InvariantCulture);
            var scheduled = layout.Sessions.FirstOrDefault(s => s.Date == day);

            if (scheduled is null)
            {
                var isBreak = BreakCalendar.IsBreakDay(snapshot.Breaks, day);
                tiles.Add(new WeekTile(
                    day,
                    weekday,
                    isToday,
                    null,
                    null,
                    null,
                    isBreak ? TileState.BreakDay : TileState.Rest));
                continue;
            }

            var session = scheduled.Session;
            var code = SessionCode(session);
            var type = session.Type;

            var matchingWorkouts = workoutsThisWeek
                .Where(w => w.SessionId == session.Id)
                .ToList();

            TileState state;
            if (matchingWorkouts.Count > 0)
            {
                var outcomes = matchingWorkouts.Select(WorkoutOutcomes.Of).ToList();
                state = outcomes.Any(o => !o.IsPartial) ? TileState.Done : TileState.Partial;
            }
            else if (BreakCalendar.IsBreakDay(snapshot.Breaks, day))
            {
                state = TileState.BreakDay;
            }
            else if (missedIds.Contains(session.Id))
            {
                state = TileState.Missed;
            }
            else if (day >= today)
            {
                state = TileState.Upcoming;
            }
            else
            {
                state = day < plan.StartDate ? TileState.Rest : TileState.Upcoming;
            }

            tiles.Add(new WeekTile(
                day,
                weekday,
                isToday,
                session,
                code,
                type,
                state));
        }

        return tiles;
    }

    public static WeekLine? Line(DataSnapshot snapshot, DateOnly today)
    {
        var plan = snapshot.Plans.FirstOrDefault(p => p.IsActive);
        if (plan is null) return null;

        var monday = WeekCalendar.MondayOf(today);
        var done = WeekRules.CountedWorkouts(plan, snapshot.Workouts, monday);
        var layout = WeekScheduleRules.Layout(plan, snapshot.WeekSchedules, snapshot.Breaks, today);
        var scheduled = layout.Sessions.Count;
        var target = plan.WeeklyTarget;

        var missed = WeekScheduleRules.MissedInWeek(plan, snapshot.WeekSchedules, snapshot.Breaks, snapshot.Workouts, today, today);
        var missedCodes = missed
            .OrderBy(m => m.Date)
            .Select(m => SessionCode(m.SessionName))
            .ToList();

        return new WeekLine(done, scheduled, target, missedCodes);
    }

    public static string SessionCode(Session session) => SessionCode(session.Name);

    public static string SessionCode(string name)
    {
        if (string.IsNullOrEmpty(name)) return string.Empty;

        var colonIndex = name.IndexOf(':');
        if (colonIndex is >= 1 and <= 3)
        {
            return name[..colonIndex];
        }

        return char.ToUpperInvariant(name[0]).ToString();
    }
}
