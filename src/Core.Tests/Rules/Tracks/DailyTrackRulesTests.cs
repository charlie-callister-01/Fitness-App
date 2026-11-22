using Core.Model;
using Core.Rules.Nudges;
using Core.Rules.Schedule;
using Core.Rules.Tracks;
using Core.Tests.Rules.TestData;
using static Core.Tests.Rules.TestData.Dates;

namespace Core.Tests.Rules.Tracks;

public class DailyTrackRulesTests
{
    private const string MainTrackId = "gym";
    private const string DailyTrackId = "mob";
    private const string DailySessionId = "s-mob";
    private const string WeeklySessionId = "s-gym";

    private static Track DailyTrack(int dayDonePercent = 75) =>
        new(DailyTrackId, "Mobility", new TrackRhythm.Daily(dayDonePercent), []);

    private static Plan MakePlan(DateOnly? start = null, int dayDonePercent = 75)
    {
        var gym = new Track(MainTrackId, "Gym", new TrackRhythm.Weekly(3, 2, false), []);
        var mob = DailyTrack(dayDonePercent);
        return PlanFixture.Make(
            start: start ?? D(2026, 11, 1),
            tracks: [gym, mob],
            sessions:
            [
                PlanFixture.Session(WeeklySessionId, DayOfWeek.Monday, trackId: MainTrackId),
                PlanFixture.Session(DailySessionId, DefaultDay.Daily, SessionType.Mobility, "Morning Mobility", trackId: DailyTrackId),
            ]);
    }

    private static Workout DailyWorkout(
        string id,
        DateOnly date,
        int plannedCount,
        int doneCount,
        int skippedCount = 0,
        int swappedCount = 0,
        string? sessionId = DailySessionId,
        bool withSets = false)
    {
        var planned = Enumerable.Range(1, plannedCount)
            .Select(i => PlanFixture.Row($"ex-{i}", sets: 1, reps: null))
            .ToList();

        var entries = new List<WorkoutEntry>();
        var idx = 1;
        for (var i = 0; i < doneCount; i++, idx++)
        {
            var sets = withSets ? new[] { HistoryBuilder.Set(seconds: 30) } : [];
            entries.Add(new WorkoutEntry($"ex-{idx}", EntryStatus.Done, null, null, null, sets));
        }

        for (var i = 0; i < skippedCount; i++, idx++)
        {
            entries.Add(new WorkoutEntry($"ex-{idx}", EntryStatus.Skipped, "time", null, null, []));
        }

        for (var i = 0; i < swappedCount; i++, idx++)
        {
            entries.Add(new WorkoutEntry($"alt-{idx}", EntryStatus.Swapped, null, $"ex-{idx}", null, []));
        }

        return new Workout(id, date, sessionId, false, null, null, planned, entries, null);
    }

    // ---- Day done threshold & row outcomes

    [Fact]
    public void Day_is_done_at_exactly_75_percent_and_above_but_not_below()
    {
        var plan = MakePlan();
        var mob = plan.Tracks[1];

        var at75 = DailyWorkout("w1", D(2026, 11, 1), plannedCount: 4, doneCount: 3);
        var below75 = DailyWorkout("w2", D(2026, 11, 2), plannedCount: 3, doneCount: 2); // 2/3 = 66.6%
        var at100 = DailyWorkout("w3", D(2026, 11, 3), plannedCount: 4, doneCount: 4);
        var oneOfTen = DailyWorkout("w4", D(2026, 11, 4), plannedCount: 10, doneCount: 1);

        Workout[] workouts = [at75, below75, at100, oneOfTen];

        Assert.True(DailyTrackRules.IsDayDone(plan, mob, workouts, D(2026, 11, 1)));
        Assert.False(DailyTrackRules.IsDayDone(plan, mob, workouts, D(2026, 11, 2)));
        Assert.True(DailyTrackRules.IsDayDone(plan, mob, workouts, D(2026, 11, 3)));
        Assert.False(DailyTrackRules.IsDayDone(plan, mob, workouts, D(2026, 11, 4)));
    }

