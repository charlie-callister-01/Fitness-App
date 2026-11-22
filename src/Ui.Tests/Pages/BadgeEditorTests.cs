using Bunit;
using Core.Model;
using Core.Storage;
using Core.Tests.Storage;
using Ui.Pages;
using Ui.Saving;
using static Ui.Tests.Pages.PageFixture;

namespace Ui.Tests.Pages;

/// <summary>The Badge editor (design C-22) on the Trophies Badges tab: create, edit, delete with confirm, refusals, a failed save, a read-only app.</summary>
public sealed class BadgeEditorTests : BunitContext
{
    private readonly SaveFailureNotice _notice = new();

    private static readonly Badge Ten = new("ten", "10 Workouts", "sessions", 10);

    private IRenderedComponent<Trophies> Open(AppData data)
    {
        Wire(this, data, _notice);
        JSInterop.SetupModule("./_content/Ui/Components/Sheet.razor.js").Mode = JSRuntimeMode.Loose;
        return Render<Trophies>();
    }

    private static async Task<AppData> SeedAsync(IDataStore? store = null, bool history = true, params Badge[] badges)
    {
        var data = await OpenSeededAsync(store, Plan(), history ? [Finished("w1", Today, Set("a", 20, 5))] : []);
        if (badges.Length > 0) Assert.IsType<CommitResult.Committed>(await data.CommitAsync(badges.Select(Change.Put).ToList()));
        return data;
    }

    private static void ClickButton(IRenderedComponent<Trophies> cut, string text) =>
        cut.FindAll("button").First(b => b.TextContent.Trim() == text).Click();

