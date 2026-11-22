using Core.Model;
using Core.Rules.Tracks;

namespace Core.Tests.Model;

public class TrackModelTests
{
    private static readonly Phase Foundation = new("foundation", "Foundation", 1, 4, [], []);

    private static Session MakeSession(string id, string trackId, DefaultDay day = DefaultDay.Monday, SessionType type = SessionType.Strength) =>
        new(id, id, trackId, day, type, null, []);

    [Fact]
    public void TrackRhythm_weekly_and_daily_expose_their_cadence_and_rules()
    {
        TrackRhythm weekly = new TrackRhythm.Weekly(WeeklyTarget: 4, StreakMinWorkoutsPerWeek: 3, StreakCountsCustomWorkouts: true);
        TrackRhythm daily = new TrackRhythm.Daily(DayDonePercent: 75);

        Assert.Equal(TrackCadence.Weekly, weekly.Cadence);
        Assert.Equal(TrackCadence.Daily, daily.Cadence);

        var weeklyRules = Assert.IsType<TrackRhythm.Weekly>(weekly);
        Assert.Equal((4, 3, true), (weeklyRules.WeeklyTarget, weeklyRules.StreakMinWorkoutsPerWeek, weeklyRules.StreakCountsCustomWorkouts));

        var dailyRules = Assert.IsType<TrackRhythm.Daily>(daily);
        Assert.Equal(75, dailyRules.DayDonePercent);
        Assert.Equal(75, new TrackRhythm.Daily().DayDonePercent);
    }

    [Fact]
    public void SessionType_includes_Mobility_and_DefaultDay_includes_Daily()
    {
        Assert.Contains(SessionType.Mobility, Enum.GetValues<SessionType>());
        Assert.Contains(DefaultDay.Daily, Enum.GetValues<DefaultDay>());
        Assert.Equal(DefaultDay.Monday, DayOfWeek.Monday.ToDefaultDay());
        Assert.Equal(DayOfWeek.Monday, DefaultDay.Monday.AsDayOfWeek());
        Assert.Null(DefaultDay.Daily.AsDayOfWeek());
    }

    [Fact]
    public void TrackRules_returns_Main_TrackOf_and_SessionsOf()
    {
        var main = new Track("main", "Main", new TrackRhythm.Weekly(3, 2, false), [Foundation]);
        var mobility = new Track("mob", "Mobility", new TrackRhythm.Daily(80), []);
        var s1 = MakeSession("s1", main.Id, DefaultDay.Monday, SessionType.Strength);
        var s2 = MakeSession("s2", mobility.Id, DefaultDay.Daily, SessionType.Mobility);
        var s3 = MakeSession("s3", main.Id, DefaultDay.Wednesday, SessionType.Cardio);

        var plan = new Plan("p1", "HYROX", true, new DateOnly(2026, 10, 19), null, null, 2.0, [main, mobility], [s1, s2, s3]);

        Assert.Same(main, TrackRules.Main(plan));
        Assert.Same(main, TrackRules.TrackOf(plan, s1));
        Assert.Same(mobility, TrackRules.TrackOf(plan, s2));
        Assert.Equal([s1, s3], TrackRules.SessionsOf(plan, main));
        Assert.Equal([s2], TrackRules.SessionsOf(plan, mobility));
    }
}
