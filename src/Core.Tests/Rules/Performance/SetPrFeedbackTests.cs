using Core.Model;
using Core.Rules.Performance;
using Core.Tests.Rules.TestData;
using static Core.Tests.Rules.TestData.HistoryBuilder;

namespace Core.Tests.Rules.Performance;

public class SetPrFeedbackTests
{
    private static Exercise Ex(LogKind kind) => new("ex", "ex", kind, null, false);

    private static SetPrFeedback Feedback(Exercise e, HistoryBuilder history, params WorkoutSet[] current) =>
        SetPrFeedbackRule.For(e, history.Workouts, current);

    private static HistoryBuilder Prior(WorkoutSet set) => new HistoryBuilder().Done("w1", "2026-01-01", "ex", set);

    /// <summary>Finished-Workout PR answer for the same data, from the existing rule.</summary>
    private static bool FinishedIsPr(Exercise e, HistoryBuilder history, params WorkoutSet[] current)
    {
        var all = history.Workouts.Append(
            new Workout("cur", new DateOnly(2026, 6, 1), null, false, null, null, [],
                [new WorkoutEntry("ex", EntryStatus.Done, null, null, null, current)], null));
        return ExercisePerformance.Prs(e, all).Any(p => p.WorkoutId == "cur");
    }

    public static TheoryData<LogKind, WorkoutSet, WorkoutSet, WorkoutSet> RankingKinds() => new()
    {
        // kind, previous result, strictly better, worse
        { LogKind.Load, Set(kg: 100, reps: 5), Set(kg: 105, reps: 5), Set(kg: 95, reps: 5) },
        { LogKind.BodyweightLoad, Set(kg: 5, reps: 8), Set(kg: 7.5, reps: 8), Set(kg: 2.5, reps: 8) },
        { LogKind.Carry, Set(kg: 24, m: 40), Set(kg: 32, m: 20), Set(kg: 20, m: 100) },
        { LogKind.Reps, Set(reps: 12), Set(reps: 13), Set(reps: 11) },
        { LogKind.Seconds, Set(seconds: 45), Set(seconds: 50), Set(seconds: 40) },
        { LogKind.Minutes, Set(seconds: 180), Set(seconds: 240), Set(seconds: 120) },
        { LogKind.DistanceKm, Set(km: 5), Set(km: 5.5), Set(km: 4) },
        { LogKind.DistanceM, Set(m: 400), Set(m: 410), Set(m: 390) },
        { LogKind.Time, Set(seconds: 1500), Set(seconds: 1450), Set(seconds: 1600) }, // lower is better
    };

    [Theory]
    [MemberData(nameof(RankingKinds))]
    public void Strictly_better_is_a_pr_equal_and_worse_are_not_and_it_agrees_with_the_finished_rule(
        LogKind kind, WorkoutSet previous, WorkoutSet better, WorkoutSet worse)
    {
        var e = Ex(kind);
        var h = Prior(previous);

        var improved = Feedback(e, h, better);
        Assert.True(improved.IsPr);
        Assert.Same(better, improved.WorkoutBest);
        Assert.Same(previous, improved.PreviousBest!.Set);
        Assert.True(FinishedIsPr(e, h, better));

        var equal = Feedback(e, h, Set(kg: previous.WeightKg, reps: previous.Reps, km: previous.DistanceKm, seconds: previous.DurationSeconds, m: previous.DistanceM));
        Assert.False(equal.IsPr);
        Assert.NotNull(equal.WorkoutBest);
        Assert.False(FinishedIsPr(e, h, equal.WorkoutBest!));

        Assert.False(Feedback(e, h, worse).IsPr);
        Assert.False(FinishedIsPr(e, h, worse));
    }

    [Fact]
    public void The_first_ever_result_is_a_baseline_and_not_a_pr()
    {
        var e = Ex(LogKind.Load);

        var feedback = Feedback(e, new HistoryBuilder(), Set(kg: 100, reps: 5));

        Assert.False(feedback.IsPr);
        Assert.Equal(100, feedback.WorkoutBest!.WeightKg);
        Assert.Null(feedback.PreviousBest);
    }

    [Fact]
    public void No_confirmed_sets_has_no_best_and_no_pr_but_still_reports_the_previous_best()
    {
        var feedback = Feedback(Ex(LogKind.Load), Prior(Set(kg: 100, reps: 5)));

        Assert.Null(feedback.WorkoutBest);
        Assert.False(feedback.IsPr);
        Assert.Equal(100, feedback.PreviousBest!.Set.WeightKg);
    }

