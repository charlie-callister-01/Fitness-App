using Core.Model;
using Core.Rules.Badges;
using Core.Storage;

namespace Core.Tests.Rules.Badges;

public class BadgeHistoriesTests
{
    private static readonly DateOnly Day = new(2026, 11, 16);

    private static Plan PlanOf(string id, bool active, string sessionId) =>
        new(id, id, active, Day, null, null, 3, 2, false, 2.5, [],
            [new Session(sessionId, "S", DayOfWeek.Monday, SessionType.Strength, null, [])]);

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
}
