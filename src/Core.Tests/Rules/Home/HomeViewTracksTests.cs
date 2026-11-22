using Core.Model;
using Core.Rules.Home;
using Core.Storage;
using Core.Tests.Rules.TestData;
using static Core.Tests.Rules.TestData.Dates;

namespace Core.Tests.Rules.Home;

public class HomeViewTracksTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Thu = Mon.AddDays(3);
    private static readonly IReadOnlySet<string> NoDismissals = new HashSet<string>();
    private static readonly DateTimeOffset RecentBackup = new(2026, 11, 18, 9, 0, 0, TimeSpan.Zero);

    private const string DailyTrackId = "mob";
    private const string DailySessionId = "s-mob";
    private const string RunTrackId = "run";

    private static Session MainSession(string id, DayOfWeek day) =>
        PlanFixture.Session(id, day, exercises: [PlanFixture.Row("squat")]);

    private static Session Daily() =>
        PlanFixture.Session(DailySessionId, DefaultDay.Daily, SessionType.Mobility, "Morning Mobility", trackId: DailyTrackId);

    private static Session Run(string id, DayOfWeek day) =>
        PlanFixture.Session(id, day, SessionType.Cardio, id, trackId: RunTrackId);

    private static Plan OneTrack() => PlanFixture.Make(
        start: Mon, sessions: [MainSession("lower", DayOfWeek.Monday), MainSession("upper", DayOfWeek.Thursday)]);

    private static Plan TwoTrack(bool daily = true, bool run = true)
    {
        List<Track> tracks = [new("p-main", "Main", new TrackRhythm.Weekly(3, 2, false), [])];
        List<Session> sessions = [MainSession("lower", DayOfWeek.Monday), MainSession("upper", DayOfWeek.Thursday)];
        if (daily)
        {
            tracks.Add(new Track(DailyTrackId, "Mobility", new TrackRhythm.Daily(75), []));
            sessions.Add(Daily());
        }

        if (run)
        {
            tracks.Add(new Track(RunTrackId, "Running", new TrackRhythm.Weekly(2, 1, false), []));
            sessions.Add(Run("s-run-a", DayOfWeek.Tuesday));
            sessions.Add(Run("s-run-b", DayOfWeek.Saturday));
        }

        return PlanFixture.Make(start: Mon, tracks: tracks, sessions: sessions);
    }

    private static HomeView Build(Plan plan, params Workout[] workouts) =>
        HomeViewBuilder.Build(
            DataSnapshot.Empty with { Plans = [plan], Workouts = workouts },
            Now, Thu, 10, NoDismissals, RecentBackup);

    private static Workout DailyWorkout(string id, DateOnly date, int planned, int done, bool open = false)
    {
        var rows = Enumerable.Range(1, planned).Select(i => PlanFixture.Row($"m{i}", 1, null)).ToList();
        var entries = Enumerable.Range(1, done)
            .Select(i => new WorkoutEntry($"m{i}", EntryStatus.Done, null, null, null, []))
            .ToList();
        return Workout(id, date, DailySessionId, rows, entries, open);
    }

    private static Workout Workout(
        string id, DateOnly date, string sessionId, IReadOnlyList<SessionExercise> rows,
        IReadOnlyList<WorkoutEntry> entries, bool open = false) =>
        new(id, date, sessionId, false,
            new DateTimeOffset(date.ToDateTime(new TimeOnly(8, 0)), TimeSpan.Zero),
            open ? null : new DateTimeOffset(date.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero),
            rows, entries, null);

    private static Workout Finished(string id, DateOnly date, string sessionId) =>
        Workout(id, date, sessionId, [PlanFixture.Row("squat")],
            [new WorkoutEntry("squat", EntryStatus.Done, null, null, null, [new WorkoutSet("s1", 100, 5, null, null, null, false)])]);

    [Fact]
    public void One_Track_Plan_has_empty_Track_lists()
    {
        var view = Build(OneTrack());

        Assert.Empty(view.DailyTracks);
        Assert.Empty(view.ExtraWeekLines);
    }

    [Fact]
    public void Other_Tracks_do_not_change_the_main_Track_card_strip_line_or_orbs()
    {
        var one = Build(OneTrack());
        var two = Build(TwoTrack());

        Assert.Equal(one.Today.Kind, two.Today.Kind);
        Assert.Equal(one.Today.Session?.Id, two.Today.Session?.Id);
        Assert.Equal(one.Strip.Select(t => (t.Date, t.Code, t.State)), two.Strip.Select(t => (t.Date, t.Code, t.State)));
        Assert.Equal((one.Line!.Done, one.Line.Scheduled, one.Line.Target), (two.Line!.Done, two.Line.Scheduled, two.Line.Target));
        Assert.Equal(one.Line.MissedCodes, two.Line.MissedCodes);
        Assert.Equal(one.Orbs, two.Orbs);
        Assert.Equal(one.UndoneSessions.Select(s => s.SessionId), two.UndoneSessions.Select(s => s.SessionId));
        Assert.Equal(TodayKind.Start, two.Today.Kind);
        Assert.Equal("upper", two.Today.Session?.Id);
    }

    [Fact]
    public void Today_card_ignores_a_daily_Workout_in_progress_or_done_today()
    {
        var open = Build(TwoTrack(), DailyWorkout("w1", Thu, 4, 1, open: true));
        var done = Build(TwoTrack(), DailyWorkout("w2", Thu, 4, 4));

        Assert.Equal(TodayKind.Start, open.Today.Kind);
        Assert.Equal(TodayKind.Start, done.Today.Kind);
    }

    [Fact]
    public void Today_card_never_offers_a_daily_Session_and_no_daily_Session_is_Missed()
    {
        var view = Build(TwoTrack(run: false));

        Assert.DoesNotContain(view.UndoneSessions, s => s.Session.Id == DailySessionId);
        Assert.Equal(DailySessionId, Assert.Single(view.DailyTracks).Session?.Id);
        Assert.DoesNotContain(view.Strip, t => t.Session?.Id == DailySessionId);
        Assert.DoesNotContain(view.Line!.MissedCodes, c => c.StartsWith('M'));
        Assert.Equal(TodayKind.Start, view.Today.Kind);
    }

    [Fact]
    public void Daily_card_is_not_started_without_a_Workout_today()
    {
        var card = Assert.Single(Build(TwoTrack(run: false)).DailyTracks);

        Assert.Equal(DailyTrackId, card.TrackId);
        Assert.Equal("Mobility", card.TrackName);
        Assert.Equal(DailyTrackState.NotStarted, card.State);
        Assert.Equal(0, card.DonePercent);
        Assert.Equal(0, card.Streak);
        Assert.False(card.OnBreak);
    }

    [Fact]
    public void Daily_card_is_in_progress_with_an_open_Workout_and_reports_the_percent_reached()
    {
        var card = Assert.Single(Build(TwoTrack(run: false), DailyWorkout("w1", Thu, 4, 2, open: true)).DailyTracks);

        Assert.Equal(DailyTrackState.InProgress, card.State);
        Assert.Equal(50, card.DonePercent);
        Assert.Equal("w1", card.InProgressWorkout?.Id);
    }

    [Fact]
    public void Daily_card_is_done_at_the_threshold_and_carries_the_Streak()
    {
        var card = Assert.Single(Build(
            TwoTrack(run: false),
            DailyWorkout("w0", Thu.AddDays(-1), 4, 4),
            DailyWorkout("w1", Thu, 4, 3)).DailyTracks);

        Assert.Equal(DailyTrackState.Done, card.State);
        Assert.Equal(75, card.DonePercent);
        Assert.Equal(2, card.Streak);
    }

    [Fact]
    public void Daily_card_below_the_threshold_once_finished_is_not_done()
    {
        var card = Assert.Single(Build(TwoTrack(run: false), DailyWorkout("w1", Thu, 4, 1)).DailyTracks);

        Assert.Equal(DailyTrackState.NotStarted, card.State);
        Assert.Equal(25, card.DonePercent);
    }

    [Fact]
    public void A_Break_marks_the_daily_card()
    {
        var plan = TwoTrack(run: false);
        var snapshot = DataSnapshot.Empty with
        {
            Plans = [plan],
            Breaks = [new Break("b1", Thu.AddDays(-1), Thu.AddDays(1), "rest")],
        };

        var view = HomeViewBuilder.Build(snapshot, Now, Thu, 10, NoDismissals, RecentBackup);

        Assert.True(Assert.Single(view.DailyTracks).OnBreak);
    }

    [Fact]
    public void An_extra_weekly_Track_gets_its_own_week_line()
    {
        var view = Build(TwoTrack(daily: false), Finished("w-run", Thu.AddDays(-2), "s-run-a"));

        var line = Assert.Single(view.ExtraWeekLines);
        Assert.Equal(RunTrackId, line.TrackId);
        Assert.Equal("Running", line.TrackName);
        Assert.Equal(1, line.Done);
        Assert.Equal(2, line.Target);
        Assert.Equal(2, line.Sessions.Count);
        Assert.Equal(("s-run-a", TileState.Done, false), (line.Sessions[0].Session.Id, line.Sessions[0].State, line.Sessions[0].IsToday));
        Assert.Equal(("s-run-b", TileState.Upcoming), (line.Sessions[1].Session.Id, line.Sessions[1].State));
        Assert.Equal(1, line.Streak);
    }

    [Fact]
    public void An_extra_weekly_Session_not_done_by_its_day_is_Missed_but_main_Missed_stays_main_only()
    {
        var view = Build(TwoTrack(daily: false));

        var line = Assert.Single(view.ExtraWeekLines);
        Assert.Equal(TileState.Missed, line.Sessions[0].State);
        Assert.Equal(["s-run-a"], line.MissedSessionIds);
        Assert.Equal(["L"], view.Line!.MissedCodes);
        Assert.DoesNotContain(view.UndoneSessions, s => s.Session.TrackId == RunTrackId);
    }

    [Fact]
    public void Both_extra_Track_kinds_appear_together_in_Track_order()
    {
        var view = Build(TwoTrack());

        Assert.Equal([DailyTrackId], view.DailyTracks.Select(d => d.TrackId));
        Assert.Equal([RunTrackId], view.ExtraWeekLines.Select(d => d.TrackId));
    }
}
