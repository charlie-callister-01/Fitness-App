using Core.Rules.Formatting;
using System.Globalization;
using Core.Model;
using Core.Rules.Badges;
using Core.Rules.Performance;
using Core.Rules.Units;
using Core.Rules.Weeks;

namespace Core.Rules.Insight;

/// <summary>Where a Badge stands for the Trophies screen. NotMeasurable is a Badge whose metric names nothing the app can measure.</summary>
public enum BadgeStanding
{
    Unlocked,
    InProgress,
    Locked,
    NotMeasurable,
}

/// <summary>One event that contributed to a Badge: a Workout that raised a count, a PR, a Weigh-in.</summary>
public sealed record BadgeEvent(DateOnly Date, string Text);

/// <summary>
/// One Badge as the Trophies screen draws it. <see cref="ProgressText"/> is "x / y" (or "not measurable"); <see cref="Needs"/> says in plain
/// words what the Badge asks for; <see cref="ArtStem"/> is the starter art file name without extension or "-locked" ("badge-frame-template" for a custom Badge).
/// </summary>
public sealed record TrophyBadge(
    string Id,
    string Name,
    BadgeStanding Standing,
    string ProgressText,
    int Percent,
    string Needs,
    string ArtStem,
    IReadOnlyList<BadgeEvent> Events)
{
    public bool IsMeasurable => Standing != BadgeStanding.NotMeasurable;
}

/// <summary>
/// The Trophies Badges tab. <see cref="Badges"/> are ordered: unlocked first, then closest to unlocking first, ties by name, Badges
/// that cannot be measured last. <see cref="Closest"/> are the few in progress that get a ProgressBar.
/// </summary>
public sealed record TrophyView(
    IReadOnlyList<TrophyBadge> Badges,
    int UnlockedCount,
    IReadOnlyList<TrophyBadge> Closest,
    bool HasHistory)
{
    public int TotalCount => Badges.Count;
}

public static class TrophyViewBuilder
{
    /// <summary>How many in-progress Badges get a ProgressBar.</summary>
    public const int ClosestCount = 3;

    public static TrophyView Build(
        Plan? plan,
        IReadOnlyList<Exercise> exercises,
        IReadOnlyList<Badge> badges,
        IReadOnlyList<Workout> workouts,
        IReadOnlyList<Break> breaks,
        IReadOnlyList<WeighIn> weighIns,
        DateOnly today,
        UnitConverter units)
    {
        plan ??= new Plan("none", "No Plan", false, today, null, null, 2.5, [new Track("main", "Main", new TrackRhythm.Weekly(0, 1, false), [])], []);
        var history = new BadgeHistory(plan, exercises, workouts, breaks, weighIns);
        var all = badges.Select(b => Make(b, history, today, units)).ToList();

        var ordered = all
            .OrderBy(b => b.Standing == BadgeStanding.Unlocked ? 0 : b.IsMeasurable ? 1 : 2)
            .ThenByDescending(b => b.Standing == BadgeStanding.Unlocked ? 0 : b.Percent)
            .ThenBy(b => b.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(b => b.Id, StringComparer.Ordinal)
            .ToList();

        var closest = ordered.Where(b => b.Standing == BadgeStanding.InProgress).Take(ClosestCount).ToList();
        return new TrophyView(ordered, ordered.Count(b => b.Standing == BadgeStanding.Unlocked), closest,
            workouts.Count > 0 || weighIns.Count > 0);
    }

    private static TrophyBadge Make(Badge badge, BadgeHistory h, DateOnly today, UnitConverter units)
    {
        var progress = BadgeRules.Progress(badge, h, today);
        var metric = badge.Metric.Trim();
        var isBody = metric.Equals("bodyweight", StringComparison.OrdinalIgnoreCase);
        var standing = !progress.IsMeasurable ? BadgeStanding.NotMeasurable
            : progress.Reached ? BadgeStanding.Unlocked
            : progress.Percent > 0 ? BadgeStanding.InProgress
            : BadgeStanding.Locked;

        string Num(double v) => DisplayFormat.Trimmed(isBody ? units.DisplayWeight(v) : v);
        var text = progress.IsMeasurable ? $"{Num(progress.Value)} / {Num(Math.Abs(badge.Target))}" : "not measurable";
        var events = progress.IsMeasurable ? BadgeText.Events(metric, h, units) : [];
        return new TrophyBadge(badge.Id, badge.Name, standing, text, progress.Percent,
            BadgeText.Needs(badge, metric, h, units), BadgeArt.Stem(metric, badge.Target), events);
    }
}

/// <summary>Which starter art a Badge wears: a known metric and target maps to its art, anything else to the custom frame.</summary>
public static class BadgeArt
{
    public const string Frame = "badge-frame-template";

    public static string Stem(string metric, double target)
    {
        var n = (int)target;
        if (target != n) return Frame;
        return metric.ToLowerInvariant() switch
        {
            "sessions" when n is 1 or 10 or 25 or 50 or 100 => $"badge-workouts-{n}",
            "streak" when n is 2 or 4 or 8 or 12 => $"badge-streak-{n}",
            "prs" when n is 10 or 25 or 50 => $"badge-prs-{n}",
            "bestweek" or "best week" => "badge-best-week",
            "bodyweight" => "badge-weight-goal",
            _ => Frame,
        };
    }

    /// <summary>True when a "-locked" variant of the art exists (the custom frame has none).</summary>
    public static bool HasLockedVariant(string stem) => stem != Frame;
}
