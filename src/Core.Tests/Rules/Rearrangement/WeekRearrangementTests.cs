using Core.Model;
using Core.Rules.Rearrangement;

namespace Core.Tests.Rules.Rearrangement;

public class WeekRearrangementTests
{
    private static DateOnly D(int y, int m, int d) => new(y, m, d);

    // Week of Mon 2026-11-16. Plan order: legsA (Mon), push (Tue), legsB (Wed), pull (Thu), run (Fri).
    // legsA and legsB share the glutes tag.
    private static readonly DateOnly Mon = D(2026, 11, 16);
    private static readonly DateOnly Start = D(2026, 1, 5);

    private static Exercise Ex(string id, params string[] groups) =>
        new(id, id, LogKind.Load, new ExerciseTags(groups, [], []), false);

    private static readonly IReadOnlyList<Exercise> Library =
    [
        Ex("squat", "quads", "glutes"), Ex("rdl", "hamstrings", "glutes"), Ex("bench", "chest"),
        Ex("row", "back"), new("jog", "jog", LogKind.DistanceKm, null, false),
    ];

    private static Session S(string id, DayOfWeek day, string exercise) =>
        new(id, id, day, SessionType.Strength, null, [new SessionExercise(exercise, 3, 5, null, null)]);

    private static Plan Plan(params Session[] sessions) => new(
        "p", "Plan", true, Start, null, null, 3, 2, false, 2, [], sessions);

    private static readonly Plan Standard = Plan(
        S("legsA", DayOfWeek.Monday, "squat"), S("push", DayOfWeek.Tuesday, "bench"),
        S("legsB", DayOfWeek.Wednesday, "rdl"), S("pull", DayOfWeek.Thursday, "row"),
        S("run", DayOfWeek.Friday, "jog"));

    private static Workout Made(DateOnly date, string sessionId) =>
        new($"w{date.DayNumber}{sessionId}", date, sessionId, false, null, null, [], [], null);

    private static Break Brk(DateOnly from, DateOnly to) => new("b", from, to, "x");

    private static RearrangementResult Suggest(
        Plan plan, DateOnly today, IReadOnlyList<Break>? breaks = null, IReadOnlyList<Workout>? workouts = null,
        IReadOnlyList<WeekSchedule>? schedules = null) =>
        WeekRearrangement.Suggest(plan, Library, schedules ?? [], breaks ?? [], workouts ?? [], today);

    private static Dictionary<string, DateOnly> Placed(RearrangementResult r) =>
        Assert.IsType<RearrangementResult.Proposal>(r).Placements.ToDictionary(p => p.SessionId, p => p.Date);

    [Fact]
    public void A_week_that_is_on_track_has_nothing_to_rearrange()
    {
        var done = new[] { Made(Mon, "legsA"), Made(Mon.AddDays(1), "push") };
        Assert.IsType<RearrangementResult.NothingToRearrange>(Suggest(Standard, Mon.AddDays(2), workouts: done));
    }

    [Fact]
    public void Nothing_outstanding_means_nothing_to_rearrange()
    {
        var all = Standard.Sessions.Select((s, i) => Made(Mon.AddDays(i), s.Id)).ToList();
        Assert.IsType<RearrangementResult.NothingToRearrange>(Suggest(Standard, Mon.AddDays(4), workouts: all));
    }

    [Fact]
    public void Sessions_lost_to_an_earlier_break_are_not_outstanding()
    {
        // Break Mon-Wed, today Thursday: the first three are not Missed; pull and run still sit on their days.
        Assert.IsType<RearrangementResult.NothingToRearrange>(Suggest(Standard, Mon.AddDays(3), [Brk(Mon, Mon.AddDays(2))]));
    }

    [Fact]
    public void A_break_pile_up_moves_sessions_off_break_days_onto_coming_days_in_plan_order()
    {
        // Break Wed-Thu, today Tuesday, Monday done: legsB and pull sit on Break days.
        var p = Placed(Suggest(Standard, Mon.AddDays(1), [Brk(Mon.AddDays(2), Mon.AddDays(3))], [Made(Mon, "legsA")]));
        Assert.Equal(Mon.AddDays(1), p["push"]);
        Assert.Equal(Mon.AddDays(4), p["legsB"]);
        Assert.Equal(Mon.AddDays(5), p["pull"]);
        Assert.Equal(Mon.AddDays(6), p["run"]);
        Assert.Equal(4, p.Count);
    }

