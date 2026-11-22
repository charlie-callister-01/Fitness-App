using Core.Model;
using Core.Rules.Guides;
using Core.Tests.Rules.TestData;
using static Core.Tests.Rules.TestData.Dates;
using static Core.Tests.Rules.TestData.HistoryBuilder;

namespace Core.Tests.Rules.Guides;

public class GuideHistoryViewTests
{
    private static readonly Exercise Squat = new("squat", "Squat", LogKind.Load, null, false);

    private static Workout Day(string id, int daysAfterMon, double kg, int reps = 5) =>
        Lift(id, Mon.AddDays(daysAfterMon), "lower", "squat", Set(kg: kg, reps: reps));

    private static readonly DateOnly Today = Mon.AddDays(30);

    [Fact]
    public void There_are_no_results_and_no_Best_without_history()
    {
        var view = GuideHistoryView.Build(Squat, [], null, Today);

        Assert.Empty(view.Recent);
        Assert.Null(view.Best);
        Assert.Null(view.Place);
    }

    [Fact]
    public void Fewer_than_three_results_are_all_shown_newest_first()
    {
        var view = GuideHistoryView.Build(Squat, [Day("w1", 0, 60), Day("w2", 7, 62.5)], null, Today);

        Assert.Equal(["w2", "w1"], view.Recent.Select(r => r.Result.WorkoutId));
    }

    [Fact]
    public void Only_the_last_three_results_are_shown_and_PRs_are_marked()
    {
        var history = new[] { Day("w1", 0, 60), Day("w2", 7, 62.5), Day("w3", 14, 62.5), Day("w4", 21, 65), Day("w5", 28, 60) };

        var view = GuideHistoryView.Build(Squat, history, null, Today);

        Assert.Equal(["w5", "w4", "w3"], view.Recent.Select(r => r.Result.WorkoutId));
        Assert.Equal([false, true, false], view.Recent.Select(r => r.IsPr));
        Assert.Equal("w4", view.Best!.WorkoutId);
    }

    [Fact]
    public void The_first_ever_result_is_a_baseline_not_a_PR()
    {
        var view = GuideHistoryView.Build(Squat, [Day("w1", 0, 60)], null, Today);

        Assert.False(Assert.Single(view.Recent).IsPr);
    }

    [Fact]
    public void A_skipped_Exercise_is_not_a_result()
    {
        var skipped = Day("w1", 0, 60) with { Entries = [new WorkoutEntry("squat", EntryStatus.Skipped, "time", null, null, [])] };

        Assert.Empty(GuideHistoryView.Build(Squat, [skipped], null, Today).Recent);
    }

    [Fact]
    public void The_place_lists_each_Session_that_holds_the_Exercise_with_its_row()
    {
        var plan = PlanFixture.Make(start: Mon, sessions:
        [
            PlanFixture.Session("lower", DayOfWeek.Monday, name: "Lower", exercises: [PlanFixture.Row("squat", sets: 4, reps: 8)]),
            PlanFixture.Session("upper", DayOfWeek.Wednesday, name: "Upper", exercises: [PlanFixture.Row("press")]),
            PlanFixture.Session("legs", DayOfWeek.Friday, name: "Legs", exercises: [PlanFixture.Row("squat", sets: 3, reps: 12)]),
        ]);

        var place = GuideHistoryView.Build(Squat, [], plan, Today).Place!;

        Assert.Equal(["Lower", "Legs"], place.Placements.Select(p => p.Session.Name));
        Assert.Equal(["Lower", "Legs"], place.Placements.Select(p => p.SessionLabel));
        Assert.Equal((4, 8), (place.Placements[0].Row.Sets, place.Placements[0].Row.Reps));
        Assert.Null(place.Phase);
    }

    [Fact]
    public void Placements_name_each_Sessions_Track_when_the_Plan_has_more_than_one_Track()
    {
        var plan = PlanFixture.Make(
            start: Mon,
            tracks:
            [
                new Track("main", "Main", new TrackRhythm.Weekly(3, 2, false), []),
                new Track("mob", "Morning mobility", new TrackRhythm.Daily(75), []),
            ],
            sessions:
            [
                PlanFixture.Session("lower", DayOfWeek.Monday, name: "A: Lower + core", exercises: [PlanFixture.Row("squat", sets: 3, reps: 8)], trackId: "main"),
                PlanFixture.Session("s-mob", DefaultDay.Daily, SessionType.Mobility, "Morning mobility", exercises: [PlanFixture.Row("squat", sets: 1, reps: 10)], trackId: "mob"),
            ]);

        var place = GuideHistoryView.Build(Squat, [], plan, Today).Place!;

        Assert.Equal(
            ["A: Lower + core (Main)", "Morning mobility (Morning mobility, daily)"],
            place.Placements.Select(p => p.SessionLabel));
    }

    [Fact]
    public void The_place_uses_the_current_Phase_override_rows_and_names_the_Phase()
    {
        var phase = new Phase("build", "Build", 1, 8, [], [new SessionOverride("lower", [PlanFixture.Row("squat", sets: 5, reps: 3)])]);
        var plan = PlanFixture.Make(start: Mon, phases: [phase], sessions:
        [
            PlanFixture.Session("lower", DayOfWeek.Monday, name: "Lower", exercises: [PlanFixture.Row("squat", sets: 3, reps: 8)]),
        ]);

        var place = GuideHistoryView.Build(Squat, [], plan, Mon.AddDays(8)).Place!;

        Assert.Equal("Build", place.Phase!.Focus);
        Assert.Equal(5, Assert.Single(place.Placements).Row.Sets);
    }

    [Fact]
    public void The_place_resolves_a_secondary_Track_Session_against_its_own_Tracks_current_Phase()
    {
        var mainPhase = new Phase("main-ph", "Main Base", 1, 8, [], []);
        var mobPhase = new Phase("mob-ph", "Mobility Block", 1, 4, [], [new SessionOverride("s-mob", [PlanFixture.Row("squat", sets: 4, reps: 10)])]);
        var plan = PlanFixture.Make(
            start: Mon,
            tracks:
            [
                new Track("main", "Gym", new TrackRhythm.Weekly(3, 2, false), [mainPhase]),
                new Track("mob", "Mobility", new TrackRhythm.Daily(75), [mobPhase]),
            ],
            sessions:
            [
                PlanFixture.Session("s-mob", DefaultDay.Daily, SessionType.Mobility, "Morning mobility", exercises: [PlanFixture.Row("squat", sets: 2, reps: 5)], trackId: "mob"),
            ]);

        var place = GuideHistoryView.Build(Squat, [], plan, Mon.AddDays(3)).Place!;

        Assert.Equal(4, Assert.Single(place.Placements).Row.Sets);
    }

    [Fact]
    public void An_Exercise_in_no_Session_has_no_place()
    {
        var plan = PlanFixture.Make(start: Mon, sessions: [PlanFixture.Session("upper", DayOfWeek.Wednesday, exercises: [PlanFixture.Row("press")])]);

        Assert.Null(GuideHistoryView.Build(Squat, [], plan, Today).Place);
    }
}