    [Fact]
    public void Tick_only_rows_with_done_entry_count_while_skipped_swapped_and_empty_planned_do_not()
    {
        var plan = MakePlan();
        var mob = plan.Tracks[1];

        // 4 planned: 2 done (tick-only, no sets), 1 skipped, 1 swapped -> only 2/4 = 50% done (not >= 75%)
        var mixed = DailyWorkout("w1", D(2026, 11, 1), plannedCount: 4, doneCount: 2, skippedCount: 1, swappedCount: 1, withSets: false);
        // 4 planned: 3 tick-only done (no sets) -> 3/4 = 75% done
        var tickOnlyDone = DailyWorkout("w2", D(2026, 11, 2), plannedCount: 4, doneCount: 3, withSets: false);
        // 0 planned rows -> never done even with extra entries
        var noPlanned = new Workout(
            "w3", D(2026, 11, 3), DailySessionId, false, null, null, [],
            [new WorkoutEntry("ex-1", EntryStatus.Done, null, null, null, [])], null);

        Workout[] workouts = [mixed, tickOnlyDone, noPlanned];

        Assert.False(DailyTrackRules.IsDayDone(plan, mob, workouts, D(2026, 11, 1)));
        Assert.True(DailyTrackRules.IsDayDone(plan, mob, workouts, D(2026, 11, 2)));
        Assert.False(DailyTrackRules.IsDayDone(plan, mob, workouts, D(2026, 11, 3)));
    }

    [Fact]
    public void Custom_day_done_percent_changes_the_result()
    {
        var day = D(2026, 11, 1);
        var halfDone = DailyWorkout("w1", day, plannedCount: 4, doneCount: 2);
        Workout[] workouts = [halfDone];

        var at75 = MakePlan(dayDonePercent: 75);
        Assert.False(DailyTrackRules.IsDayDone(at75, at75.Tracks[1], workouts, day));
        Assert.Equal(new DailyTrackStatus(0, 0, 0, false), DailyTrackRules.Status(at75, at75.Tracks[1], workouts, [], day));

        var at50 = MakePlan(dayDonePercent: 50);
        Assert.True(DailyTrackRules.IsDayDone(at50, at50.Tracks[1], workouts, day));
        Assert.Equal(new DailyTrackStatus(1, 1, 1, true), DailyTrackRules.Status(at50, at50.Tracks[1], workouts, [], day));

        var at100 = MakePlan(dayDonePercent: 100);
        var threeOfFour = DailyWorkout("w2", day, plannedCount: 4, doneCount: 3);
        var fourOfFour = DailyWorkout("w3", day, plannedCount: 4, doneCount: 4);
        Assert.False(DailyTrackRules.IsDayDone(at100, at100.Tracks[1], [threeOfFour], day));
        Assert.True(DailyTrackRules.IsDayDone(at100, at100.Tracks[1], [fourOfFour], day));
    }

    [Fact]
    public void Multiple_workouts_on_one_date_count_as_one_done_day_if_any_passes_without_combining_partials()
    {
        var day1 = D(2026, 11, 1);
        var day2 = D(2026, 11, 2);
        var plan = MakePlan(start: day1);
        var mob = plan.Tracks[1];

        // Day 1: one partial (1/4) and one done (3/4) -> done, and counts as 1 day
        // Day 2: two partials (each 2/4 = 50%) -> not done
        Workout[] workouts =
        [
            DailyWorkout("w1", day1, plannedCount: 4, doneCount: 1),
            DailyWorkout("w2", day1, plannedCount: 4, doneCount: 3),
            DailyWorkout("w3", day2, plannedCount: 4, doneCount: 2),
            DailyWorkout("w4", day2, plannedCount: 4, doneCount: 2),
        ];

        Assert.True(DailyTrackRules.IsDayDone(plan, mob, workouts, day1));
        Assert.False(DailyTrackRules.IsDayDone(plan, mob, workouts, day2));
        Assert.Equal(new DailyTrackStatus(1, 1, 1, false), DailyTrackRules.Status(plan, mob, workouts, [], day2));
        Assert.Equal(new DailyTrackStatus(0, 1, 1, false), DailyTrackRules.Status(plan, mob, workouts, [], day2.AddDays(1)));
    }

