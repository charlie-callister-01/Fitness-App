using Core.Model;
using Core.Rules.Badges;
using Core.Rules.Outcomes;
using Core.Rules.Summary;
using Core.Tests.Rules.TestData;
using static Core.Tests.Rules.TestData.HistoryBuilder;

namespace Core.Tests.Rules.Summary;

public class WorkoutSummaryTests
{
    private static readonly DateOnly Today = Dates.Mon;

    private static readonly Exercise Squat = new("squat", "Goblet squat", LogKind.Load, null, false);
    private static readonly Exercise Bench = new("bench", "Bench press", LogKind.Load, null, false);
    private static readonly Exercise Row = new("row", "Row", LogKind.Load, null, false);
    private static readonly IReadOnlyList<Exercise> Exercises = [Squat, Bench, Row];

    private static SessionExercise Planned(string id) => new(id, 3, 8, null, null);

    private static WorkoutEntry Entry(string id, int? rpe, params WorkoutSet[] sets) =>
        new(id, EntryStatus.Done, null, null, rpe, sets);

    private static Workout Make(
        string id = "w-now",
        IReadOnlyList<SessionExercise>? planned = null,
        IReadOnlyList<WorkoutEntry>? entries = null,
        PainCheck? pain = null,
        DateTimeOffset? start = null,
        DateTimeOffset? end = null,
        bool afterwards = false,
        DateOnly? date = null) =>
        new(id, date ?? Today, "s1", afterwards, start, end, planned ?? [], entries ?? [], pain);

    private static BadgeHistory History(Workout current, IEnumerable<Workout>? earlier = null, Plan? plan = null, IReadOnlyList<Break>? breaks = null) =>
        new(plan ?? PlanFixture.Make(Today, target: 3, minPerWeek: 1, sessions: [PlanFixture.Session("s1", DayOfWeek.Monday)]),
            Exercises, [.. earlier ?? [], current], breaks ?? [], []);

    private static WorkoutSummary Build(Workout current, IEnumerable<Workout>? earlier = null, IReadOnlyList<Badge>? badges = null, Plan? plan = null, IReadOnlyList<Break>? breaks = null) =>
        WorkoutSummaryBuilder.Build(current, History(current, earlier, plan, breaks), badges ?? [], Today);

    private static Workout Earlier(string id, string date, string exerciseId, params WorkoutSet[] sets) =>
        new(id, DateOnly.Parse(date), "s1", false, null, null, [], [Entry(exerciseId, null, sets)], null);

    // Counts and PRs

    [Fact]
    public void Counts_sets_and_done_of_planned()
    {
        var current = Make(
            planned: [Planned("squat"), Planned("bench"), Planned("row")],
            entries:
            [
                Entry("squat", 7, Set(kg: 20, reps: 8), Set(kg: 20, reps: 8)),
                new WorkoutEntry("bench", EntryStatus.Skipped, "time", null, null, []),
                new WorkoutEntry("cable-row", EntryStatus.Swapped, null, "row", null, [Set(kg: 30, reps: 10)]),
            ]);

        var summary = Build(current);

        Assert.Equal(3, summary.SetCount);
        Assert.Equal(2, summary.DoneCount);
        Assert.Equal(3, summary.PlannedCount);
        Assert.True(summary.IsPartial);
    }

    [Fact]
    public void First_ever_result_is_a_baseline_not_a_pr()
    {
        var summary = Build(Make(entries: [Entry("squat", null, Set(kg: 20, reps: 8))]));

        Assert.Empty(summary.Prs);
    }

    [Fact]
    public void One_pr_per_exercise_is_the_best_set_with_previous_best()
    {
        var earlier = Earlier("w-old", "2026-11-02", "squat", Set(kg: 20, reps: 8));
        var best = Set(kg: 24, reps: 8);
        var summary = Build(
            Make(entries: [Entry("squat", null, Set(kg: 22, reps: 8), best, Set(kg: 21, reps: 8))]),
            [earlier]);

        var pr = Assert.Single(summary.Prs);
        Assert.Equal("squat", pr.ExerciseId);
        Assert.Equal(best, pr.Set);
        Assert.Equal(20, pr.PreviousBest!.Set.WeightKg);
        Assert.Equal(1, summary.PrCount);
    }

