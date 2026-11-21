using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Ui.Components;

namespace Ui.Tests.Components;

/// <summary>The shared Sheet shell: a labelled modal dialog that closes by Escape or an outside tap, and hands focus in and back out.</summary>
public sealed class SheetTests : BunitContext
{
    private const string Module = "./_content/Ui/Components/Sheet.razor.js";

    private readonly BunitJSModuleInterop _module;
    private int _closed;

    public SheetTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        _module = JSInterop.SetupModule(Module);
        _module.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<Sheet> RenderSheet(bool open = true) =>
        Render<Sheet>(p => p
            .Add(s => s.Open, open)
            .Add(s => s.Title, "Goblet squat")
            .Add(s => s.OnClose, EventCallback.Factory.Create(this, () => _closed++))
            .AddChildContent("<p class=\"body-text\">Pick one</p>")
            .Add(s => s.Actions, "<button class=\"action\">Cancel</button>"));

    [Fact]
    public void A_closed_sheet_renders_nothing()
    {
        var cut = RenderSheet(open: false);

        Assert.Empty(cut.FindAll("[role='dialog']"));
        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public void An_open_sheet_is_a_modal_dialog_labelled_by_its_title()
    {
        var cut = RenderSheet();

        var dialog = cut.Find("[role='dialog']");
        Assert.Equal("true", dialog.GetAttribute("aria-modal"));
        var title = cut.Find(".fa-sheet__title");
        Assert.Equal("Goblet squat", title.TextContent);
        Assert.Equal(title.Id, dialog.GetAttribute("aria-labelledby"));
        Assert.False(string.IsNullOrEmpty(title.Id));
    }

    [Fact]
    public void The_body_and_the_actions_are_shown_in_their_own_places()
    {
        var cut = RenderSheet();

        Assert.Equal("Pick one", cut.Find(".fa-sheet__body .body-text").TextContent);
        Assert.Equal("Cancel", cut.Find(".fa-sheet__actions .action").TextContent);
    }

    [Fact]
    public void Opening_asks_the_browser_to_move_focus_into_the_sheet()
    {
        RenderSheet();

        _module.VerifyInvoke("attach");
    }

    [Fact]
    public void Closing_asks_the_browser_to_return_focus_to_the_opener()
    {
        var cut = RenderSheet();

        cut.Render(p => p.Add(s => s.Open, false));

        _module.VerifyInvoke("detach");
    }

    [Fact]
    public void Escape_closes_the_sheet()
    {
        var cut = RenderSheet();

        cut.Find("[role='dialog']").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Equal(1, _closed);
    }

    [Fact]
    public void Other_keys_do_not_close_the_sheet()
    {
        var cut = RenderSheet();

        cut.Find("[role='dialog']").KeyDown(new KeyboardEventArgs { Key = "a" });

        Assert.Equal(0, _closed);
    }

    [Fact]
    public void Tapping_outside_the_sheet_closes_it()
    {
        var cut = RenderSheet();

        cut.Find(".fa-sheet__backdrop").Click();

        Assert.Equal(1, _closed);
    }
}