    [Fact]
    public void Workouts_from_a_weekly_track_or_custom_workouts_are_ignored()
    {
        var day = D(2026, 11, 1);
        var plan = MakePlan(start: day);
        var gym = plan.Tracks[0];
        var mob = plan.Tracks[1];

        Workout[] workouts =
        [
            DailyWorkout("w-weekly", day, plannedCount: 4, doneCount: 4, sessionId: WeeklySessionId),
            DailyWorkout("w-custom", day, plannedCount: 4, doneCount: 4, sessionId: null),
        ];

        Assert.False(DailyTrackRules.IsDayDone(plan, mob, workouts, day));
        Assert.Equal(new DailyTrackStatus(0, 0, 0, false), DailyTrackRules.Status(plan, mob, workouts, [], day));
        Assert.Equal(new DailyTrackStatus(0, 0, 0, false), DailyTrackRules.Status(plan, gym, workouts, [], day));
    }

    [Fact]
    public void Extra_unplanned_entries_do_not_inflate_planned_completion()
    {
        var day = D(2026, 11, 1);
        var plan = MakePlan(start: day);
        var mob = plan.Tracks[1];

        var w = new Workout(
            "w1", day, DailySessionId, false, null, null,
            [PlanFixture.Row("ex-1"), PlanFixture.Row("ex-2"), PlanFixture.Row("ex-3"), PlanFixture.Row("ex-4")],
            [
                new WorkoutEntry("ex-1", EntryStatus.Done, null, null, null, []),
                new WorkoutEntry("ex-2", EntryStatus.Done, null, null, null, []),
                new WorkoutEntry("extra-1", EntryStatus.Done, null, null, null, []),
                new WorkoutEntry("extra-2", EntryStatus.Done, null, null, null, []),
            ],
            null);

        Assert.Equal(50, DailyTrackRules.DonePercent(w));
        Assert.False(DailyTrackRules.IsDayDone(plan, mob, [w], day));
    }

    // ---- Daily Streak, best run, days done, Breaks, start date

    [Fact]
    public void Open_today_does_not_break_the_run_and_done_today_adds_to_it()
    {
        var start = D(2026, 11, 1);
        var plan = MakePlan(start: start);
        var mob = plan.Tracks[1];

        Workout[] beforeToday =
        [
            DailyWorkout("w1", D(2026, 11, 1), 4, 3),
            DailyWorkout("w2", D(2026, 11, 2), 4, 4),
        ];

        var today = D(2026, 11, 3);
        var openStatus = DailyTrackRules.Status(plan, mob, beforeToday, [], today);
        Assert.Equal(new DailyTrackStatus(Current: 2, BestRun: 2, DaysDone: 2, TodayDone: false), openStatus);

        // Partial workout today below threshold keeps TodayDone false and doesn't break streak
        Workout[] partialToday = [.. beforeToday, DailyWorkout("w3", today, 4, 1)];
        Assert.Equal(
            new DailyTrackStatus(Current: 2, BestRun: 2, DaysDone: 2, TodayDone: false),
            DailyTrackRules.Status(plan, mob, partialToday, [], today));

        // Completing today increments Current, BestRun, DaysDone and sets TodayDone true
        Workout[] doneToday = [.. beforeToday, DailyWorkout("w3", today, 4, 3)];
        Assert.Equal(
            new DailyTrackStatus(Current: 3, BestRun: 3, DaysDone: 3, TodayDone: true),
            DailyTrackRules.Status(plan, mob, doneToday, [], today));
    }

