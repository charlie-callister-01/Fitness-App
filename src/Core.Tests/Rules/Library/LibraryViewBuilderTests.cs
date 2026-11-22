using Core.Model;
using Core.Rules.Library;
using Core.Tests.Rules.TestData;

namespace Core.Tests.Rules.Library;

public class LibraryViewBuilderTests
{
    private static Exercise Ex(string id, string name, string? muscle = null, string? equipment = null) =>
        new(id, name, LogKind.Load, muscle is null && equipment is null ? null : new ExerciseTags(muscle is null ? [] : [muscle], equipment is null ? [] : [equipment], []), false);

    private static readonly IReadOnlyList<Exercise> Library =
    [
        Ex("squat", "Squat", "legs", "barbell"), Ex("lunge", "Lunge", "legs"), Ex("press", "Press", "shoulders", "barbell"),
        Ex("zed", "Zercher", null), Ex("curl", "Curl", "arms"),
    ];

    private static Plan Plan() => PlanFixture.Make(sessions:
    [
        PlanFixture.Session("lower", DayOfWeek.Monday, name: "Lower", exercises: [PlanFixture.Row("squat"), PlanFixture.Row("lunge")]),
        PlanFixture.Session("upper", DayOfWeek.Wednesday, name: "Upper", exercises: [PlanFixture.Row("press"), PlanFixture.Row("squat")]),
        PlanFixture.Session("empty", DayOfWeek.Friday, name: "Empty"),
    ]);

    private static string[] Names(LibraryGroup g) => [.. g.Rows.Select(r => r.Exercise.Name)];

    [Fact]
    public void Exercises_are_grouped_by_the_first_Session_that_holds_them_then_the_rest_by_name()
    {
        var view = LibraryViewBuilder.Build(Plan(), Library, [], null, null);

        Assert.Equal(["Lower", "Upper", "Not in a Session"], view.Groups.Select(g => g.Heading));
        Assert.Equal(["Squat", "Lunge"], Names(view.Groups[0]));
        Assert.Equal(["Press"], Names(view.Groups[1]));
        Assert.Equal(["Curl", "Zercher"], Names(view.Groups[2]));
    }

    [Fact]
    public void With_no_Plan_everything_is_not_in_a_Session()
    {
        var view = LibraryViewBuilder.Build(null, Library, [], null, null);

        Assert.Equal("Not in a Session", Assert.Single(view.Groups).Heading);
        Assert.Equal(5, view.Groups[0].Rows.Count);
    }

    [Fact]
    public void Guide_status_is_per_Exercise()
    {
        var guide = new Guide("squat", null, null, null, null, [], [], null, null, [], null, null);

        var rows = LibraryViewBuilder.Build(Plan(), Library, [guide], null, null).Groups.SelectMany(g => g.Rows);

        Assert.Equal(["squat"], rows.Where(r => r.HasGuide).Select(r => r.Exercise.Id));
    }

    [Fact]
    public void Search_narrows_by_name_ignoring_case_and_spaces_and_drops_empty_groups()
    {
        var view = LibraryViewBuilder.Build(Plan(), Library, [], "  PRE ", null);

        Assert.Equal(["Upper"], view.Groups.Select(g => g.Heading));
        Assert.Equal(["Press"], Names(view.Groups[0]));
        Assert.Null(view.NewExerciseOffer);
    }

    [Fact]
    public void A_search_with_no_match_offers_the_text_as_a_new_Exercise()
    {
        var view = LibraryViewBuilder.Build(Plan(), Library, [], " squ1 ", null);

        Assert.Empty(view.Groups);
        Assert.Equal("squ1", view.NewExerciseOffer);
        Assert.False(view.IsLibraryEmpty);
    }

    [Fact]
    public void A_tag_filter_keeps_Exercises_with_that_tag_in_any_category_and_combines_with_search()
    {
        var byTag = LibraryViewBuilder.Build(Plan(), Library, [], null, "BARBELL");
        var both = LibraryViewBuilder.Build(Plan(), Library, [], "pre", "barbell");

        Assert.Equal(["Squat", "Press"], byTag.Groups.SelectMany(Names));
        Assert.Equal(["Press"], both.Groups.SelectMany(Names));
        Assert.Null(byTag.NewExerciseOffer);
    }

    [Fact]
    public void Tags_are_every_tag_once_sorted()
    {
        var tags = LibraryViewBuilder.Build(Plan(), [.. Library, Ex("x", "X", "LEGS")], [], null, null).Tags;

        Assert.Equal(["arms", "barbell", "legs", "shoulders"], tags);
    }

    [Fact]
    public void The_search_box_shows_only_for_more_than_twenty_Exercises()
    {
        var many = Enumerable.Range(0, 21).Select(i => Ex($"e{i}", $"E{i}")).ToList();

        Assert.False(LibraryViewBuilder.Build(null, many.Take(20).ToList(), [], null, null).ShowSearch);
        Assert.True(LibraryViewBuilder.Build(null, many, [], null, null).ShowSearch);
    }

    [Fact]
    public void An_empty_library_is_flagged()
    {
        var view = LibraryViewBuilder.Build(Plan(), [], [], null, null);

        Assert.True(view.IsLibraryEmpty);
        Assert.Empty(view.Groups);
        Assert.Null(view.NewExerciseOffer);
    }

    [Fact]
    public void A_planned_row_for_an_Exercise_not_in_the_library_is_ignored()
    {
        var plan = PlanFixture.Make(sessions: [PlanFixture.Session("s", DayOfWeek.Monday, exercises: [PlanFixture.Row("ghost")])]);

        Assert.Equal("Not in a Session", Assert.Single(LibraryViewBuilder.Build(plan, Library, [], null, null).Groups).Heading);
    }

    [Fact]
    public void Session_group_headings_name_the_Track_when_the_Plan_has_more_than_one()
    {
        var main = new Track("main", "Main", new TrackRhythm.Weekly(2, 1, false), []);
        var mobility = new Track("mob", "Mobility", new TrackRhythm.Daily(), []);
        var plan = PlanFixture.Make(tracks: [main, mobility], sessions:
        [
            PlanFixture.Session("lower", DayOfWeek.Monday, name: "Lower", exercises: [PlanFixture.Row("squat")], trackId: "main"),
            PlanFixture.Session("stretch", DefaultDay.Daily, SessionType.Mobility, name: "Morning", exercises: [PlanFixture.Row("lunge")], trackId: "mob"),
        ]);

        var view = LibraryViewBuilder.Build(plan, Library, [], null, null);

        Assert.Equal(["Main · Lower", "Mobility · Morning", "Not in a Session"], view.Groups.Select(g => g.Heading));
    }
}
