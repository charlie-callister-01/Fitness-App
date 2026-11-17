using Core.Model;
using Core.Rules.CustomWorkout;

namespace Core.Tests.Rules.CustomWorkout;

public class CustomWorkoutOfferTests
{
    private static DateOnly D(int y, int m, int d) => new(y, m, d);

    // Plan starts Mon 2026-01-05. Lower (Mon): squat, lunge. Upper (Wed): press, squat. Run (Fri): run.
    private static readonly DateOnly Start = D(2026, 1, 5);
    private static readonly DateOnly Mon = D(2026, 11, 16);

    private static Exercise Ex(string id) => new(id, id, LogKind.Load, null, false);
    private static readonly Exercise[] Library = [Ex("curl"), Ex("run"), Ex("squat"), Ex("press"), Ex("lunge")];

    private static SessionExercise Row(string id) => new(id, 3, 8, null, null);

    private static Plan Plan(IReadOnlyList<Phase>? phases = null) => new(
        "p", "Plan", true, Start, null, null, 3, 2, false, 2, phases ?? [],
        [
            new Session("lower", "Lower", DayOfWeek.Monday, SessionType.Strength, null, [Row("squat"), Row("lunge")]),
            new Session("upper", "Upper", DayOfWeek.Wednesday, SessionType.Strength, null, [Row("press"), Row("squat")]),
            new Session("run", "Run", DayOfWeek.Friday, SessionType.Cardio, null, [Row("run")]),
        ]);

    private static Workout Made(DateOnly date, string? sessionId) =>
        new($"w{date.DayNumber}{sessionId}", date, sessionId, false, null, null, [], [], null);

    private static List<string> Ids(IReadOnlyList<Exercise> o) => o.Select(e => e.Id).ToList();

    [Fact]
    public void With_nothing_missed_the_library_order_is_kept()
    {
        // Monday itself: nothing is before today yet.
        Assert.Equal(["curl", "run", "squat", "press", "lunge"], Ids(CustomWorkoutOffer.Order(Plan(), [], [], [], Library, Mon)));
    }

    [Fact]
    public void Missed_session_exercises_come_first_in_plan_order_without_duplicates_then_the_rest_of_the_library()
    {
        // Thursday: Lower (Mon) and Upper (Wed) are missed; Run (Fri) is not yet due.
        var order = CustomWorkoutOffer.Order(Plan(), [], [], [], Library, Mon.AddDays(3));

        Assert.Equal(["squat", "lunge", "press", "curl", "run"], Ids(order));
    }

    [Fact]
    public void A_session_that_was_done_is_not_offered_first()
    {
        var order = CustomWorkoutOffer.Order(Plan(), [], [], [Made(Mon, "lower")], Library, Mon.AddDays(3));

        Assert.Equal(["press", "squat", "curl", "run", "lunge"], Ids(order));
    }

    [Fact]
    public void A_Custom_workout_does_not_clear_a_missed_session_so_its_exercises_stay_first()
    {
        var order = CustomWorkoutOffer.Order(Plan(), [], [], [Made(Mon.AddDays(1), null)], Library, Mon.AddDays(1));

        Assert.Equal(["squat", "lunge", "curl", "run", "press"], Ids(order));
    }

    [Fact]
    public void Exercises_missing_from_the_library_are_skipped()
    {
        var order = CustomWorkoutOffer.Order(Plan(), [], [], [], [Ex("curl"), Ex("lunge")], Mon.AddDays(1));

        Assert.Equal(["lunge", "curl"], Ids(order));
    }

    [Fact]
    public void The_current_Phase_override_rows_are_the_ones_offered()
    {
        var phase = new Phase("ph", "Focus", 1, 100, [], [new SessionOverride("lower", [Row("press")])]);

        var order = CustomWorkoutOffer.Order(Plan([phase]), [], [], [], Library, Mon.AddDays(1));

        Assert.Equal(["press", "curl", "run", "squat", "lunge"], Ids(order));
    }
}