    [Fact]
    public void A_break_day_inside_a_run_is_neutral()
    {
        var start = D(2026, 11, 1);
        var plan = MakePlan(start: start);
        var mob = plan.Tracks[1];
        Break[] breaks = [HistoryBuilder.BreakOf(D(2026, 11, 3), D(2026, 11, 4))];

        Workout[] workouts =
        [
            DailyWorkout("w1", D(2026, 11, 1), 4, 4),
            DailyWorkout("w2", D(2026, 11, 2), 4, 4),
            // Nov 3-4 are Break days (Nov 3 untouched, Nov 4 even has a workout -> adds nothing to streak)
            DailyWorkout("w4", D(2026, 11, 4), 4, 4),
            DailyWorkout("w5", D(2026, 11, 5), 4, 4),
        ];

        var status = DailyTrackRules.Status(plan, mob, workouts, breaks, D(2026, 11, 5));
        Assert.Equal(new DailyTrackStatus(Current: 3, BestRun: 3, DaysDone: 4, TodayDone: true), status);
    }

    [Fact]
    public void A_missed_day_ends_the_run_while_best_run_and_total_days_keep_history()
    {
        var start = D(2026, 11, 1);
        var plan = MakePlan(start: start);
        var mob = plan.Tracks[1];

        Workout[] workouts =
        [
            DailyWorkout("w1", D(2026, 11, 1), 4, 4),
            DailyWorkout("w2", D(2026, 11, 2), 4, 4),
            DailyWorkout("w3", D(2026, 11, 3), 4, 4),
            // Nov 4 missed
            DailyWorkout("w5", D(2026, 11, 5), 4, 4),
            DailyWorkout("w6", D(2026, 11, 6), 4, 4),
        ];

        var status = DailyTrackRules.Status(plan, mob, workouts, [], D(2026, 11, 6));
        Assert.Equal(new DailyTrackStatus(Current: 2, BestRun: 3, DaysDone: 5, TodayDone: true), status);
    }

    [Fact]
    public void Days_before_the_plan_start_date_are_ignored()
    {
        var start = D(2026, 11, 5);
        var plan = MakePlan(start: start);
        var mob = plan.Tracks[1];

        Workout[] workouts =
        [
            DailyWorkout("w-pre1", D(2026, 11, 3), 4, 4),
            DailyWorkout("w-pre2", D(2026, 11, 4), 4, 4),
            DailyWorkout("w-start", D(2026, 11, 5), 4, 4),
        ];

        Assert.False(DailyTrackRules.IsDayDone(plan, mob, workouts, D(2026, 11, 4)));
        Assert.Equal(
            new DailyTrackStatus(0, 0, 0, false),
            DailyTrackRules.Status(plan, mob, workouts, [], D(2026, 11, 4)));
        Assert.Equal(
            new DailyTrackStatus(Current: 1, BestRun: 1, DaysDone: 1, TodayDone: true),
            DailyTrackRules.Status(plan, mob, workouts, [], D(2026, 11, 5)));
    }

    [Fact]
    public void A_daily_session_is_never_missed_and_produces_no_missed_nudge()
    {
        var plan = MakePlan(start: Mon);
        var mob = plan.Tracks[1];
        var fri = Mon.AddDays(4);

        Assert.Empty(WeekScheduleRules.MissedInWeek(plan, mob, [], [], [], Mon, fri));
        Assert.Empty(WeekScheduleRules.Missed(plan, mob, [], [], [], fri));

        var nudges = NudgeRules.Compute(plan, [], [], [], new DateTimeOffset(2026, 11, 15, 9, 0, 0, TimeSpan.Zero), fri);
        Assert.DoesNotContain(nudges, n => n.Kind == NudgeKind.MissedSession && n.SessionId == DailySessionId);
    }
}
