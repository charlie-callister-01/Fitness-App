using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Ui.Accessibility;

namespace Ui.Tests.Accessibility;

/// <summary>The shared polite live region and the announcer every screen speaks through.</summary>
public sealed class AnnouncerTests : BunitContext
{
    private readonly Announcer _announcer = new();

    public AnnouncerTests() => Services.AddSingleton(_announcer);

    [Fact]
    public void The_region_is_present_polite_and_empty_before_anything_is_announced()
    {
        var cut = Render<LiveRegion>();

        var region = cut.Find("[data-testid='live-region']");
        Assert.Equal("polite", region.GetAttribute("aria-live"));
        Assert.Equal("status", region.GetAttribute("role"));
        Assert.Equal("", region.TextContent);
    }

    [Fact]
    public void An_announced_message_appears_in_the_region()
    {
        var cut = Render<LiveRegion>();

        _announcer.Announce("Badge unlocked: Twice over");

        cut.WaitForAssertion(() => Assert.Equal("Badge unlocked: Twice over", cut.Find("[data-testid='live-region']").TextContent));
    }

    [Fact]
    public void The_same_message_again_clears_the_region_then_sets_it_so_it_is_read_out_again()
    {
        var cut = Render<LiveRegion>();
        _announcer.Announce("Rest over");
        cut.WaitForAssertion(() => Assert.Equal("Rest over", cut.Find("[data-testid='live-region']").TextContent));

        _announcer.Announce("Rest over");

        cut.WaitForAssertion(() => Assert.Equal("", cut.Find("[data-testid='live-region']").TextContent));
        cut.WaitForAssertion(() => Assert.Equal("Rest over", cut.Find("[data-testid='live-region']").TextContent));
    }

    [Fact]
    public void Each_announcement_is_raised_even_when_the_message_repeats()
    {
        var raised = 0;
        _announcer.Announced += () => raised++;

        _announcer.Announce("Copied.");
        _announcer.Announce("Copied.");

        Assert.Equal(2, raised);
        Assert.Equal("Copied.", _announcer.Message);
    }

    [Fact]
    public void A_blank_message_is_not_announced()
    {
        var raised = 0;
        _announcer.Announced += () => raised++;

        _announcer.Announce("  ");

        Assert.Equal(0, raised);
        Assert.Null(_announcer.Message);
    }
}
