using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Ui.Accessibility;

namespace Ui.Tests.Accessibility;

/// <summary>A status line shows its text and reads it out once per new Key.</summary>
public sealed class StatusLineTests : BunitContext
{
    private readonly Announcer _announcer = new();
    private readonly List<string> _announced = [];

    public StatusLineTests()
    {
        Services.AddSingleton(_announcer);
        _announcer.Announced += _announced.Add;
    }

    [Fact]
    public void Shows_nothing_and_says_nothing_until_a_status_is_set()
    {
        var cut = Render<StatusLine>(p => p.Add(s => s.Text, null).Add(s => s.Key, 0));

        Assert.Equal("", cut.Markup.Trim());
        Assert.Empty(_announced);
    }

    [Fact]
    public void A_new_Key_shows_the_text_and_announces_it_once_even_when_it_repeats()
    {
        var cut = Render<StatusLine>(p => p.Add(s => s.Text, null).Add(s => s.Key, 0).AddUnmatched("class", "fa-own"));

        cut.Render(p => p.Add(s => s.Text, "Copied.").Add(s => s.Key, 1));
        cut.Render(p => p.Add(s => s.Text, "Copied.").Add(s => s.Key, 1));
        cut.Render(p => p.Add(s => s.Text, "Copied.").Add(s => s.Key, 2));

        Assert.Equal("Copied.", cut.Find("p.fa-own").TextContent);
        Assert.Equal(["Copied.", "Copied."], _announced);
    }

    [Fact]
    public void A_line_created_with_a_status_already_set_does_not_announce_it_again()
    {
        var cut = Render<StatusLine>(p => p.Add(s => s.Text, "Saved.").Add(s => s.Key, 3));

        Assert.Equal("Saved.", cut.Find("p").TextContent);
        Assert.Empty(_announced);
    }

    [Fact]
    public void Child_content_shows_in_place_of_the_plain_line_and_Text_is_what_is_announced()
    {
        var cut = Render<StatusLine>(p => p.Add(s => s.Text, null).Add(s => s.Key, 0)
            .AddChildContent("<div class=\"panel\">Saved.</div>"));

        cut.Render(p => p.Add(s => s.Text, "Plan saved.").Add(s => s.Key, 1)
            .AddChildContent("<div class=\"panel\">Saved.</div>"));

        Assert.Equal("Saved.", cut.Find("div.panel").TextContent);
        Assert.Empty(cut.FindAll("p"));
        Assert.Equal(["Plan saved."], _announced);
    }
}