    [Fact]
    public void No_pr_when_the_workout_does_not_beat_history()
    {
        var earlier = Earlier("w-old", "2026-11-02", "squat", Set(kg: 30, reps: 8));

        var summary = Build(Make(entries: [Entry("squat", null, Set(kg: 20, reps: 8))]), [earlier]);

        Assert.Empty(summary.Prs);
    }

    [Fact]
    public void A_max_set_in_history_is_not_beaten_by_a_heavier_normal_set()
    {
        var earlier = Earlier("w-old", "2026-11-02", "squat", Set(kg: 20, reps: 8, max: true));

        var summary = Build(Make(entries: [Entry("squat", null, Set(kg: 40, reps: 8))]), [earlier]);

        Assert.Empty(summary.Prs);
    }

    [Fact]
    public void Previous_best_ignores_later_workouts_and_this_workout()
    {
        var older = Earlier("w-old", "2026-11-02", "squat", Set(kg: 20, reps: 8));
        var summary = Build(Make(entries: [Entry("squat", null, Set(kg: 25, reps: 8))]), [older]);

        Assert.Equal("w-old", Assert.Single(summary.Prs).PreviousBest!.WorkoutId);
    }

    [Fact]
    public void A_past_dated_workout_is_judged_against_history_up_to_its_own_date()
    {
        var before = Earlier("w-before", "2026-11-02", "squat", Set(kg: 20, reps: 8));
        var after = Earlier("w-after", "2026-11-09", "squat", Set(kg: 30, reps: 8));
        var pastDated = Make(entries: [Entry("squat", null, Set(kg: 22, reps: 8))], afterwards: true, date: new DateOnly(2026, 11, 5));

        var summary = Build(pastDated, [before, after]);

        var pr = Assert.Single(summary.Prs);
        Assert.Equal(22, pr.Set.WeightKg);
        Assert.Equal("w-before", pr.PreviousBest!.WorkoutId);
    }

    [Fact]
    public void A_past_dated_workout_is_no_pr_when_it_does_not_beat_its_own_past_whatever_came_later()
    {
        var before = Earlier("w-before", "2026-11-02", "squat", Set(kg: 30, reps: 8));
        var after = Earlier("w-after", "2026-11-09", "squat", Set(kg: 10, reps: 8));
        var pastDated = Make(entries: [Entry("squat", null, Set(kg: 22, reps: 8))], afterwards: true, date: new DateOnly(2026, 11, 5));

        Assert.Empty(Build(pastDated, [before, after]).Prs);
    }

    // Outcomes

    [Fact]
    public void Rows_list_planned_order_then_extras_with_rpe_reason_and_original()
    {
        var current = Make(
            planned: [Planned("squat"), Planned("bench"), Planned("row")],
            entries:
            [
                Entry("plank", 5, Set(seconds: 30)),
                new WorkoutEntry("cable-row", EntryStatus.Swapped, null, "row", 8, [Set(kg: 30, reps: 10)]),
                new WorkoutEntry("bench", EntryStatus.Skipped, "pain", null, null, []),
                Entry("squat", 7, Set(kg: 20, reps: 8)),
            ]);

        var rows = Build(current).Rows;

        Assert.Collection(rows,
            r => { Assert.Equal("squat", r.ExerciseId); Assert.Equal(RowOutcomeKind.Done, r.Kind); Assert.Equal(7, r.Rpe); Assert.False(r.IsExtra); },
            r => { Assert.Equal("bench", r.ExerciseId); Assert.Equal(RowOutcomeKind.Skipped, r.Kind); Assert.Equal("pain", r.SkipReason); Assert.Null(r.Rpe); },
            r => { Assert.Equal("cable-row", r.ExerciseId); Assert.Equal(RowOutcomeKind.Swapped, r.Kind); Assert.Equal("row", r.OriginalExerciseId); Assert.Equal(8, r.Rpe); },
            r => { Assert.Equal("plank", r.ExerciseId); Assert.True(r.IsExtra); Assert.Equal(5, r.Rpe); });
    }

