using Core.Model;
using Core.Rules.Insight;
using Core.Storage;
using Core.Tests.Rules.TestData;
using static Core.Tests.Rules.TestData.HistoryBuilder;

namespace Core.Tests.Rules.Insight;

public class WeeklyReviewViewTests
{
    // Plan starts Mon 5 Jan 2026, target 3 a week, Streak minimum 2, Custom workouts count. "a-lower" (Lower) is on Mondays.
    private static readonly DateOnly Start = new(2026, 1, 5);
    private static readonly DateOnly Wed = new(2026, 1, 7);
    private static readonly Exercise Squat = new("squat", "Squat", LogKind.Load, null, false);
    private static readonly Plan Plan = PlanFixture.Make(Start, customCounts: true);

    private static Workout Done(string id, DateOnly date, string? session = null, PainCheck? pain = null, params WorkoutSet[] sets) =>
        new(id, date, session, false, null, new DateTimeOffset(date.ToDateTime(new TimeOnly(10, 0)), TimeSpan.Zero),
            [new SessionExercise("squat", 1, 5, null, null)],
            [new WorkoutEntry("squat", EntryStatus.Done, null, null, null, sets.Length == 0 ? [Set(kg: 20, reps: 5)] : sets)], pain);

    private static WeeklyReviewView Build(DateOnly monday, DateOnly today, IReadOnlyList<Workout>? workouts = null,
        IReadOnlyList<Break>? breaks = null, IReadOnlyList<WeighIn>? weighIns = null, Settings? settings = null) =>
        WeeklyReviewBuilder.Build(
            new DataSnapshot
            {
                Plans = [Plan], Exercises = [Squat], Workouts = workouts ?? [], Breaks = breaks ?? [], WeighIns = weighIns ?? [],
                Settings = settings ?? Settings.Default,
            },
            monday, today);

    private static Workout[] Days(params int[] offsets) =>
        offsets.Select((o, i) => Done($"w{i}", Start.AddDays(o), i == 0 ? "a-lower" : null)).ToArray();

    [Fact]
    public void Early_in_the_week_with_time_to_spare_is_On_track()
    {
        var view = Build(Start, Wed, Days(0));

        Assert.Equal(WeeklyVerdict.OnTrack, view.Verdict);
        Assert.Equal(1, view.Counted);
        Assert.Equal(3, view.Target);
        Assert.True(view.IsCurrentWeek);
        Assert.StartsWith("On track", view.Message);
    }

    [Fact]
    public void No_spare_day_is_Behind()
    {
        // Fri: 3 to go, Fri-Sat-Sun available.
        Assert.Equal(WeeklyVerdict.Behind, Build(Start, Start.AddDays(4)).Verdict);
    }

    [Fact]
    public void More_to_go_than_days_left_is_Target_out_of_reach()
    {
        Assert.Equal(WeeklyVerdict.TargetOutOfReach, Build(Start, Start.AddDays(5)).Verdict);
    }

    [Fact]
    public void Reaching_the_target_is_Target_hit()
    {
        var view = Build(Start, Wed, Days(0, 1, 2));

        Assert.Equal(WeeklyVerdict.TargetHit, view.Verdict);
        Assert.Equal(3, view.Counted);
    }

    [Fact]
    public void A_Break_week_is_On_a_Break_and_never_Behind()
    {
        var view = Build(Start, Start.AddDays(5), breaks: [new Break("b", Start.AddDays(3), Start.AddDays(6), "Trip")]);

        Assert.Equal(WeeklyVerdict.OnABreak, view.Verdict);
        Assert.Contains("On a Break", view.Message);
    }

    [Fact]
    public void A_past_Break_week_is_On_a_Break_too()
    {
        var view = Build(Start, Start.AddDays(9), breaks: [new Break("b", Start.AddDays(3), Start.AddDays(6), "Trip")]);

        Assert.Equal(WeeklyVerdict.OnABreak, view.Verdict);
    }

    [Fact]
    public void In_a_Break_week_one_short_of_the_target_is_On_a_Break_and_reaching_it_is_Target_hit()
    {
        var breaks = new[] { new Break("b", Start.AddDays(4), Start.AddDays(6), "Trip") };

        Assert.Equal(WeeklyVerdict.OnABreak, Build(Start, Wed, Days(0, 1), breaks: breaks).Verdict);
        Assert.Equal(WeeklyVerdict.TargetHit, Build(Start, Wed, Days(0, 1, 2), breaks: breaks).Verdict);
    }

    [Fact]
    public void Target_hit_wins_over_a_Break()
    {
        var view = Build(Start, Wed, Days(0, 1, 2), breaks: [new Break("b", Start.AddDays(4), Start.AddDays(6), "Trip")]);

        Assert.Equal(WeeklyVerdict.TargetHit, view.Verdict);
    }

    [Fact]
    public void A_past_week_that_missed_the_target_is_Behind_with_its_count_and_a_hit_week_stays_Target_hit()
    {
        var nextWed = Start.AddDays(9);
        var miss = Build(Start, nextWed, Days(0));
        var hit = Build(Start, nextWed, Days(0, 1, 2));

        Assert.False(miss.IsCurrentWeek);
        Assert.Equal(WeeklyVerdict.Behind, miss.Verdict);
        Assert.Equal("Behind: 1 of 3 done that week.", miss.Message);
        Assert.Equal(WeeklyVerdict.TargetHit, hit.Verdict);
    }

