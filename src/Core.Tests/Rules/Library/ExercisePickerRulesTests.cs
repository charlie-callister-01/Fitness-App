using Core.Model;
using Core.Rules.Library;
using Core.Tests.Rules.Outcomes;
using Core.Tests.Rules.TestData;
using static Core.Tests.Rules.TestData.Dates;
using static Core.Tests.Rules.TestData.HistoryBuilder;

namespace Core.Tests.Rules.Library;

public class ExercisePickerRulesTests
{
    private static Exercise Ex(string id, string name, params string[] muscles) =>
        new(id, name, LogKind.Load, muscles.Length == 0 ? null : new ExerciseTags(muscles, [], []), false);

    private static string[] Names(PickerGroup group) => [.. group.Items.Select(e => e.Name)];

    private static PickerGroup Group(PickerResult result, string heading) => result.Groups.Single(g => g.Heading == heading);

    // Swap and Add: the Exercise in hand, similar first.

    private static readonly Exercise Squat = Ex("squat", "Squat", "legs");

    private static readonly Exercise[] SwapLibrary =
    [
        Squat, Ex("bench", "Bench", "chest"), Ex("lunge", "Lunge", "legs"), Ex("curl", "Curl"), Ex("press", "Press", "legs", "shoulders"),
    ];

    private static readonly Workout InWorkout = new WorkoutBuilder().Plan("squat", "bench").Build();

    [Theory]
    [InlineData(PickerContext.Swap)]
    [InlineData(PickerContext.Add)]
    public void Swap_and_Add_offer_Similar_first_then_the_rest_without_what_is_already_in_the_Workout(PickerContext context)
    {
        var request = context == PickerContext.Swap
            ? PickerRequest.ForSwap(Squat, InWorkout, SwapLibrary)
            : PickerRequest.ForAdd(Squat, InWorkout, SwapLibrary);

        var result = ExercisePickerRules.Choose(request, null);

        Assert.Equal(context, request.Context);
        Assert.Equal(["Lunge", "Press"], Names(Group(result, "Similar")));
        Assert.Equal(["Curl"], Names(Group(result, "All Exercises")));
        Assert.Equal(["Similar", "All Exercises"], result.Groups.Select(g => g.Heading));
        Assert.Null(result.NewExerciseOffer);
    }

    [Fact]
    public void A_search_narrows_both_groups_ignoring_case_and_spaces_and_drops_a_group_with_no_match()
    {
        var result = ExercisePickerRules.Choose(PickerRequest.ForSwap(Squat, InWorkout, SwapLibrary), "  PRE ");

        Assert.Equal(["Press"], Names(Assert.Single(result.Groups)));
        Assert.Equal("Similar", result.Groups[0].Heading);
    }

    [Fact]
    public void A_search_with_no_match_offers_the_typed_text_as_a_new_Exercise()
    {
        var result = ExercisePickerRules.Choose(PickerRequest.ForSwap(Squat, InWorkout, SwapLibrary), "  zzz ");

        Assert.True(result.IsEmpty);
        Assert.Equal("zzz", result.NewExerciseOffer);
    }

    [Fact]
    public void An_empty_library_with_no_search_has_nothing_and_no_offer()
    {
        var result = ExercisePickerRules.Choose(PickerRequest.ForPlanEditor([], []), "");

        Assert.True(result.IsEmpty);
        Assert.Null(result.NewExerciseOffer);
    }

    // The search threshold.

    [Theory]
    [InlineData(20, false)]
    [InlineData(21, true)]
    public void The_search_box_shows_only_when_the_library_has_more_than_twenty_Exercises(int count, bool expected)
    {
        var library = Enumerable.Range(0, count).Select(i => Ex($"e{i}", $"Exercise {i}")).ToList();

        Assert.Equal(expected, ExercisePickerRules.Choose(PickerRequest.ForPlanEditor(library, []), null).ShowSearch);
    }

    // Custom workout: missed Sessions first, the draft left out.

    private static readonly Exercise[] CustomLibrary = [Ex("curl", "Curl"), Ex("run", "Run"), Ex("squat", "Squat"), Ex("press", "Press"), Ex("lunge", "Lunge")];

    private static Plan CustomPlan() => PlanFixture.Make(start: Mon, sessions:
    [
        PlanFixture.Session("lower", DayOfWeek.Monday, name: "Lower", exercises: [PlanFixture.Row("squat"), PlanFixture.Row("lunge")]),
        PlanFixture.Session("upper", DayOfWeek.Wednesday, name: "Upper", exercises: [PlanFixture.Row("press")]),
    ]);

    [Fact]
    public void Custom_workout_offers_missed_Session_Exercises_first_then_the_library_with_no_Similar_group()
    {
        var request = PickerRequest.ForCustomWorkout(CustomPlan(), [], [], [], CustomLibrary, Mon.AddDays(3), []);

        var result = ExercisePickerRules.Choose(request, null);

        Assert.Equal(["All Exercises"], result.Groups.Select(g => g.Heading));
        Assert.Equal(["Squat", "Lunge", "Press", "Curl", "Run"], Names(result.Groups[0]));
    }

    [Fact]
    public void Custom_workout_leaves_out_the_Exercises_already_in_the_draft()
    {
        var request = PickerRequest.ForCustomWorkout(CustomPlan(), [], [], [], CustomLibrary, Mon.AddDays(3), ["squat", "curl"]);

        Assert.Equal(["Lunge", "Press", "Run"], Names(ExercisePickerRules.Choose(request, null).Groups[0]));
    }

    [Fact]
    public void Custom_workout_with_no_active_Plan_offers_the_library_in_library_order()
    {
        var request = PickerRequest.ForCustomWorkout(null, [], [], [], CustomLibrary, Mon, ["run"]);

        Assert.Equal(["Curl", "Squat", "Press", "Lunge"], Names(ExercisePickerRules.Choose(request, null).Groups[0]));
    }

    [Fact]
    public void Custom_workout_does_not_offer_what_was_logged_as_done_first()
    {
        var request = PickerRequest.ForCustomWorkout(CustomPlan(), [], [], [Logged(Mon, "lower")], CustomLibrary, Mon.AddDays(3), []);

        Assert.Equal(["Press", "Curl", "Run", "Squat", "Lunge"], Names(ExercisePickerRules.Choose(request, null).Groups[0]));
    }

    // Plan editor.

    [Fact]
    public void Plan_editor_offers_the_library_in_order_without_the_Exercises_the_Session_already_has()
    {
        var request = PickerRequest.ForPlanEditor(CustomLibrary, ["run", "press"]);

        var result = ExercisePickerRules.Choose(request, null);

        Assert.Equal(PickerContext.PlanEditor, request.Context);
        Assert.Equal(["Curl", "Squat", "Lunge"], Names(Assert.Single(result.Groups)));
    }
}