    [Fact]
    public void A_planned_row_with_no_entry_is_unrecorded()
    {
        var summary = Build(Make(planned: [Planned("squat")], entries: []));

        Assert.Equal(RowOutcomeKind.Unrecorded, Assert.Single(summary.Rows).Kind);
        Assert.True(summary.IsPartial);
    }

    // Pain check

    [Fact]
    public void Pain_check_text_covers_unanswered_none_and_reported()
    {
        Assert.Equal("Not answered", Build(Make(pain: null)).PainCheck);
        Assert.Equal("No", Build(Make(pain: new PainCheck("p", PainStatus.None, [], null, null, null))).PainCheck);

        var reported = new PainCheck("p", PainStatus.Reported, [BodyArea.Knee], PainSeverity.Mild, "Left side. Twinge on the way down", "squat");
        Assert.Equal("Yes: Left knee, mild (during Goblet squat)", Build(Make(pain: reported)).PainCheck);
    }

    [Fact]
    public void Pain_check_text_without_side_exercise_or_note_and_with_several_areas()
    {
        var reported = new PainCheck("p", PainStatus.Reported, [BodyArea.LowerBack, BodyArea.Hip], PainSeverity.Sharp, null, null);

        Assert.Equal("Yes: Lower back, hip, sharp", Build(Make(pain: reported)).PainCheck);
    }

    // Streak

    [Fact]
    public void Streak_line_matches_the_streak_rule()
    {
        var current = Make(entries: [Entry("squat", null, Set(kg: 20, reps: 8))]);

        Assert.Equal("Week 1 of your Streak", Build(current).StreakLine);
    }

    [Fact]
    public void Streak_line_is_absent_when_the_streak_is_zero()
    {
        // A Workout with no done entry does not count.
        Assert.Null(Build(Make(entries: [])).StreakLine);
    }

    [Fact]
    public void Streak_line_counts_a_run_across_weeks_and_through_a_break()
    {
        var plan = PlanFixture.Make(Today.AddDays(-14), target: 3, minPerWeek: 1, sessions: [PlanFixture.Session("s1", DayOfWeek.Monday)]);
        var w1 = Earlier("w1", Today.AddDays(-14).ToString("yyyy-MM-dd"), "squat", Set(kg: 20, reps: 8));
        var current = Make(entries: [Entry("squat", null, Set(kg: 20, reps: 8))]);
        var breakWeek = BreakOf(Today.AddDays(-7), Today.AddDays(-1));

        Assert.Equal("Week 2 of your Streak", Build(current, [w1], plan: plan, breaks: [breakWeek]).StreakLine);
    }

    // Badges

    [Fact]
    public void A_badge_unlocks_only_when_this_workout_crossed_its_target()
    {
        var plan = PlanFixture.Make(Today, minPerWeek: 1, sessions: [PlanFixture.Session("s1", DayOfWeek.Monday)]);
        var first = Earlier("w1", "2026-11-09", "squat", Set(kg: 20, reps: 8));
        var current = Make(entries: [Entry("squat", null, Set(kg: 20, reps: 8))]);
        Badge[] badges =
        [
            new("two", "Two sessions", "sessions", 2),
            new("one", "One session", "sessions", 1),
            new("ten", "Ten sessions", "sessions", 10),
        ];

        var summary = Build(current, [first], badges, plan);

        Assert.Equal(["two"], summary.UnlockedBadges.Select(b => b.Id));
    }

    // Duration

    [Fact]
    public void Duration_is_end_minus_start()
    {
        var start = new DateTimeOffset(2026, 11, 16, 17, 0, 0, TimeSpan.Zero);

        var summary = Build(Make(start: start, end: start.AddMinutes(52)));

        Assert.Equal(TimeSpan.FromMinutes(52), summary.Duration);
    }

    [Fact]
    public void Duration_is_absent_when_logged_afterwards_or_a_time_is_missing()
    {
        var start = new DateTimeOffset(2026, 11, 16, 17, 0, 0, TimeSpan.Zero);

        Assert.Null(Build(Make(start: start, end: start.AddMinutes(52), afterwards: true)).Duration);
        Assert.Null(Build(Make(start: null, end: start)).Duration);
        Assert.Null(Build(Make(start: start, end: null)).Duration);
    }
}