    private static void ClickInSheet(IRenderedComponent<Trophies> cut, string text) =>
        cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == text).Click();

    private static string Dialog(IRenderedComponent<Trophies> cut) => cut.Find("[role='dialog']").TextContent;

    [Fact]
    public async Task Adding_a_Badge_saves_it_with_a_new_id_and_it_appears_as_a_tile()
    {
        var data = await SeedAsync();
        var cut = Open(data);

        ClickButton(cut, "Add a Badge");
        cut.Find(".fa-badgeeditor__name").Input("Twenty");
        cut.Find(".fa-badgeeditor__target").Input("20");
        ClickInSheet(cut, "Save");

        cut.WaitForAssertion(() => Assert.Single(data.Snapshot.Badges));
        Assert.Equal(new Badge("id-1", "Twenty", "sessions", 20), data.Snapshot.Badges[0]);
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[role='dialog']")));
        Assert.Contains("Twenty", cut.Find(".fa-tile").GetAttribute("aria-label"));
    }

    [Fact]
    public async Task Before_any_history_the_empty_state_still_offers_Add_a_Badge()
    {
        var data = await SeedAsync(history: false);
        var cut = Open(data);

        Assert.Contains("Add a Badge", cut.Find(".empty").TextContent);

        ClickButton(cut, "Add a Badge");

        Assert.NotNull(cut.Find("[role='dialog'] .fa-badgeeditor__name"));
    }

    [Fact]
    public async Task The_metric_choice_shows_its_own_fields()
    {
        var cut = Open(await SeedAsync());
        ClickButton(cut, "Add a Badge");

        Assert.Empty(cut.FindAll(".fa-badgeeditor__exercise"));
        Assert.Empty(cut.FindAll(".fa-badgeeditor__drop"));

        cut.Find(".fa-badgeeditor__metric").Change("BodyWeight");
        Assert.NotNull(cut.Find(".fa-badgeeditor__drop"));

        cut.Find(".fa-badgeeditor__metric").Change("ExerciseBest");
        Assert.NotNull(cut.Find(".fa-badgeeditor__exercise"));
        Assert.Empty(cut.FindAll(".fa-badgeeditor__reps"));

        cut.Find(".fa-badgeeditor__exercise").Change(Squat);
        Assert.NotNull(cut.Find(".fa-badgeeditor__reps"));
    }

    [Fact]
    public async Task The_Exercise_list_leaves_out_Exercises_that_cannot_be_measured()
    {
        var cut = Open(await SeedAsync());
        ClickButton(cut, "Add a Badge");
        cut.Find(".fa-badgeeditor__metric").Change("ExerciseBest");

        var options = cut.FindAll(".fa-badgeeditor__exercise option").Select(o => o.TextContent.Trim()).ToList();

        Assert.Contains("Goblet squat", options);
        Assert.DoesNotContain("Journal", options);
    }

    [Fact]
    public async Task An_Exercise_best_Badge_saves_the_Exercise_id_and_the_weight_times_reps_target()
    {
        var data = await SeedAsync();
        var cut = Open(data);
        ClickButton(cut, "Add a Badge");

        cut.Find(".fa-badgeeditor__name").Input("Squat 24x8");
        cut.Find(".fa-badgeeditor__metric").Change("ExerciseBest");
        cut.Find(".fa-badgeeditor__exercise").Change(Squat);
        cut.Find(".fa-badgeeditor__target").Input("24");
        cut.Find(".fa-badgeeditor__reps").Input("8");
        ClickInSheet(cut, "Save");

        cut.WaitForAssertion(() => Assert.Single(data.Snapshot.Badges));
        Assert.Equal(Squat, data.Snapshot.Badges[0].Metric);
        Assert.Equal(24 * (1 + 8 / 30.0), data.Snapshot.Badges[0].Target, 6);
    }

    [Fact]
    public async Task A_drop_by_weight_Badge_saves_a_negative_target()
    {
        var data = await SeedAsync();
        var cut = Open(data);
        ClickButton(cut, "Add a Badge");

        cut.Find(".fa-badgeeditor__name").Input("Lose 3");
        cut.Find(".fa-badgeeditor__metric").Change("BodyWeight");
        cut.Find(".fa-badgeeditor__target").Input("3");
        cut.Find(".fa-badgeeditor__drop").Change(true);
        ClickInSheet(cut, "Save");

        cut.WaitForAssertion(() => Assert.Single(data.Snapshot.Badges));
        Assert.Equal(new Badge("id-1", "Lose 3", "bodyweight", -3), data.Snapshot.Badges[0]);
    }

    [Fact]
    public async Task The_live_line_shows_where_the_draft_stands()
    {
        var cut = Open(await SeedAsync());
        ClickButton(cut, "Add a Badge");

        cut.Find(".fa-badgeeditor__target").Input("10");

        Assert.Equal("Now: 1. Unlocks when: complete 10 Workouts.", cut.Find(".fa-badgeeditor__live").TextContent.Trim());
    }

    [Fact]
    public async Task A_refused_draft_shows_the_reason_and_saves_nothing()
    {
        var data = await SeedAsync();
        var cut = Open(data);
        ClickButton(cut, "Add a Badge");

        ClickInSheet(cut, "Save");
        Assert.Contains("Give the Badge a name.", Dialog(cut));

        cut.Find(".fa-badgeeditor__name").Input("Zero");
        cut.Find(".fa-badgeeditor__target").Input("0");
        ClickInSheet(cut, "Save");
        Assert.Contains("Enter a target above zero.", Dialog(cut));

        Assert.Empty(data.Snapshot.Badges);
    }

    [Fact]
    public async Task Editing_from_the_detail_replaces_the_Badge_and_keeps_its_id()
    {
        var data = await SeedAsync(badges: [Ten]);
        var cut = Open(data);
        cut.Find(".fa-tile").Click();

        ClickButton(cut, "Edit");
        Assert.Equal("10 Workouts", cut.Find(".fa-badgeeditor__name").GetAttribute("value"));
        Assert.Equal("10", cut.Find(".fa-badgeeditor__target").GetAttribute("value"));
        cut.Find(".fa-badgeeditor__name").Input("Twelve");
        cut.Find(".fa-badgeeditor__target").Input("12");
        ClickInSheet(cut, "Save");

        cut.WaitForAssertion(() => Assert.Equal(new Badge("ten", "Twelve", "sessions", 12), Assert.Single(data.Snapshot.Badges)));
        cut.WaitForAssertion(() => Assert.Contains("Twelve", cut.Find(".fa-badgedetail__name").TextContent));
    }

    [Fact]
    public async Task Deleting_asks_first_and_a_keep_leaves_the_Badge()
    {
        var data = await SeedAsync(badges: [Ten]);
        var cut = Open(data);
        cut.Find(".fa-tile").Click();
        ClickButton(cut, "Edit");

        ClickInSheet(cut, "Delete");
        Assert.Contains("Delete this Badge?", Dialog(cut));
        Assert.Single(data.Snapshot.Badges);

        ClickInSheet(cut, "Keep");

        Assert.DoesNotContain("Delete this Badge?", Dialog(cut));
        Assert.Single(data.Snapshot.Badges);
    }

    [Fact]
    public async Task Confirming_a_delete_removes_the_Badge_and_returns_to_the_grid()
    {
        var data = await SeedAsync(badges: [Ten, new Badge("two", "Two", "sessions", 2)]);
        var cut = Open(data);
        cut.FindAll(".fa-tile").Single(t => t.GetAttribute("aria-label")!.StartsWith("10 Workouts")).Click();
        ClickButton(cut, "Edit");

        ClickInSheet(cut, "Delete");
        ClickInSheet(cut, "Delete Badge");

        cut.WaitForAssertion(() => Assert.Equal(["two"], data.Snapshot.Badges.Select(b => b.Id)));
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".fa-badgedetail")));
    }

    [Fact]
    public async Task A_failed_save_keeps_the_draft_and_shows_the_notice()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        var data = await SeedAsync(store);
        var cut = Open(data);
        ClickButton(cut, "Add a Badge");
        cut.Find(".fa-badgeeditor__name").Input("Twenty");
        cut.Find(".fa-badgeeditor__target").Input("20");
        store.FailAllWrites(StoreFailureKind.StorageFull);

        ClickInSheet(cut, "Save");

        cut.WaitForAssertion(() => Assert.NotNull(_notice.Message));
        Assert.Empty(data.Snapshot.Badges);
        Assert.Equal("Twenty", cut.Find(".fa-badgeeditor__name").GetAttribute("value"));
        Assert.Contains(_notice.Message!, Dialog(cut));
    }

    [Fact]
    public async Task A_read_only_app_disables_Add_and_the_editor_says_why()
    {
        var store = new InMemoryDataStore();
        await (await OpenSeededAsync(store, Plan(), [Finished("w1", Today, Set("a", 20, 5))])).CommitAsync([Change.Put(Ten)]);
        var locked = await AppData.OpenAsync(store, new InMemoryStorageEnvironment { WriterLock = WriterLockResult.HeldElsewhere });
        var cut = Open(locked);

        Assert.True(cut.FindAll("button").Single(b => b.TextContent.Trim() == "Add a Badge").HasAttribute("disabled"));

        cut.Find(".fa-tile").Click();
        ClickButton(cut, "Edit");

        Assert.Contains("read-only", Dialog(cut));
        Assert.True(cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == "Save").HasAttribute("disabled"));
        Assert.True(cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == "Delete").HasAttribute("disabled"));
    }
}
