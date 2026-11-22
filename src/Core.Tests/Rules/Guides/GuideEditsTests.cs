using Core.Model;
using Core.Rules.Guides;

namespace Core.Tests.Rules.Guides;

public class GuideEditsTests
{
    private static GuideSections Sections(
        string? works = null, string? how = null, string[]? cues = null, string? video = null, string[]? watchOuts = null) =>
        new(works, null, null, how, cues ?? [], [], null, null, watchOuts ?? [], video);

    private static Guide Saved(GuideEditResult result) => Assert.IsType<GuideEditResult.Valid>(result).Guide;

    private static Guide Existing() =>
        new("squat", "Legs", "Strength", "Bar on back", "Sit down and up", ["Chest up"], ["Knees cave"], "Box squat", "Pause squat",
            ["Back pain"], "https://example.com/v", "Left knee: keep the box high");

    [Fact]
    public void Saving_sections_creates_the_Guide_when_there_is_none()
    {
        var guide = Saved(GuideEdits.WithSections(null, "squat", Sections(works: "Legs", cues: ["Chest up"])));

        Assert.Equal("squat", guide.ExerciseId);
        Assert.Equal("Legs", guide.Works);
        Assert.Equal(["Chest up"], guide.Cues);
        Assert.Null(guide.PersonalNotes);
    }

    [Fact]
    public void An_empty_section_is_stored_as_absent_and_text_is_trimmed()
    {
        var guide = Saved(GuideEdits.WithSections(Existing(), "squat", Sections(works: "   ", how: "  Sit down  ")));

        Assert.Null(guide.Works);
        Assert.Equal("Sit down", guide.HowTo);
        Assert.Null(guide.Setup);
    }

    [Fact]
    public void Blank_list_items_are_dropped_and_an_empty_list_is_kept_empty()
    {
        var guide = Saved(GuideEdits.WithSections(Existing(), "squat", Sections(cues: [" Chest up ", "", "  "], watchOuts: [])));

        Assert.Equal(["Chest up"], guide.Cues);
        Assert.Empty(guide.WatchOuts);
        Assert.Empty(guide.CommonMistakes);
    }

    [Fact]
    public void Saving_sections_keeps_the_personal_notes()
    {
        var guide = Saved(GuideEdits.WithSections(Existing(), "squat", Sections(works: "New")));

        Assert.Equal("Left knee: keep the box high", guide.PersonalNotes);
    }

    [Fact]
    public void Saving_notes_keeps_every_section()
    {
        var existing = Existing();

        var guide = GuideEdits.WithNotes(existing, "squat", "  Use the low box  ");

        Assert.Equal("Use the low box", guide.PersonalNotes);
        Assert.Equal(existing with { PersonalNotes = "Use the low box" }, guide);
    }

    [Fact]
    public void Saving_notes_creates_the_Guide_when_there_is_none_and_empty_notes_are_absent()
    {
        var created = GuideEdits.WithNotes(null, "squat", "A note");
        var cleared = GuideEdits.WithNotes(created, "squat", " ");

        Assert.Equal("squat", created.ExerciseId);
        Assert.Equal("A note", created.PersonalNotes);
        Assert.Null(created.Works);
        Assert.Null(cleared.PersonalNotes);
    }

    [Theory]
    [InlineData("https://example.com/watch?v=1")]
    [InlineData("http://example.com")]
    [InlineData("  https://example.com  ")]
    public void A_web_address_is_accepted_as_the_video_link(string link)
    {
        var guide = Saved(GuideEdits.WithSections(null, "squat", Sections(video: link)));

        Assert.Equal(link.Trim(), guide.VideoUrl);
    }

    [Theory]
    [InlineData("example.com/video")]
    [InlineData("ftp://example.com/video")]
    [InlineData("javascript:alert(1)")]
    [InlineData("not a link")]
    [InlineData("https://")]
    public void Anything_that_is_not_an_http_or_https_address_is_refused(string link)
    {
        var result = GuideEdits.WithSections(Existing(), "squat", Sections(video: link));

        Assert.Contains("video link", Assert.IsType<GuideEditResult.Invalid>(result).Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void An_empty_video_link_is_allowed_and_stored_as_absent(string? link) =>
        Assert.Null(Saved(GuideEdits.WithSections(Existing(), "squat", Sections(video: link))).VideoUrl);

    [Fact]
    public void The_sections_of_a_Guide_round_trip_through_the_editor()
    {
        var existing = Existing();

        var saved = Saved(GuideEdits.WithSections(existing, "squat", GuideEdits.SectionsOf(existing)));

        Assert.Equal(GuideEdits.SectionsOf(existing).Cues, saved.Cues);
        Assert.Equal(existing with { Cues = saved.Cues, CommonMistakes = saved.CommonMistakes, WatchOuts = saved.WatchOuts }, saved);
    }

    [Fact]
    public void There_are_no_sections_without_a_Guide()
    {
        var sections = GuideEdits.SectionsOf(null);

        Assert.Null(sections.Works);
        Assert.Empty(sections.Cues);
        Assert.Null(sections.VideoUrl);
    }
}
