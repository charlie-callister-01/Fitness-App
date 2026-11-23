using Core.Model;
using Core.Rules.Starting;
using Core.Storage;

namespace Core.Tests.Rules.Starting;

public class FirstRunTests
{
    private static readonly DateOnly Today = new(2026, 11, 16);

    private static Plan SomePlan() =>
        FirstRun.EmptyPlan("p", "t", Today) with { IsActive = false };

    [Fact]
    public void An_empty_dataset_is_the_first_run_state()
    {
        Assert.True(FirstRun.IsEmpty(DataSnapshot.Empty));
    }

    [Fact]
    public void A_dataset_with_only_Exercises_is_still_the_first_run_state()
    {
        var snapshot = DataSnapshot.Empty with { Exercises = [new Exercise("squat", "Squat", LogKind.Load, null, false)] };

        Assert.True(FirstRun.IsEmpty(snapshot));
    }

    [Fact]
    public void A_Plan_a_Workout_or_a_Weigh_in_each_end_the_first_run_state()
    {
        Assert.False(FirstRun.IsEmpty(DataSnapshot.Empty with { Plans = [SomePlan()] }));
        Assert.False(FirstRun.IsEmpty(DataSnapshot.Empty with { WeighIns = [new WeighIn("w", Today, 80)] }));
        Assert.False(FirstRun.IsEmpty(DataSnapshot.Empty with
        {
            Workouts = [new Workout("x", Today, null, true, DateTimeOffset.UnixEpoch, null, [], [], null)],
        }));
    }

    [Fact]
    public void The_empty_Plan_is_active_with_one_weekly_main_Track_and_no_Sessions()
    {
        var plan = FirstRun.EmptyPlan("plan-1", "track-1", Today);

        Assert.Equal("plan-1", plan.Id);
        Assert.True(plan.IsActive);
        Assert.Equal(Today, plan.StartDate);
        Assert.Empty(plan.Sessions);
        var track = Assert.Single(plan.Tracks);
        Assert.Equal("track-1", track.Id);
        Assert.IsType<TrackRhythm.Weekly>(track.Rhythm);
        Assert.Empty(track.Phases);
    }

    [Theory]
    [InlineData(false, PersistenceStatus.Granted, true)]
    [InlineData(false, PersistenceStatus.NotGranted, true)]
    [InlineData(true, PersistenceStatus.NotGranted, true)]
    [InlineData(true, PersistenceStatus.Granted, false)]
    [InlineData(true, PersistenceStatus.Unsupported, false)]
    [InlineData(false, PersistenceStatus.Unsupported, true)]
    public void Two_quick_things_show_while_not_installed_or_not_persistent(bool standalone, PersistenceStatus persistence, bool expected)
    {
        Assert.Equal(expected, FirstRun.HasQuickThingsToDo(standalone, persistence));
    }
}
