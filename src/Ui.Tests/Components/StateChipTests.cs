using Bunit;
using Ui.Components;

namespace Ui.Tests.Components;

/// <summary>C-07: every state shows a glyph and a word as well as a colour, never colour alone.</summary>
public sealed class StateChipTests : BunitContext
{
    [Theory]
    [InlineData(StateChipKind.Pr, "▲", "PR", "fa-chip--pr")]
    [InlineData(StateChipKind.Done, "✓", "Done", "fa-chip--done")]
    [InlineData(StateChipKind.Partial, "◐", "Partial", "fa-chip--partial")]
    [InlineData(StateChipKind.Skipped, "—", "Skipped", "fa-chip--skipped")]
    [InlineData(StateChipKind.Swapped, "⇄", "Swapped", "fa-chip--swapped")]
    [InlineData(StateChipKind.Pain, "✚", "Pain", "fa-chip--pain")]
    public void Each_state_shows_its_glyph_and_word_with_its_colour_class(StateChipKind kind, string glyph, string word, string css)
    {
        var cut = Render<StateChip>(p => p.Add(c => c.Kind, kind));

        var chip = cut.Find(".fa-chip");
        Assert.Contains(css, chip.ClassList);
        Assert.Equal(glyph, chip.QuerySelector(".fa-chip__glyph")!.TextContent);
        Assert.Equal(word, chip.QuerySelector(".fa-chip__word")!.TextContent);
    }

    [Fact]
    public void The_glyph_is_hidden_from_screen_readers_because_the_word_says_it()
    {
        var cut = Render<StateChip>(p => p.Add(c => c.Kind, StateChipKind.Done));

        Assert.Equal("true", cut.Find(".fa-chip__glyph").GetAttribute("aria-hidden"));
    }
}