    [Fact]
    public void Week_boundaries_are_Monday_to_Sunday()
    {
        // Sunday 11 Jan counts in week one; Monday 12 Jan counts in week two.
        var workouts = new[] { Done("a", Start.AddDays(6)), Done("b", Start.AddDays(7)) };
        var today = Start.AddDays(9);

        Assert.Equal(1, Build(Start, today, workouts).Counted);
        Assert.Equal(1, Build(Start.AddDays(7), today, workouts).Counted);
    }

    [Fact]
    public void Any_date_in_a_week_selects_that_week_and_a_future_week_clamps_to_this_one()
    {
        Assert.Equal(Start, Build(Start.AddDays(3), Wed).Monday);
        Assert.Equal(Start, Build(Start.AddDays(14), Wed).Monday);
    }

    [Fact]
    public void Previous_and_next_stop_at_the_Plan_start_and_this_week()
    {
        var first = Build(Start, Start.AddDays(15));
        var middle = Build(Start.AddDays(7), Start.AddDays(15));
        var current = Build(Start.AddDays(14), Start.AddDays(15));

        Assert.Null(first.PreviousMonday);
        Assert.Equal(Start.AddDays(7), first.NextMonday);
        Assert.Equal(Start, middle.PreviousMonday);
        Assert.Equal(Start.AddDays(14), middle.NextMonday);
        Assert.Null(current.NextMonday);
    }

    [Fact]
    public void The_Streak_is_as_of_the_viewed_week()
    {
        var workouts = new[] { Done("a", Start), Done("b", Start.AddDays(1)), Done("c", Start.AddDays(7)), Done("d", Start.AddDays(8)) };
        var today = Start.AddDays(15);

        Assert.Equal(1, Build(Start, today, workouts).Streak);
        Assert.Equal(2, Build(Start.AddDays(7), today, workouts).Streak);
    }

    [Fact]
    public void Missed_Sessions_are_those_of_the_viewed_week()
    {
        var view = Build(Start, Start.AddDays(9));

        Assert.Equal(["Lower", "Test"], view.Missed.Select(m => m.SessionName));
        Assert.Equal(Start, view.Missed[0].Date);
        Assert.DoesNotContain(Build(Start, Start.AddDays(9), [Done("a", Start, "a-lower")]).Missed, m => m.SessionName == "Lower");
    }

    [Fact]
    public void PRs_are_only_those_set_in_the_week_and_the_first_result_is_a_baseline()
    {
        var workouts = new[]
        {
            Done("w0", Start, "a-lower", null, Set(kg: 20, reps: 5)),
            Done("w1", Start.AddDays(7), "a-lower", null, Set(kg: 30, reps: 5)),
        };
        var today = Start.AddDays(9);

        Assert.Empty(Build(Start, today, workouts).Prs);
        var pr = Assert.Single(Build(Start.AddDays(7), today, workouts).Prs);
        Assert.Equal("Squat", pr.ExerciseName);
    }

    [Fact]
    public void Weight_average_is_compared_with_the_week_before_in_the_display_unit()
    {
        WeighIn[] weighIns = [new("a", Start.AddDays(-7), 90), new("b", Start.AddDays(-6), 92), new("c", Start, 88), new("d", Start.AddDays(2), 90)];
        var kg = Build(Start, Wed, weighIns: weighIns);

        Assert.Equal(89, kg.WeightAverage);
        Assert.Equal(91, kg.LastWeekAverage);
        Assert.Equal("kg", kg.UnitLabel);

        var lb = Build(Start, Wed, weighIns: weighIns, settings: Settings.Default with { DisplayWeightUnit = WeightUnit.Lb });
        Assert.Equal("lb", lb.UnitLabel);
        Assert.Equal(196, lb.WeightAverage); // 89 kg is 196.2 lb, shown to the unit's own rounding
    }

    [Fact]
    public void No_Weigh_ins_gives_no_averages()
    {
        var view = Build(Start, Wed);

        Assert.Null(view.WeightAverage);
        Assert.Null(view.LastWeekAverage);
    }

    [Fact]
    public void Pain_checks_that_week_list_only_reported_ones()
    {
        var reported = new PainCheck("p1", PainStatus.Reported, [BodyArea.LowerBack, BodyArea.Knee], PainSeverity.Moderate, null, null);
        var none = new PainCheck("p2", PainStatus.None, [], null, null, null);
        var workouts = new[]
        {
            Done("a", Start.AddDays(1), pain: reported),
            Done("b", Start.AddDays(2), pain: none),
            Done("c", Start.AddDays(8), pain: reported),
        };

        var pain = Assert.Single(Build(Start, Start.AddDays(9), workouts).PainChecks);
        Assert.Equal(Start.AddDays(1), pain.Date);
        Assert.Equal("Lower back, knee, moderate", pain.Text);
    }

    [Fact]
    public void No_active_Plan_gives_an_empty_review()
    {
        var view = WeeklyReviewBuilder.Build(new DataSnapshot(), Start, Wed);

        Assert.False(view.HasPlan);
        Assert.Empty(view.Missed);
        Assert.Empty(view.Prs);
        Assert.Empty(view.PainChecks);
    }
}
