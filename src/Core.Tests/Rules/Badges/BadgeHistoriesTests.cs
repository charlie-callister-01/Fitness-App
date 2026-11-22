using Core.Model;
using Core.Rules.Badges;
using Core.Storage;
using Core.Tests.Rules.TestData;

namespace Core.Tests.Rules.Badges;

public class BadgeHistoriesTests
{
    private static readonly DateOnly Day = new(2026, 11, 16);

    private static Plan PlanOf(string id, bool active, string sessionId) =>
        PlanFixture.Make(id: id, name: id, isActive: active, start: Day, incrementKg: 2.5,
            sessions: [PlanFixture.Session(sessionId, DefaultDay.Monday, name: "S", trackId: $"{id}-main")]);

    private static Workout WorkoutOn(string sessionId) =>
        new("w1", Day, sessionId, false, null, null, [], [], null);

    [Fact]
    public void The_Plan_holding_the_Workouts_Session_wins_over_the_active_Plan()
    {
        var snapshot = new DataSnapshot { Plans = [PlanOf("old", false, "old-s"), PlanOf("new", true, "new-s")] };

        Assert.Equal("old", BadgeHistories.For(snapshot, WorkoutOn("old-s")).Plan.Id);
        Assert.Equal("new", BadgeHistories.For(snapshot, WorkoutOn(null!)).Plan.Id);
    }

    [Fact]
    public void With_no_Plan_at_all_the_history_gets_an_empty_Plan_and_carries_the_stored_data()
    {
        var workout = WorkoutOn("x");
        var snapshot = new DataSnapshot { Workouts = [workout] };

        var history = BadgeHistories.For(snapshot, workout);

        Assert.Empty(history.Plan.Sessions);
        Assert.Equal([workout], history.Workouts);
    }

    [Fact]
    public void Carries_all_Tracks_of_the_Plan_so_qualified_Track_Badges_can_be_measured()
    {
        var main = new Track("main", "Main", new TrackRhythm.Weekly(3, 2, false), []);
        var mob = new Track("mob", "Mobility", new TrackRhythm.Daily(75), []);
        var plan = PlanFixture.Make(
            start: Day,
            tracks: [main, mob],
            sessions:
            [
                PlanFixture.Session("s-main", DefaultDay.Monday, trackId: "main"),
                PlanFixture.Session("s-mob", DefaultDay.Daily, SessionType.Mobility, trackId: "mob"),
            ]);
        var workout = new Workout("w1", Day, "s-mob", false, null, null,
            [new SessionExercise("cat-cow", 1, null, null, null)],
            [new WorkoutEntry("cat-cow", EntryStatus.Done, null, null, null, [])], null);
        var snapshot = new DataSnapshot { Plans = [plan], Workouts = [workout] };

        var history = BadgeHistories.For(snapshot, workout);
        var progress = BadgeRules.Progress(new Badge("b1", "Mob 1 day", "days:mob", 1), history, Day);

        Assert.True(progress.IsMeasurable);
        Assert.Equal(1, progress.Value);
        Assert.True(progress.Reached);
    }
}
