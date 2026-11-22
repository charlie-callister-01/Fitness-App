using Bunit;
using Core.Model;
using Core.Storage;
using Core.Tests.Storage;
using Microsoft.Extensions.DependencyInjection;
using Ui.Pages;
using Ui.Saving;
using static Ui.Tests.Pages.PageFixture;

namespace Ui.Tests.Pages;

/// <summary>The Weight screen (design S-08): stats, weight chart, weekly averages and Log weight. Fixed today is Mon 16 Nov 2026.</summary>
public sealed class WeightTests : BunitContext
{
    private readonly SaveFailureNotice _notice = new();

    private IRenderedComponent<Weight> RenderWeight(AppData data)
    {
        Wire(this, data, _notice);
        return Render<Weight>();
    }

    private static async Task<AppData> SeededAsync(IDataStore? store = null, params WeighIn[] weighIns)
    {
        var data = await OpenSeededAsync(store, Plan());
        if (weighIns.Length > 0)
        {
            await data.CommitAsync(weighIns.Select(w => Change.Put(w)).ToList());
        }

        return data;
    }

    private static void ClickSave(IRenderedComponent<Weight> cut) =>
        cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == "Save").Click();

    [Fact]
    public async Task With_no_Weigh_ins_it_shows_an_empty_state_with_Log_weight_that_opens_the_sheet()
    {
        var cut = RenderWeight(await SeededAsync());

        Assert.Contains("No Weigh-ins yet", cut.Find(".empty").TextContent);
        Assert.Empty(cut.FindAll("svg.fa-chart"));
        cut.Find(".empty button").Click();

        Assert.NotEmpty(cut.FindAll("[role='dialog']"));
    }

    [Fact]
    public async Task Populated_it_shows_the_three_stats_the_chart_and_the_weekly_table()
    {
        var cut = RenderWeight(await SeededAsync(null,
            new WeighIn("a", Today.AddDays(-9), 91), new WeighIn("b", Today.AddDays(-2), 90), new WeighIn("c", Today, 88)));

        Assert.Equal("88.0 kg", cut.Find(".fa-weight__now").TextContent.Trim());
        Assert.Equal("-3.0 kg", cut.Find(".fa-weight__change").TextContent.Trim());
        Assert.Equal("88.0 kg", cut.Find(".fa-weight__average").TextContent.Trim());
        Assert.Equal(3, cut.FindAll("svg.fa-chart .fa-chart__point").Count);
        Assert.Single(cut.FindAll("svg.fa-chart .fa-chart__ring"));
        Assert.Empty(cut.FindAll("svg.fa-chart .fa-chart__goal"));
        Assert.Equal(3, cut.FindAll("section[aria-label='Weekly averages'] tbody tr").Count);
    }

    [Fact]
    public async Task A_weight_Badge_draws_the_goal_line()
    {
        var data = await SeededAsync(null, new WeighIn("a", Today.AddDays(-3), 91), new WeighIn("b", Today, 90));
        await data.CommitAsync(Change.Put(new Badge("g", "Goal", "bodyweight", 85)));

        var cut = RenderWeight(data);

        Assert.Single(cut.FindAll("svg.fa-chart .fa-chart__goal"));
    }

    [Fact]
    public async Task The_Table_toggle_lists_the_Weigh_ins_and_Chart_returns()
    {
        var cut = RenderWeight(await SeededAsync(null, new WeighIn("a", Today.AddDays(-3), 91), new WeighIn("b", Today, 90)));

        cut.Find(".fa-chart-card button").Click();
        Assert.Empty(cut.FindAll("svg.fa-chart"));
        Assert.Equal(2, cut.FindAll(".fa-chart-card tbody tr").Count);
        cut.Find(".fa-chart-card button").Click();

        Assert.NotEmpty(cut.FindAll("svg.fa-chart"));
    }

    [Fact]
    public async Task Log_weight_adds_a_new_Weigh_in_even_on_a_day_that_has_one()
    {
        var data = await SeededAsync(null, new WeighIn("a", Today, 90));
        var cut = RenderWeight(data);

        cut.Find(".fa-weight__header button").Click();
        Assert.Equal("90.0", cut.Find("[role='dialog'] input").GetAttribute("value"));
        cut.Find("[role='dialog'] input").Input("89.5");
        ClickSave(cut);

        cut.WaitForAssertion(() => Assert.Equal(2, data.Snapshot.WeighIns.Count));
        cut.WaitForAssertion(() => Assert.Equal("89.5 kg", cut.Find(".fa-weight__now").TextContent.Trim()));
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[role='dialog']")));
    }

    [Fact]
    public async Task A_failed_save_keeps_the_sheet_open_and_reports_the_failure()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        var data = await SeededAsync(store);
        var cut = RenderWeight(data);
        cut.Find(".fa-weight__header button").Click();
        cut.Find("[role='dialog'] input").Input("86");
        store.FailAllWrites(StoreFailureKind.StorageFull);

        ClickSave(cut);

        Assert.Empty(data.Snapshot.WeighIns);
        Assert.Contains("out of storage", _notice.Message);
        Assert.NotEmpty(cut.FindAll("[role='dialog']"));
    }

    [Fact]
    public async Task Read_only_disables_Save_and_writes_nothing()
    {
        var store = new InMemoryDataStore();
        await OpenSeededAsync(store, Plan());
        var locked = await AppData.OpenAsync(store, new InMemoryStorageEnvironment { WriterLock = WriterLockResult.HeldElsewhere });
        var cut = RenderWeight(locked);

        cut.Find(".fa-weight__header button").Click();

        var save = cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == "Save");
        Assert.True(save.HasAttribute("disabled"));
        Assert.Empty(locked.Snapshot.WeighIns);
    }
}
