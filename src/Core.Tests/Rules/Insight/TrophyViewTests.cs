using Core.Model;
using Core.Rules.Insight;
using Core.Rules.Units;
using Core.Tests.Rules.TestData;
using static Core.Tests.Rules.TestData.Dates;
using static Core.Tests.Rules.TestData.HistoryBuilder;

namespace Core.Tests.Rules.Insight;

public class TrophyViewTests
{
    private static readonly DateOnly Start = D(2026, 1, 5);
    private static readonly UnitConverter Kg = new(WeightUnit.Kg, DistanceUnit.Km, 2);
    private static readonly Exercise Squat = new("squat", "Squat", LogKind.Load, null, false);

    private static Plan Plan() => PlanFixture.Make(start: Start, sessions: [PlanFixture.Session("lower", DayOfWeek.Monday, name: "Lower")]);

    private static Workout Day(int day) => Lift($"w{day}", Start.AddDays(day), "lower", "squat", Set(kg: 50, reps: 5));

    private static TrophyView Build(IReadOnlyList<Badge> badges, IReadOnlyList<Workout>? workouts = null, IReadOnlyList<WeighIn>? weighIns = null) =>
        TrophyViewBuilder.Build(Plan(), [Squat], badges, workouts ?? [], [], weighIns ?? [], D(2026, 2, 1), Kg);

    private static Badge Sessions(string id, string name, int target) => new(id, name, "sessions", target);

    [Fact]
    public void Unlocked_come_first_then_closest_to_unlocking()
    {
        var view = Build([Sessions("far", "Far", 10), Sessions("done", "Done", 1), Sessions("near", "Near", 4)],
            [Day(0), Day(1), Day(2)]);

        Assert.Equal(["done", "near", "far"], view.Badges.Select(b => b.Id));
        Assert.Equal([BadgeStanding.Unlocked, BadgeStanding.InProgress, BadgeStanding.InProgress], view.Badges.Select(b => b.Standing));
        Assert.Equal("3 / 4", view.Badges[1].ProgressText);
    }

    [Fact]
    public void Ties_are_broken_by_name()
    {
        var view = Build([Sessions("b", "Bravo", 4), Sessions("a", "Alpha", 4), Sessions("z", "Zed", 1)], [Day(0), Day(1)]);

        Assert.Equal(["z", "a", "b"], view.Badges.Select(b => b.Id));
    }

    [Fact]
    public void None_unlocked_counts_zero_and_nothing_measured_is_locked()
    {
        var view = Build([Sessions("a", "A", 5), Sessions("b", "B", 10)]);

        Assert.Equal(0, view.UnlockedCount);
        Assert.Equal(2, view.TotalCount);
        Assert.All(view.Badges, b => Assert.Equal(BadgeStanding.Locked, b.Standing));
        Assert.False(view.HasHistory);
        Assert.Empty(view.Closest);
    }

    [Fact]
    public void All_unlocked_counts_every_Badge_and_has_no_closest()
    {
        var view = Build([Sessions("a", "A", 1), Sessions("b", "B", 2)], [Day(0), Day(1)]);

        Assert.Equal(2, view.UnlockedCount);
        Assert.Equal(2, view.TotalCount);
        Assert.Empty(view.Closest);
        Assert.True(view.HasHistory);
    }

    [Fact]
    public void Closest_is_the_first_three_in_progress_in_order()
    {
        var view = Build(
            [Sessions("a", "A", 20), Sessions("b", "B", 10), Sessions("c", "C", 5), Sessions("d", "D", 3), Sessions("e", "E", 100)],
            [Day(0), Day(1)]);

        Assert.Equal(["d", "c", "b"], view.Closest.Select(b => b.Id));
    }

