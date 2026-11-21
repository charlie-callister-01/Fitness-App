using Bunit;
using Ui.Components;

namespace Ui.Tests.Components;

/// <summary>The sheets that write (Pain check, Partial prompt, Logged afterwards) wait when the window cannot save, not only while a save runs.</summary>
public sealed class SheetButtonsReadOnlyTests : BunitContext
{
    public SheetButtonsReadOnlyTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupModule("./_content/Ui/Components/Sheet.razor.js").Mode = JSRuntimeMode.Loose;
    }

    private static bool Disabled(IRenderedComponent<PainCheckSheet> cut, string label) => IsDisabled(cut.FindAll("[role='dialog'] button"), label);

    private static bool Disabled(IRenderedComponent<PartialPrompt> cut, string label) => IsDisabled(cut.FindAll("[role='dialog'] button"), label);

    private static bool Disabled(IRenderedComponent<LoggedAfterwardsSheet> cut, string label) => IsDisabled(cut.FindAll("[role='dialog'] button"), label);

    private static bool IsDisabled(IEnumerable<AngleSharp.Dom.IElement> buttons, string label) =>
        buttons.Single(b => b.TextContent.Trim() == label).HasAttribute("disabled");

    [Fact]
    public void The_Pain_check_No_is_disabled_when_the_window_cannot_write_and_Skip_is_not()
    {
        var cut = Render<PainCheckSheet>(p => p.Add(s => s.Open, true).Add(s => s.CanWrite, false));

        Assert.True(Disabled(cut, "No"));
        Assert.False(Disabled(cut, "Skip"));
    }

    [Fact]
    public void The_Pain_check_Save_is_disabled_when_the_window_cannot_write()
    {
        var cut = Render<PainCheckSheet>(p => p.Add(s => s.Open, true).Add(s => s.CanWrite, true));
        cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == "Yes").Click();
        Assert.False(Disabled(cut, "Save and see summary"));

        cut.Render(p => p.Add(s => s.Open, true).Add(s => s.CanWrite, false));

        Assert.True(Disabled(cut, "Save and see summary"));
    }

    [Fact]
    public void The_Partial_prompt_Finish_is_disabled_when_the_window_cannot_write_and_Keep_going_is_not()
    {
        var cut = Render<PartialPrompt>(p => p.Add(s => s.Open, true).Add(s => s.CanWrite, false));

        Assert.True(Disabled(cut, "Finish"));
        Assert.False(Disabled(cut, "Keep going"));
    }

    [Fact]
    public void The_Logged_afterwards_Save_date_is_disabled_when_the_window_cannot_write_and_Cancel_is_not()
    {
        var cut = Render<LoggedAfterwardsSheet>(p => p.Add(s => s.Open, true).Add(s => s.Today, new DateOnly(2026, 11, 16)).Add(s => s.CanWrite, false));

        Assert.True(Disabled(cut, "Save date"));
        Assert.False(Disabled(cut, "Cancel"));
    }
}
