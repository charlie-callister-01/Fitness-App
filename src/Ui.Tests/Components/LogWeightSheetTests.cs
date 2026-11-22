using Bunit;
using Ui.Components;

namespace Ui.Tests.Components;

public sealed class LogWeightSheetTests : BunitContext
{
    public LogWeightSheetTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupModule("./_content/Ui/Components/Sheet.razor.js").Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<LogWeightSheet> Open(Action<ComponentParameterCollectionBuilder<LogWeightSheet>>? more = null) =>
        Render<LogWeightSheet>(p =>
        {
            p.Add(s => s.Open, true).Add(s => s.InitialText, "86.3").Add(s => s.LastText, "Last: 86.3 kg on Sun 15 Nov").Add(s => s.UnitLabel, "kg");
            more?.Invoke(p);
        });

    private static AngleSharp.Dom.IElement SaveButton(IRenderedComponent<LogWeightSheet> cut) =>
        cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == "Save");

    [Fact]
    public void The_field_is_prefilled_labelled_with_the_unit_and_decimal()
    {
        var cut = Open();

        var input = cut.Find("input");
        Assert.Equal("86.3", input.GetAttribute("value"));
        Assert.Equal("decimal", input.GetAttribute("inputmode"));
        Assert.Contains("Weight (kg)", cut.Markup);
        Assert.Contains("Last: 86.3 kg on Sun 15 Nov", cut.Markup);
        Assert.Contains("Log weight", cut.Find("[role='dialog'] h2").TextContent);
    }

    [Fact]
    public void Save_raises_OnSave_with_the_text_as_typed()
    {
        string? saved = null;
        var cut = Open(p => p.Add(s => s.OnSave, t => saved = t));
        cut.Find("input").Input("190,5");

        SaveButton(cut).Click();

        cut.WaitForAssertion(() => Assert.Equal("190,5", saved));
    }

    [Fact]
    public void A_message_is_shown_as_an_alert()
    {
        var cut = Open(p => p.Add(s => s.Message, "Enter your weight as a number."));

        Assert.Contains("Enter your weight as a number.", cut.Find("[role='alert']").TextContent);
    }

    [Fact]
    public void Save_is_disabled_while_saving()
    {
        var cut = Open(p => p.Add(s => s.Saving, true));

        Assert.True(SaveButton(cut).HasAttribute("disabled"));
    }

    [Fact]
    public void Save_is_disabled_when_read_only_and_the_reason_is_shown()
    {
        var cut = Open(p => p.Add(s => s.CanWrite, false).Add(s => s.ReadOnlyMessage, "Open in one window to save."));

        Assert.True(SaveButton(cut).HasAttribute("disabled"));
        Assert.Contains("Open in one window to save.", cut.Markup);
    }

    [Fact]
    public void Cancel_raises_OnClose()
    {
        var closed = false;
        var cut = Open(p => p.Add(s => s.OnClose, () => closed = true));

        cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == "Cancel").Click();

        cut.WaitForAssertion(() => Assert.True(closed));
    }
}
