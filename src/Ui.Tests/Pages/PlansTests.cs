using Bunit;
using Core.Model;
using Core.Storage;
using Core.Tests.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Ui.Pages;
using Ui.Saving;
using static Ui.Tests.Pages.PageFixture;

namespace Ui.Tests.Pages;

public sealed class PlansTests : BunitContext
{
    private readonly SaveFailureNotice _notice = new();

    private static Phase Ph(string id, string focus, int start, int end) => new(id, focus, start, end, [], []);

    private static Plan Other(string id = "other", string name = "Winter block") =>
        Plan() with
        {
            Id = id,
            Name = name,
            IsActive = false,
            Phases = [Ph("o1", "Base", 1, 6)],
            Sessions = [new("o-lower", "Legs", DayOfWeek.Tuesday, SessionType.Strength, null, [new SessionExercise(Squat, 3, 8, null, null)])],
        };

    private IRenderedComponent<Plans> RenderPlans(AppData data)
    {
        Wire(this, data, _notice);
        JSInterop.SetupModule("./_content/Ui/Components/Sheet.razor.js").Mode = JSRuntimeMode.Loose;
        return Render<Plans>();
    }

    private string CurrentPath => Services.GetRequiredService<NavigationManager>().Uri[Services.GetRequiredService<NavigationManager>().BaseUri.Length..];

    private static async Task<AppData> OpenWithAsync(params Plan[] plans)
    {
        var data = await AppData.OpenAsync(new InMemoryDataStore());
        Assert.IsType<CommitResult.Committed>(await data.CommitAsync([.. PageFixture.Exercises.Select(Change.Put), .. plans.Select(p => Change.Put(p))]));
        return data;
    }

    private static void ClickButton(IRenderedComponent<Plans> cut, string text) =>
        cut.FindAll("button, a.fa-btn").First(b => b.TextContent.Trim() == text).Click();

