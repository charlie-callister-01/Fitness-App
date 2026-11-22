using Core.Model;
using Core.Rules.CustomWorkout;
using Core.Tests.Rules.TestData;
using static Core.Tests.Rules.TestData.Dates;
using static Core.Tests.Rules.TestData.HistoryBuilder;

namespace Core.Tests.Rules.CustomWorkout;

public class CustomWorkoutOfferTests
{
    // Plan starts Mon 2026-01-05. Lower (Mon): squat, lunge. Upper (Wed): press, squat. Run (Fri): run.
    private static readonly DateOnly Start = D(2026, 1, 5);

    private static Exercise Ex(string id) => new(id, id, LogKind.Load, null, false);
    private static readonly Exercise[] Library = [Ex("curl"), Ex("run"), Ex("squat"), Ex("press"), Ex("lunge")];

    private static SessionExercise Row(string id) => PlanFixture.Row(id);

    private static Plan Plan(IReadOnlyList<Phase>? phases = null) => PlanFixture.Make(start: Start, phases: phases, sessions:
    [
        PlanFixture.Session("lower", DayOfWeek.Monday, name: "Lower", exercises: [Row("squat"), Row("lunge")]),
        PlanFixture.Session("upper", DayOfWeek.Wednesday, name: "Upper", exercises: [Row("press"), Row("squat")]),
        PlanFixture.Session("run", DayOfWeek.Friday, SessionType.Cardio, name: "Run", exercises: [Row("run")]),
    ]);

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
        var order = CustomWorkoutOffer.Order(Plan(), [], [], [Logged(Mon, "lower")], Library, Mon.AddDays(3));

        Assert.Equal(["press", "squat", "curl", "run", "lunge"], Ids(order));
    }

    [Fact]
    public void A_Custom_workout_does_not_clear_a_missed_session_so_its_exercises_stay_first()
    {
        var order = CustomWorkoutOffer.Order(Plan(), [], [], [Logged(Mon.AddDays(1), null)], Library, Mon.AddDays(1));

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

    [Fact]
    public void Only_missed_Sessions_on_the_main_Track_are_offered_first()
    {
        var main = new Track("main", "Main", new TrackRhythm.Weekly(2, 1, false), []);
        var cardio = new Track("cardio", "Cardio", new TrackRhythm.Weekly(1, 1, false), []);
        var mobility = new Track("mob", "Mobility", new TrackRhythm.Daily(), []);
        var plan = PlanFixture.Make(start: Start, tracks: [main, cardio, mobility], sessions:
        [
            PlanFixture.Session("lower", DayOfWeek.Wednesday, name: "Lower", exercises: [Row("squat")], trackId: "main"),
            PlanFixture.Session("run", DayOfWeek.Monday, SessionType.Cardio, name: "Run", exercises: [Row("run")], trackId: "cardio"),
            PlanFixture.Session("mob", DefaultDay.Daily, SessionType.Mobility, name: "Mob", exercises: [Row("lunge")], trackId: "mob"),
        ]);

        // Tuesday: Cardio's Run (Mon) is missed, Main's Lower (Wed) is not yet due. Custom workout offer stays main-Track only.
        var order = CustomWorkoutOffer.Order(plan, [], [], [], Library, Mon.AddDays(1));

        Assert.Equal(["curl", "run", "squat", "press", "lunge"], Ids(order));
    }
}