    [Fact]
    public void Missed_sessions_are_pulled_onto_coming_days_in_plan_order_ordered_by_date()
    {
        // Today Wednesday, nothing done: legsA and push missed, legsB today, pull and run to come.
        var r = Assert.IsType<RearrangementResult.Proposal>(Suggest(Standard, Mon.AddDays(2)));
        Assert.Equal(["legsA", "push", "legsB", "pull", "run"], r.Placements.Select(p => p.SessionId));
        Assert.Equal([2, 3, 4, 5, 6], r.Placements.Select(p => p.Date.DayNumber - Mon.DayNumber));
        Assert.Empty(r.Unplaced);
    }

    [Fact]
    public void Back_to_back_days_with_a_shared_muscle_group_are_avoided()
    {
        var plan = Plan(S("legsA", DayOfWeek.Monday, "squat"), S("legsB", DayOfWeek.Tuesday, "rdl"), S("push", DayOfWeek.Wednesday, "bench"));
        var p = Placed(Suggest(plan, Mon.AddDays(3)));
        Assert.True(Math.Abs(p["legsA"].DayNumber - p["legsB"].DayNumber) >= 2);
        Assert.Equal(3, p.Values.Distinct().Count());
    }

    [Fact]
    public void A_clashing_pair_is_still_placed_on_consecutive_days_when_there_is_no_alternative()
    {
        var plan = Plan(S("legsA", DayOfWeek.Monday, "squat"), S("legsB", DayOfWeek.Tuesday, "rdl"));
        var p = Placed(Suggest(plan, Mon.AddDays(5)));
        Assert.Equal(Mon.AddDays(5), p["legsA"]);
        Assert.Equal(Mon.AddDays(6), p["legsB"]);
    }

    [Fact]
    public void Sessions_are_not_doubled_up_while_a_free_day_remains()
    {
        var p = Placed(Suggest(Standard, Mon.AddDays(3)));
        Assert.Equal(p.Count, p.Values.Distinct().Count() + 1); // five Sessions, four days: exactly one double
    }

    [Fact]
    public void Sessions_double_up_only_when_there_are_more_sessions_than_coming_days()
    {
        var p = Placed(Suggest(Standard, Mon.AddDays(5)));
        Assert.Equal(5, p.Count);
        Assert.All(p.Values, d => Assert.True(d == Mon.AddDays(5) || d == Mon.AddDays(6)));
        Assert.True(p.Values.Count(d => d == Mon.AddDays(5)) >= 2);
        Assert.True(p.Values.Count(d => d == Mon.AddDays(6)) >= 2);
    }

    [Fact]
    public void An_exercise_without_tags_never_clashes()
    {
        var plan = Plan(S("runA", DayOfWeek.Monday, "jog"), S("runB", DayOfWeek.Tuesday, "jog"));
        var p = Placed(Suggest(plan, Mon.AddDays(3)));
        Assert.Equal(Mon.AddDays(3), p["runA"]);
        Assert.Equal(Mon.AddDays(4), p["runB"]);
    }

    [Fact]
    public void Break_days_are_never_used_and_a_week_with_no_free_days_leaves_everything_unplaced()
    {
        var p = Placed(Suggest(Standard, Mon.AddDays(3), [Brk(Mon.AddDays(4), Mon.AddDays(5))]));
        Assert.DoesNotContain(Mon.AddDays(4), p.Values);
        Assert.DoesNotContain(Mon.AddDays(5), p.Values);

        var none = Assert.IsType<RearrangementResult.Proposal>(Suggest(Standard, Mon.AddDays(3), [Brk(Mon.AddDays(3), Mon.AddDays(6))]));
        Assert.Empty(none.Placements);
        Assert.Equal(["legsA", "push", "legsB", "pull", "run"], none.Unplaced);
    }

    [Fact]
    public void Suggesting_changes_nothing_and_is_repeatable()
    {
        var schedules = new List<WeekSchedule>();
        var a = Suggest(Standard, Mon.AddDays(3), schedules: schedules);
        var b = Suggest(Standard, Mon.AddDays(3), schedules: schedules);
        Assert.Empty(schedules);
        Assert.Equal(Placed(a), Placed(b));
    }
}
