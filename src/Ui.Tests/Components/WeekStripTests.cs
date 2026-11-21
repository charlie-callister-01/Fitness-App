using Bunit;
using Core.Model;
using Core.Rules.Home;
using Ui.Components;

namespace Ui.Tests.Components;

public sealed class WeekStripTests : BunitContext
{
    private static readonly DateOnly Monday = new(2026, 11, 16);
    private static readonly Session A = new("a", "A: Lower", DayOfWeek.Monday, SessionType.Strength, null, []);
    private static readonly Session B = new("b", "B: Run", DayOfWeek.Wednesday, SessionType.Cardio, null, []);

    private static WeekTile Tile(int offset, Session? session, TileState state, bool today = false) =>
        new(Monday.AddDays(offset), Monday.AddDays(offset).ToString("ddd", System.Globalization.CultureInfo.InvariantCulture),
            today, session, session is null ? null : HomeWeekRules.SessionCode(session), session?.Type, state);

    private static IReadOnlyList<WeekTile> Week() =>
    [
        Tile(0, A, TileState.Done),
        Tile(1, null, TileState.Rest),
        Tile(2, B, TileState.Upcoming, today: true),
        Tile(3, A with { Id = "c", Name = "C: Body", Type = SessionType.Body }, TileState.Missed),
        Tile(4, B with { Id = "d", Name = "D: Test", Type = SessionType.Test }, TileState.Partial),
        Tile(5, null, TileState.BreakDay),
        Tile(6, null, TileState.Rest),
    ];

    [Fact]
    public void Seven_tiles_render_in_order_with_glyphs()
    {
        var cut = Render<WeekStrip>(p => p.Add(s => s.Tiles, Week()));

        var tiles = cut.FindAll(".fa-weekstrip__tile");
        Assert.Equal(7, tiles.Count);
        Assert.Equal(["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"],
            tiles.Select(t => t.QuerySelector(".fa-weekstrip__day")!.TextContent.Trim()));
        Assert.Equal(["✓", "", "", "○", "◐", "–", ""],
            tiles.Select(t => t.QuerySelector(".fa-weekstrip__glyph")!.TextContent.Trim()));
    }

    [Fact]
    public void Each_tile_has_an_aria_label_with_the_state_in_words()
    {
        var cut = Render<WeekStrip>(p => p.Add(s => s.Tiles, Week()));

        Assert.Equal(
            [
                "Monday, Session A, done",
                "Tuesday, rest day",
                "Today, Wednesday, Session B, upcoming",
                "Thursday, Session C, missed",
                "Friday, Session D, partial",
                "Saturday, Break day",
                "Sunday, rest day",
            ],
            cut.FindAll(".fa-weekstrip__tile").Select(t => t.GetAttribute("aria-label")));
    }

    [Fact]
    public void Only_today_is_outlined_and_codes_are_coloured_by_type()
    {
        var cut = Render<WeekStrip>(p => p.Add(s => s.Tiles, Week()));

        Assert.Single(cut.FindAll(".fa-weekstrip__tile--today"));
        Assert.NotNull(cut.Find(".fa-weekstrip__code--strength"));
        Assert.NotNull(cut.Find(".fa-weekstrip__code--cardio"));
        Assert.NotNull(cut.Find(".fa-weekstrip__code--body"));
        Assert.NotNull(cut.Find(".fa-weekstrip__code--test"));
    }

    [Fact]
    public void Tapping_a_tile_raises_OnOpenDay_with_its_date()
    {
        DateOnly? opened = null;
        var cut = Render<WeekStrip>(p => p.Add(s => s.Tiles, Week()).Add(s => s.OnOpenDay, d => opened = d));

        cut.FindAll(".fa-weekstrip__tile")[3].Click();

        cut.WaitForAssertion(() => Assert.Equal(Monday.AddDays(3), opened));
    }
}
