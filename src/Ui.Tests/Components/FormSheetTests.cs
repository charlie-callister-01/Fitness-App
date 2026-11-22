using Bunit;
using Ui.Components;

namespace Ui.Tests.Components;

/// <summary>The shared sheet frame: the read-only well, the error message, Cancel and the rule for when write buttons wait.</summary>
public sealed class FormSheetTests : BunitContext
{
    public FormSheetTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupModule("./_content/Ui/Components/Sheet.razor.js").Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<FormSheet> Open(Action<ComponentParameterCollectionBuilder<FormSheet>>? more = null) =>
        Render<FormSheet>(p =>
        {
            p.Add(s => s.Open, true).Add(s => s.Title, "A form");
            p.Add(s => s.Actions, state => builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "type", "button");
                builder.AddAttribute(2, "disabled", state.WriteDisabled);
                builder.AddContent(3, "Write");
                builder.CloseElement();
            });
            more?.Invoke(p);
        });

    private static bool WriteDisabled(IRenderedComponent<FormSheet> cut) =>
        cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == "Write").HasAttribute("disabled");

    [Fact]
    public void A_window_that_can_write_has_an_enabled_write_button_and_no_well()
    {
        var cut = Open();

        Assert.False(WriteDisabled(cut));
        Assert.Empty(cut.FindAll(".fa-well"));
    }

    [Fact]
    public void While_saving_the_write_button_and_Cancel_wait()
    {
        var cut = Open(p => p.Add(s => s.Saving, true));

        Assert.True(WriteDisabled(cut));
        Assert.True(cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == "Cancel").HasAttribute("disabled"));
    }

    [Fact]
    public void A_read_only_window_says_why_once_in_a_status_well_and_disables_writing_but_not_Cancel()
    {
        var cut = Open(p => p.Add(s => s.CanWrite, false).Add(s => s.ReadOnlyMessage, "Open in one window to save."));

        var well = Assert.Single(cut.FindAll(".fa-well"));
        Assert.Equal("status", well.GetAttribute("role"));
        Assert.Equal("Open in one window to save.", well.TextContent);
        Assert.True(WriteDisabled(cut));
        Assert.False(cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == "Cancel").HasAttribute("disabled"));
    }

    [Fact]
    public void A_message_shows_as_an_alert_with_the_sheets_own_class()
    {
        var cut = Open(p => p.Add(s => s.Message, "Enter a number.").Add(s => s.MessageClass, "fa-own"));

        var alert = cut.Find("[role='alert']");
        Assert.Equal("Enter a number.", alert.TextContent);
        Assert.Contains("fa-own", alert.ClassName);
    }

    [Fact]
    public void Cancel_and_the_label_are_the_sheets_choice_and_Cancel_asks_to_close()
    {
        var closed = 0;
        var cut = Open(p => p.Add(s => s.CancelLabel, "Keep going").Add(s => s.OnClose, () => closed++));

        cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == "Keep going").Click();

        Assert.Equal(1, closed);
        Assert.DoesNotContain(cut.FindAll("[role='dialog'] button"), b => b.TextContent.Trim() == "Cancel");
    }

    [Fact]
    public void An_empty_Cancel_label_means_no_Cancel_button()
    {
        var cut = Open(p => p.Add(s => s.CancelLabel, ""));

        Assert.DoesNotContain(cut.FindAll("[role='dialog'] button"), b => b.TextContent.Trim() == "Cancel");
    }
}
