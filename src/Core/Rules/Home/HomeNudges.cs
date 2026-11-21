using Core.Model;
using Core.Rules.Nudges;
using Core.Rules.Weeks;

namespace Core.Rules.Home;

public enum NudgeAction
{
    None,
    MakeUp,
    DifferentSession,
    BackUp,
}

public sealed record HomeNudge(Nudge Nudge, string Key, NudgeAction Action);

public static class HomeNudges
{
    public static IReadOnlyList<HomeNudge> Select(
        IReadOnlyList<Nudge> computed,
        IReadOnlySet<string> dismissedKeys,
        DateOnly today)
    {
        var result = new List<HomeNudge>();
        foreach (var nudge in computed)
        {
            var key = KeyFor(nudge, today);
            if (dismissedKeys.Contains(key)) continue;

            result.Add(new HomeNudge(nudge, key, ActionFor(nudge.Kind)));
            if (result.Count == 2) break;
        }

        return result;
    }

    public static IReadOnlyList<string> KeysFor(
        IReadOnlyList<Nudge> computed,
        DateOnly today) =>
        computed.Select(n => KeyFor(n, today)).ToList();

    public static string KeyFor(Nudge nudge, DateOnly today)
    {
        var monday = WeekCalendar.MondayOf(today);
        var subject = nudge.SessionId ?? (nudge.Area?.ToString() ?? "");
        return $"{monday:yyyy-MM-dd}|{nudge.Kind}|{subject}";
    }

    private static NudgeAction ActionFor(NudgeKind kind) => kind switch
    {
        NudgeKind.MissedSession => NudgeAction.MakeUp,
        NudgeKind.OneShortOfTarget => NudgeAction.DifferentSession,
        NudgeKind.NoRecentBackup => NudgeAction.BackUp,
        _ => NudgeAction.None,
    };
}
