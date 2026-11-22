using Bunit;
using Ui.Components;
using LineModel = Core.Rules.Home.WeekLine;

namespace Ui.Tests.Components;

public sealed class WeekLineTests : BunitContext
{
    [Fact]
    public void Shows_progress_and_target()
    {
        var cut = Render<Ui.Components.WeekLine>(p => p.Add(l => l.Line, new LineModel(2, 4, 3, [])));

        Assert.Equal("This week 2 of 4 · target 3", cut.Find("p").TextContent.Trim());
    }

    [Fact]
    public void Lists_Missed_codes_when_there_are_some()
    {
        var cut = Render<Ui.Components.WeekLine>(p => p.Add(l => l.Line, new LineModel(1, 4, 3, ["A", "C"])));

        Assert.Equal("This week 1 of 4 · target 3 · Missed: A, C", cut.Find("p").TextContent.Trim());
    }
}
