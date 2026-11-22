using Core.Model;
using Core.Rules.Home;
using Core.Rules.Nudges;
using Core.Storage;
using Core.Tests.Rules.TestData;
using static Core.Tests.Rules.TestData.Dates;

namespace Core.Tests.Rules.Home;

public class HomeViewBuilderTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Thu = Mon.AddDays(3);
    private static readonly IReadOnlySet<string> NoDismissals = new HashSet<string>();
    private static readonly DateTimeOffset RecentBackup = new(2026, 11, 18, 9, 0, 0, TimeSpan.Zero);

    private static Plan MakePlan() => PlanFixture.Make(
        start: Mon,
        target: 3,
        phases: [new Phase("ph", "Foundation", 1, 8, [], [])],
        sessions:
        [
            PlanFixture.Session("lower", DayOfWeek.Monday, name: "L: Lower", exercises: [PlanFixture.Row("squat")]),
            PlanFixture.Session("upper", DayOfWeek.Thursday, name: "Upper", exercises: [PlanFixture.Row("press")]),
        ]);

    private static DataSnapshot Snapshot(params Workout[] workouts) =>
        DataSnapshot.Empty with { Plans = [MakePlan()], Workouts = workouts };

    private static HomeView Build(DataSnapshot snapshot, DateOnly? today = null, int hour = 10,
        IReadOnlySet<string>? dismissed = null, bool noBackup = false) =>
        HomeViewBuilder.Build(snapshot, Now, today ?? Thu, hour, dismissed ?? NoDismissals, noBackup ? null : RecentBackup);

    private static Workout Finished(string id, DateOnly date, string? sessionId) => new(
        id, date, sessionId, false,
        new DateTimeOffset(date.ToDateTime(new TimeOnly(8, 0)), TimeSpan.Zero),
        new DateTimeOffset(date.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero),
        [PlanFixture.Row("squat")],
        [new WorkoutEntry("squat", EntryStatus.Done, null, null, null, [new WorkoutSet("s1", 100, 5, null, null, null, false)])],
        null);

    private static Workout InProgress(DateOnly date) => new(
        "w-open", date, "upper", false, Now.AddMinutes(-10), null,
        [PlanFixture.Row("press")], [], null);

    [Theory]
    [InlineData(0, "Good morning")]
    [InlineData(11, "Good morning")]
    [InlineData(12, "Good afternoon")]
    [InlineData(17, "Good afternoon")]
    [InlineData(18, "Good evening")]
    [InlineData(23, "Good evening")]
    public void Greeting_follows_the_local_hour(int hour, string expected)
    {
        Assert.Equal(expected, Build(Snapshot(), hour: hour).Header.Greeting);
    }

    [Fact]
    public void Header_carries_the_date_and_phase_label()
    {
        var view = Build(Snapshot());

        Assert.Equal(Thu, view.Header.Date);
        Assert.Equal("Foundation week 1 of 8", view.Header.PhaseLabel);
    }

    [Fact]
    public void Empty_snapshot_is_NoPlan_and_does_not_throw()
    {
        var view = Build(DataSnapshot.Empty);

        Assert.Equal(TodayKind.NoPlan, view.Today.Kind);
        Assert.Empty(view.UndoneSessions);
        Assert.Empty(view.Strip);
        Assert.Null(view.Line);
        Assert.Null(view.Orbs);
        Assert.Empty(view.Nudges);
        Assert.Empty(view.NudgeKeys);
        Assert.Empty(view.RecentPrs);
        Assert.Null(view.Weight.Latest);
        Assert.Null(view.Weight.WeekAverageKg);
        Assert.Null(view.Weight.ChangeKg);
        Assert.Equal(string.Empty, view.Header.PhaseLabel);
        Assert.False(view.ShowAlternatives);
        Assert.False(view.ShowLogPastWorkout);
        Assert.False(view.ShowNudges);
    }

    [Fact]
    public void Break_day_hides_alternatives_and_nudges_but_keeps_Log_weight()
    {
        var snapshot = Snapshot() with { Breaks = [new Break("b", Mon, Mon.AddDays(13), "Holiday")] };

        var view = Build(snapshot, noBackup: true);

        Assert.Equal(TodayKind.Break, view.Today.Kind);
        Assert.False(view.ShowAlternatives);
        Assert.False(view.ShowLogPastWorkout);
        Assert.False(view.ShowNudges);
        Assert.Empty(view.Nudges);
        Assert.True(view.Orbs!.Streak.OnBreak);
    }

    [Fact]
    public void In_progress_hides_alternatives_but_still_shows_nudges()
    {
        var view = Build(Snapshot(InProgress(Thu)), noBackup: true);

        Assert.Equal(TodayKind.InProgress, view.Today.Kind);
        Assert.False(view.ShowAlternatives);
        Assert.False(view.ShowLogPastWorkout);
        Assert.True(view.ShowNudges);
        Assert.NotEmpty(view.Nudges);
    }

    [Fact]
    public void Normal_day_shows_alternatives_and_past_workout_link()
    {
        var view = Build(Snapshot());

        Assert.Equal(TodayKind.Start, view.Today.Kind);
        Assert.True(view.ShowAlternatives);
        Assert.True(view.ShowLogPastWorkout);
        Assert.True(view.ShowNudges);
    }

    [Fact]
    public void Normal_week_is_consistent_across_the_parts()
    {
        var snapshot = Snapshot();

        var view = Build(snapshot);

        Assert.Equal(7, view.Strip.Count);
        Assert.NotNull(view.Line);
        Assert.NotNull(view.Orbs);
        Assert.Equal(["L"], view.Line!.MissedCodes);
        var missed = Assert.Single(view.Nudges, n => n.Nudge.Kind == NudgeKind.MissedSession);
        Assert.Equal("lower", missed.Nudge.SessionId);
        Assert.Contains(view.UndoneSessions, s => s.Session.Id == "lower");
        Assert.Equal(view.Nudges.Select(n => n.Key).Order(), view.NudgeKeys.Where(k => view.Nudges.Any(n => n.Key == k)).Order());
    }

    [Fact]
    public void Nudges_are_capped_at_two()
    {
        var snapshot = DataSnapshot.Empty with { Plans = [PlanFixture.WithMainWeekly(MakePlan(), target: 1)] };

        var view = Build(snapshot, noBackup: true);

        Assert.Equal(2, view.Nudges.Count);
        Assert.Equal(3, view.NudgeKeys.Count);
    }

    [Fact]
    public void A_dismissed_key_removes_that_Nudge()
    {
        var first = Build(Snapshot());
        var key = first.Nudges[0].Key;

        var view = Build(Snapshot(), dismissed: new HashSet<string> { key });

        Assert.DoesNotContain(view.Nudges, n => n.Key == key);
    }

    [Fact]
    public void Null_last_backup_gives_the_No_Backup_nudge_and_a_recent_one_does_not()
    {
        var snapshot = Snapshot(Finished("w1", Mon, "lower"));

        var none = Build(snapshot, noBackup: true);
        var recent = Build(snapshot);

        Assert.Contains(none.NudgeKeys, k => k.Contains(nameof(NudgeKind.NoRecentBackup)));
        Assert.DoesNotContain(recent.NudgeKeys, k => k.Contains(nameof(NudgeKind.NoRecentBackup)));
    }

    [Fact]
    public void Weight_panel_and_recent_prs_come_from_the_snapshot()
    {
        var snapshot = Snapshot(Finished("w1", Mon, "lower")) with
        {
            WeighIns = [new WeighIn("wi1", Thu, 86.3)],
            Exercises = [new Exercise("squat", "Squat", LogKind.Load, null, false)],
        };

        var view = Build(snapshot);

        Assert.Equal(86.3, view.Weight.Latest!.WeightKg);
        Assert.Equal(86.3, view.Weight.WeekAverageKg);
    }
}
