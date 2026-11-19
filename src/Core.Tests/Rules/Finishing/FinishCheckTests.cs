using Core.Model;
using Core.Rules.Finishing;

namespace Core.Tests.Rules.Finishing;

public class FinishCheckTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 16, 9, 0, 0, TimeSpan.Zero);

    private static WorkoutSet Set() => new("s1", 20, 10, null, null, null, false);

    private static WorkoutEntry Done(string id, params WorkoutSet[] sets) => new(id, EntryStatus.Done, null, null, null, sets);

    private static Workout Open(params WorkoutEntry[] entries) => new(
        "w1", new DateOnly(2026, 11, 16), "s1", false, Start, null,
        [new SessionExercise("squat", 3, 8, null, null), new SessionExercise("press", 3, 8, null, null)],
        entries, null);

    [Fact]
    public void A_Workout_with_no_confirmed_Set_is_refused()
    {
        var check = FinishCheck.Of(Open(Done("squat")));

        Assert.Equal(WorkoutFinishing.NoSetRefusal, check.Refusal);
    }

    [Fact]
    public void Every_planned_Exercise_done_is_not_Partial()
    {
        var check = FinishCheck.Of(Open(Done("squat", Set()), Done("press", Set())));

        Assert.Null(check.Refusal);
        Assert.False(check.WouldBePartial);
        Assert.Equal((2, 2), (check.DoneCount, check.PlannedCount));
    }

    [Fact]
    public void An_Exercise_left_or_done_with_no_Sets_makes_it_Partial_and_is_not_counted_done()
    {
        var check = FinishCheck.Of(Open(Done("squat", Set()), Done("press")));

        Assert.True(check.WouldBePartial);
        Assert.Equal((1, 2), (check.DoneCount, check.PlannedCount));
    }
}