    private static void ClickInSheet(IRenderedComponent<Plans> cut, string text) =>
        cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == text).Click();

    [Fact]
    public async Task With_no_Plan_the_page_shows_an_empty_state_with_New_Plan()
    {
        var cut = RenderPlans(await OpenWithAsync());

        Assert.Contains("No Plan yet", cut.Markup);
        Assert.Empty(cut.FindAll(".fa-plans__plan"));
        Assert.Single(cut.FindAll("button"), b => b.TextContent.Trim() == "New Plan");
    }

    [Fact]
    public async Task One_Plan_is_outlined_as_active_with_its_week_of_the_total()
    {
        var plan = Plan() with { Phases = [Ph("p1", "Base", 1, 8)] };
        var cut = RenderPlans(await OpenWithAsync(plan));

        var card = Assert.Single(cut.FindAll(".fa-plans__plan"));
        Assert.Contains("fa-plans__plan--active", card.ClassName);
        Assert.Contains("Active · week 1 of 8", card.TextContent);
        Assert.DoesNotContain(card.QuerySelectorAll("button, a"), b => b.TextContent.Trim() == "Make active");
    }

    [Fact]
    public async Task Several_Plans_list_the_active_one_first_and_the_others_with_Make_active()
    {
        var cut = RenderPlans(await OpenWithAsync(Other(), Plan()));

        var cards = cut.FindAll(".fa-plans__plan");
        Assert.Equal(2, cards.Count);
        Assert.Contains("Active · week 1", cards[0].TextContent);
        Assert.Contains("Winter block", cards[1].TextContent);
        Assert.DoesNotContain("Active ·", cards[1].TextContent);
        Assert.Contains(cards[1].QuerySelectorAll("button"), b => b.TextContent.Trim() == "Make active");
    }

    [Fact]
    public async Task Make_active_leaves_exactly_one_active_Plan_in_the_same_save()
    {
        var data = await OpenWithAsync(Plan(), Other());
        var cut = RenderPlans(data);

        cut.Find("button[aria-label='Make Winter block active']").Click();

        cut.WaitForAssertion(() => Assert.True(data.Snapshot.Plans.Single(p => p.Id == "other").IsActive));
        Assert.Equal(["other"], data.Snapshot.Plans.Where(p => p.IsActive).Select(p => p.Id));
        cut.WaitForAssertion(() => Assert.Contains("fa-plans__plan--active", cut.FindAll(".fa-plans__plan")[0].ClassName));
        Assert.Contains("Winter block", cut.FindAll(".fa-plans__plan")[0].TextContent);
    }

    [Fact]
    public async Task A_failed_Make_active_changes_nothing_and_says_so()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        var data = await AppData.OpenAsync(store);
        await data.CommitAsync([.. PageFixture.Exercises.Select(Change.Put), Change.Put(Plan()), Change.Put(Other())]);
        var cut = RenderPlans(data);
        store.FailAllWrites(StoreFailureKind.StorageFull);

        cut.Find("button[aria-label='Make Winter block active']").Click();

        cut.WaitForAssertion(() => Assert.NotNull(_notice.Message));
        Assert.Equal("plan", Assert.Single(data.Snapshot.Plans, p => p.IsActive).Id);
    }

    [Fact]
    public async Task From_scratch_makes_a_new_inactive_Plan_and_opens_it()
    {
        var data = await OpenWithAsync(Plan());
        var cut = RenderPlans(data);

        ClickButton(cut, "New Plan");
        cut.Find("[role='dialog'] input[type='text']").Input("Spring");
        ClickInSheet(cut, "From scratch");

        cut.WaitForAssertion(() => Assert.Equal(2, data.Snapshot.Plans.Count));
        var created = data.Snapshot.Plans.Single(p => p.Name == "Spring");
        Assert.False(created.IsActive);
        Assert.NotEqual("plan", created.Id);
        Assert.Empty(created.Sessions);
        Assert.Equal("plan", Assert.Single(data.Snapshot.Plans, p => p.IsActive).Id);
        Assert.Equal($"plans/{created.Id}", CurrentPath);
    }

    [Fact]
    public async Task Copy_this_Plan_makes_an_inactive_copy_with_new_ids()
    {
        var data = await OpenWithAsync(Plan());
        var cut = RenderPlans(data);

        ClickButton(cut, "New Plan");
        cut.Find("[role='dialog'] input[type='text']").Input("Copy of Plan");
        ClickInSheet(cut, "Copy this Plan");

        cut.WaitForAssertion(() => Assert.Equal(2, data.Snapshot.Plans.Count));
        var copy = data.Snapshot.Plans.Single(p => p.Name == "Copy of Plan");
        Assert.False(copy.IsActive);
        Assert.NotEqual("plan", copy.Id);
        Assert.Equal("Lower", Assert.Single(copy.Sessions).Name);
        Assert.NotEqual("lower", copy.Sessions[0].Id);
        Assert.Equal(Plan().Sessions[0].Exercises, copy.Sessions[0].Exercises);
    }

    [Fact]
    public async Task A_blank_name_is_refused_in_the_sheet_and_nothing_is_made()
    {
        var data = await OpenWithAsync(Plan());
        var cut = RenderPlans(data);

        ClickButton(cut, "New Plan");
        ClickInSheet(cut, "From scratch");
        Assert.Contains("Give the Plan a name.", cut.Find("[role='dialog']").TextContent);
        ClickInSheet(cut, "Copy this Plan");

        Assert.Contains("Give the Plan a name.", cut.Find("[role='dialog']").TextContent);
        Assert.Single(data.Snapshot.Plans);
    }

    [Fact]
    public async Task A_read_only_window_disables_Make_active_and_New_Plan_and_says_why()
    {
        var store = new InMemoryDataStore();
        await (await AppData.OpenAsync(store)).CommitAsync([.. PageFixture.Exercises.Select(Change.Put), Change.Put(Plan()), Change.Put(Other())]);
        var locked = await AppData.OpenAsync(store, new InMemoryStorageEnvironment { WriterLock = WriterLockResult.HeldElsewhere });
        var cut = RenderPlans(locked);

        Assert.Contains("read-only", cut.Markup);
        Assert.True(cut.Find("button[aria-label='Make Winter block active']").HasAttribute("disabled"));
        Assert.True(cut.FindAll("button").Single(b => b.TextContent.Trim() == "New Plan").HasAttribute("disabled"));
    }
}