    [Fact]
    public void A_drop_target_reads_as_how_far_it_has_fallen()
    {
        var drop = new Badge("drop", "Drop 5 kg", "bodyweight", -5);

        var view = Build([drop], null, [new WeighIn("a", D(2026, 1, 5), 90), new WeighIn("b", D(2026, 1, 20), 87)]);

        var badge = view.Badges.Single();
        Assert.Equal("3 / 5", badge.ProgressText);
        Assert.Equal(60, badge.Percent);
        Assert.Equal(BadgeStanding.InProgress, badge.Standing);
        Assert.Equal("Drop 5 kg from your first Weigh-in", badge.Needs);
    }

    [Fact]
    public void A_metric_that_cannot_be_measured_says_so_and_sorts_last()
    {
        var view = Build([new Badge("odd", "Odd", "nonsense", 3), Sessions("a", "A", 50)], [Day(0)]);

        Assert.Equal(["a", "odd"], view.Badges.Select(b => b.Id));
        var odd = view.Badges[1];
        Assert.Equal(BadgeStanding.NotMeasurable, odd.Standing);
        Assert.Equal("not measurable", odd.ProgressText);
        Assert.Empty(odd.Events);
        Assert.DoesNotContain(odd, view.Closest);
    }

    [Fact]
    public void Sessions_events_are_the_counted_Workouts_newest_first_and_leave_out_an_uncounted_Custom_workout()
    {
        var custom = Logged(Start.AddDays(5), null) with { Entries = [new WorkoutEntry("squat", EntryStatus.Done, null, null, null, [])] };

        var badge = Build([Sessions("s", "S", 10)], [Day(0), Day(2), custom]).Badges.Single();

        Assert.Equal(
            [Start.AddDays(2), Start]
            , badge.Events.Select(e => e.Date));
        Assert.Equal("Workout: Lower", badge.Events[1].Text);
    }

    [Fact]
    public void Weigh_in_events_list_each_Weigh_in()
    {
        var badge = Build([new Badge("w", "W", "bodyweight", 80)], null,
            [new WeighIn("a", D(2026, 1, 5), 90), new WeighIn("b", D(2026, 1, 20), 88.5)]).Badges.Single();

        Assert.Equal(["Weigh-in: 88.5 kg", "Weigh-in: 90 kg"], badge.Events.Select(e => e.Text));
    }

    [Fact]
    public void Prs_events_are_the_dates_PRs_were_set()
    {
        var w1 = Lift("w1", D(2026, 1, 5), "lower", "squat", Set(kg: 50, reps: 5));
        var w2 = Lift("w2", D(2026, 1, 12), "lower", "squat", Set(kg: 60, reps: 5));

        var badge = Build([new Badge("p", "P", "prs", 10)], [w1, w2]).Badges.Single();

        Assert.Equal([D(2026, 1, 12)], badge.Events.Select(e => e.Date));
        Assert.Equal("PR: Squat", badge.Events[0].Text);
    }

    [Fact]
    public void Best_week_events_are_the_Workouts_of_the_busiest_week()
    {
        var badge = Build([new Badge("b", "B", "bestweek", 5)], [Day(0), Day(2), Day(8)]).Badges.Single();

        Assert.Equal([Start.AddDays(2), Start], badge.Events.Select(e => e.Date));
    }

    [Fact]
    public void Art_maps_known_targets_and_falls_back_to_the_frame()
    {
        Assert.Equal("badge-workouts-10", BadgeArt.Stem("sessions", 10));
        Assert.Equal("badge-streak-4", BadgeArt.Stem("Streak", 4));
        Assert.Equal("badge-prs-25", BadgeArt.Stem("prs", 25));
        Assert.Equal("badge-best-week", BadgeArt.Stem("best week", 5));
        Assert.Equal("badge-weight-goal", BadgeArt.Stem("bodyweight", -5));
        Assert.Equal(BadgeArt.Frame, BadgeArt.Stem("sessions", 7));
        Assert.Equal(BadgeArt.Frame, BadgeArt.Stem("squat", 100));
    }
}
