using Core.Rules.Finishing;

namespace Core.Tests.Rules.Finishing;

public class PainNoteTests
{
    [Fact]
    public void The_sides_are_left_right_and_both()
    {
        Assert.Equal(["Left side", "Right side", "Both sides"], PainNote.Sides);
    }

    [Theory]
    [InlineData("Left side", "tight", "Left side: tight")]
    [InlineData("Right side", null, "Right side")]
    [InlineData(null, "  tight ", "tight")]
    [InlineData("Both sides", "  ", "Both sides")]
    public void Compose_writes_the_side_first_and_trims_the_note(string? side, string? note, string expected)
    {
        Assert.Equal(expected, PainNote.Compose(side, note));
    }

    [Fact]
    public void Compose_with_neither_is_null()
    {
        Assert.Null(PainNote.Compose(null, " "));
    }

    [Theory]
    [InlineData("Left side: tight", "Left side", "tight")]
    [InlineData("Right side", "Right side", null)]
    [InlineData("tight", null, "tight")]
    [InlineData("both sides: a: b", "Both sides", "a: b")]
    public void Parse_reads_side_and_note_back(string text, string? side, string? note)
    {
        var parsed = PainNote.Parse(text);

        Assert.Equal(side, parsed.Side);
        Assert.Equal(note, parsed.Text);
    }

    [Fact]
    public void Parse_of_nothing_is_empty()
    {
        var parsed = PainNote.Parse(null);

        Assert.Null(parsed.Side);
        Assert.Null(parsed.Text);
    }

    [Theory]
    [InlineData("Left side", "tight")]
    [InlineData("Both sides", null)]
    [InlineData(null, "tight")]
    [InlineData(null, null)]
    public void Compose_then_Parse_round_trips(string? side, string? note)
    {
        var parsed = PainNote.Parse(PainNote.Compose(side, note));

        Assert.Equal(side, parsed.Side);
        Assert.Equal(note, parsed.Text);
    }
}