    [Fact]
    public void Only_the_workouts_single_best_set_is_returned_and_the_marker_moves_to_a_later_better_set()
    {
        var e = Ex(LogKind.Load);
        var h = Prior(Set(kg: 100, reps: 5));
        var first = Set(kg: 105, reps: 5);
        var later = Set(kg: 110, reps: 5);

        Assert.Same(first, Feedback(e, h, first).WorkoutBest);

        var two = Feedback(e, h, first, later);
        Assert.Same(later, two.WorkoutBest);
        Assert.True(two.IsPr);

        var laterButWorse = Feedback(e, h, first, Set(kg: 102, reps: 5));
        Assert.Same(first, laterButWorse.WorkoutBest);
    }

    [Fact]
    public void An_equal_later_set_keeps_the_marker_on_the_earlier_set()
    {
        var e = Ex(LogKind.Load);
        var first = Set(kg: 105, reps: 5);

        var feedback = Feedback(e, Prior(Set(kg: 100, reps: 5)), first, Set(kg: 105, reps: 5));

        Assert.Same(first, feedback.WorkoutBest);
    }

    [Fact]
    public void Removing_or_editing_the_set_changes_the_answer()
    {
        var e = Ex(LogKind.Load);
        var h = Prior(Set(kg: 100, reps: 5));
        var pr = Set(kg: 110, reps: 5);
        var other = Set(kg: 95, reps: 5);

        Assert.True(Feedback(e, h, other, pr).IsPr);

        var removed = Feedback(e, h, other);
        Assert.False(removed.IsPr);
        Assert.Same(other, removed.WorkoutBest);

        Assert.False(Feedback(e, h, other, pr with { WeightKg = 100 }).IsPr);
    }

    [Fact]
    public void A_max_set_in_history_outranks_a_better_ordinary_set()
    {
        var e = Ex(LogKind.Load);
        var h = Prior(Set(kg: 100, reps: 1, max: true));
        var ordinary = Set(kg: 100, reps: 10);

        var feedback = Feedback(e, h, ordinary);

        Assert.False(feedback.IsPr);
        Assert.True(feedback.PreviousBest!.IsMax);
        Assert.False(FinishedIsPr(e, h, ordinary));
    }

    [Fact]
    public void A_better_max_set_beats_a_max_set_in_history()
    {
        var e = Ex(LogKind.Load);
        var h = Prior(Set(kg: 100, reps: 1, max: true));
        var better = Set(kg: 105, reps: 1, max: true);

        Assert.True(Feedback(e, h, better).IsPr);
        Assert.True(FinishedIsPr(e, h, better));
        Assert.False(Feedback(e, h, Set(kg: 95, reps: 1, max: true)).IsPr);
    }

    [Fact]
    public void A_max_set_in_the_workout_is_its_best_even_with_a_higher_ordinary_estimate_and_must_still_beat_history()
    {
        var e = Ex(LogKind.Load);
        var h = Prior(Set(kg: 100, reps: 5));
        var max = Set(kg: 130, reps: 1, max: true); // 134.3 beats 100 x 5 = 116.7
        var ordinary = Set(kg: 100, reps: 10); // 133.3, below the Max set's 134.3 but would not matter either way

        var feedback = Feedback(e, h, ordinary, max);

        Assert.Same(max, feedback.WorkoutBest);
        Assert.True(feedback.IsPr);
        Assert.True(FinishedIsPr(e, h, ordinary, max));
    }

    [Fact]
    public void A_max_set_in_the_workout_that_ranks_below_history_is_not_a_pr_even_if_an_ordinary_set_would_be()
    {
        var e = Ex(LogKind.Load);
        var h = Prior(Set(kg: 100, reps: 5)); // 116.7
        var max = Set(kg: 90, reps: 1, max: true); // 92.7
        var ordinary = Set(kg: 120, reps: 5); // 140

        var feedback = Feedback(e, h, max, ordinary);

        Assert.Same(max, feedback.WorkoutBest);
        Assert.False(feedback.IsPr);
        Assert.False(FinishedIsPr(e, h, max, ordinary));
    }

    [Fact]
    public void Kinds_that_do_not_rank_give_no_best_and_no_pr()
    {
        foreach (var kind in new[] { LogKind.BodyMeasurementKg, LogKind.BodyMeasurementCm, LogKind.Text, LogKind.TickOnly })
        {
            var feedback = Feedback(Ex(kind), Prior(Set(kg: 80)), Set(kg: 79));
            Assert.Null(feedback.WorkoutBest);
            Assert.False(feedback.IsPr);
        }
    }

    [Fact]
    public void Skipped_history_does_not_count_as_a_previous_best()
    {
        var h = new HistoryBuilder().Skipped("w1", "2026-01-01", "ex");

        var feedback = Feedback(Ex(LogKind.Load), h, Set(kg: 100, reps: 5));

        Assert.False(feedback.IsPr);
        Assert.Null(feedback.PreviousBest);
    }
}
