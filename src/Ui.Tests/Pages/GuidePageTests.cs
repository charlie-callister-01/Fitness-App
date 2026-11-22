using AngleSharp.Dom;
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

/// <summary>The Guide page (design S-03, F-09): read view, editor, notes, discard prompt, video and the way back to the log.</summary>
public sealed class GuidePageTests : BunitContext
{
    private const string SheetModule = "./_content/Ui/Components/Sheet.razor.js";
    private const string VideoModule = "./_content/Ui/Pages/GuidePage.razor.js";
    private const string Video = "https://example.com/squat";

    private readonly SaveFailureNotice _notice = new();

    private static Guide SquatGuide(string? notes = null) =>
        new(Squat, "Quads and glutes", "Builds the leg drive", "Feet shoulder width", "Sit between the heels", ["Knees out", "Brace first"],
            ["Heels lifting"], "Hold a light weight", "Add a pause", ["Back pain: stop"], Video, notes);

    private static Workout Past(string id, DateOnly date, double kg, int reps) => Finished(id, date, Set($"{id}-s", kg, reps));

    private async Task<IRenderedComponent<GuidePage>> RenderPageAsync(
        Guide? guide = null, IDataStore? store = null, string exerciseId = Squat, AppData? data = null, params Workout[] workouts)
    {
        data ??= await OpenSeededAsync(store, Plan(), workouts);
        if (guide is not null)
        {
            Assert.IsType<CommitResult.Committed>(await data.CommitAsync([Change.Put(guide)]));
        }

        Wire(this, data, _notice);
        JSInterop.SetupModule(SheetModule);
        return Render<GuidePage>(p => p.Add(g => g.ExerciseId, exerciseId));
    }

    private static IElement ButtonNamed(IRenderedComponent<GuidePage> cut, string text) =>
        cut.FindAll("button").Single(b => b.TextContent.Trim() == text);

    private static IElement Area(IRenderedComponent<GuidePage> cut, string id) => cut.Find("#" + id);

    // ---- Read view

    [Fact]
    public async Task The_read_view_shows_the_sections_in_the_designs_order_then_the_demo_notes_history_and_place()
    {
        var cut = await RenderPageAsync(SquatGuide("Left knee: keep the box high"), workouts: Past("w1", Today.AddDays(-7), 40, 8));

        var text = cut.Find(".fa-root").TextContent;
        var order = new[]
        {
            "What it works", "Why it's in your plan", "Setup", "How to do it", "Cues", "Common mistakes", "Easier", "Harder", "Watch-outs",
            "Watch a demo", "My notes", "Your history", "In your Plan",
        };
        var positions = order.Select(h => text.IndexOf(h, StringComparison.Ordinal)).ToList();
        Assert.DoesNotContain(-1, positions);
        Assert.Equal(positions.Order().ToList(), positions);
        Assert.Contains("Left knee: keep the box high", text);
        Assert.Contains("Knees out", text);
    }

    [Fact]
    public async Task The_header_shows_where_the_Exercise_sits_in_the_Plan_and_its_Best()
    {
        var cut = await RenderPageAsync(SquatGuide(), workouts: Past("w1", Today.AddDays(-7), 40, 8));

        Assert.Equal("Goblet squat", cut.Find("h1").TextContent);
        Assert.Equal("Lower · 3 × 8 · Best 40 kg × 8", cut.Find(".fa-guidepage__header p").TextContent);
        Assert.Contains("Lower: 3 × 8 · Chest up", cut.Find("section[aria-label='In your Plan']").TextContent);
    }

    [Fact]
    public async Task History_lists_the_last_three_results_and_marks_the_Personal_records()
    {
        var cut = await RenderPageAsync(
            SquatGuide(),
            workouts:
            [
                Past("w1", Today.AddDays(-28), 40, 8),
                Past("w2", Today.AddDays(-21), 45, 8),
                Past("w3", Today.AddDays(-14), 42, 8),
                Past("w4", Today.AddDays(-7), 50, 8),
            ]);

        var rows = cut.FindAll(".fa-guidepage__result");
        Assert.Equal(3, rows.Count);
        Assert.Contains("50 kg × 8", rows[0].TextContent);
        Assert.Contains("PR", rows[0].TextContent);
        Assert.Contains("42 kg × 8", rows[1].TextContent);
        Assert.DoesNotContain("PR", rows[1].TextContent);
        Assert.Contains("45 kg × 8", rows[2].TextContent);
        Assert.Contains("PR", rows[2].TextContent);
    }

    // ---- No Guide yet and editing

    [Fact]
    public async Task An_Exercise_with_no_Guide_says_so_and_offers_Write_a_Guide()
    {
        var cut = await RenderPageAsync();

        Assert.Contains("No Guide yet", cut.Find(".empty").TextContent);
        Assert.False(ButtonNamed(cut, "Write a Guide").HasAttribute("disabled"));
        Assert.Empty(cut.FindAll("textarea"));
    }

    [Fact]
    public async Task Writing_a_Guide_and_saving_creates_it_and_returns_to_the_read_view()
    {
        var data = await OpenSeededAsync(plan: Plan());
        var cut = await RenderPageAsync(data: data);

        ButtonNamed(cut, "Write a Guide").Click();
        Area(cut, "guide-works").Input("Quads");
        Area(cut, "guide-cues").Input("Knees out\n\nBrace first\n");
        cut.Find("#guide-video").Input("https://example.com/v");
        ButtonNamed(cut, "Save").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll("textarea"));
            Assert.Contains("Quads", cut.Markup);
        });
        var guide = Assert.Single(data.Snapshot.Guides);
        Assert.Equal(Squat, guide.ExerciseId);
        Assert.Equal("Quads", guide.Works);
        Assert.Equal(["Knees out", "Brace first"], guide.Cues);
        Assert.Equal("https://example.com/v", guide.VideoUrl);
        Assert.Null(guide.Setup);
    }

    [Fact]
    public async Task The_editor_starts_from_the_stored_Guide_with_a_labelled_box_per_section()
    {
        var cut = await RenderPageAsync(SquatGuide());

        ButtonNamed(cut, "Edit").Click();

        foreach (var label in new[] { "What it works", "Why it's in your plan", "Setup", "How to do it", "Cues", "Common mistakes", "Easier", "Harder", "Watch-outs", "Video link" })
        {
            Assert.Contains(cut.FindAll("label"), l => l.QuerySelector(".fa-label")?.TextContent == label);
        }

        Assert.Equal("Knees out\nBrace first", Area(cut, "guide-cues").GetAttribute("value")!.ReplaceLineEndings("\n"));
    }

    [Fact]
    public async Task An_invalid_video_link_is_refused_with_a_plain_message_and_nothing_is_saved()
    {
        var data = await OpenSeededAsync(plan: Plan());
        var cut = await RenderPageAsync(SquatGuide(), data: data);
        var before = data.Snapshot.Guides.Single();

        ButtonNamed(cut, "Edit").Click();
        cut.Find("#guide-video").Input("javascript:alert(1)");
        ButtonNamed(cut, "Save").Click();

        cut.WaitForAssertion(() => Assert.Contains("not a web address", cut.Find("[role='alert']").TextContent));
        Assert.Equal(before, data.Snapshot.Guides.Single());
        Assert.NotEmpty(cut.FindAll("textarea"));
    }

    [Fact]
    public async Task A_failed_save_keeps_the_edits_on_screen_and_says_why()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        var data = await OpenSeededAsync(store, Plan());
        var cut = await RenderPageAsync(SquatGuide(), data: data);
        ButtonNamed(cut, "Edit").Click();
        Area(cut, "guide-works").Input("A rewrite I do not want to lose");
        store.FailAllWrites(StoreFailureKind.StorageFull);

        ButtonNamed(cut, "Save").Click();

        cut.WaitForAssertion(() => Assert.NotNull(_notice.Message));
        Assert.Equal("A rewrite I do not want to lose", Area(cut, "guide-works").GetAttribute("value"));
        Assert.Equal("Quads and glutes", data.Snapshot.Guides.Single().Works);
        Assert.False(ButtonNamed(cut, "Save").HasAttribute("disabled"));
    }

    [Fact]
    public async Task Tapping_Save_twice_while_it_saves_saves_once()
    {
        var gate = new GateStore(new InMemoryDataStore());
        var data = await OpenSeededAsync(gate, Plan());
        var cut = await RenderPageAsync(SquatGuide(), data: data);
        ButtonNamed(cut, "Edit").Click();
        Area(cut, "guide-works").Input("New text");
        gate.Hold();

        ButtonNamed(cut, "Save").Click();
        cut.WaitForAssertion(() => Assert.True(ButtonNamed(cut, "Save").HasAttribute("disabled")));
        ButtonNamed(cut, "Save").Click();
        gate.Release();

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("textarea")));
        Assert.Equal(1, gate.Writes);
        Assert.Equal("New text", data.Snapshot.Guides.Single().Works);
    }

    // ---- Discard prompt

    [Fact]
    public async Task Cancel_with_no_changes_leaves_the_editor_without_asking()
    {
        var cut = await RenderPageAsync(SquatGuide());
        ButtonNamed(cut, "Edit").Click();

        ButtonNamed(cut, "Cancel").Click();

        Assert.Empty(cut.FindAll("[role='dialog']"));
        Assert.Empty(cut.FindAll("textarea"));
    }

    [Fact]
    public async Task Cancel_with_unsaved_changes_asks_Discard_changes_and_Keep_editing_keeps_the_text()
    {
        var cut = await RenderPageAsync(SquatGuide());
        ButtonNamed(cut, "Edit").Click();
        Area(cut, "guide-works").Input("Half typed");

        ButtonNamed(cut, "Cancel").Click();

        var dialog = cut.Find("[role='dialog']");
        Assert.Equal("Discard changes?", dialog.QuerySelector(".fa-sheet__title")!.TextContent);
        cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == "Keep editing").Click();
        Assert.Empty(cut.FindAll("[role='dialog']"));
        Assert.Equal("Half typed", Area(cut, "guide-works").GetAttribute("value"));
    }

    [Fact]
    public async Task Discard_returns_to_the_read_view_and_saves_nothing()
    {
        var data = await OpenSeededAsync(plan: Plan());
        var cut = await RenderPageAsync(SquatGuide(), data: data);
        ButtonNamed(cut, "Edit").Click();
        Area(cut, "guide-works").Input("Half typed");
        ButtonNamed(cut, "Cancel").Click();

        cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == "Discard").Click();

        Assert.Empty(cut.FindAll("[role='dialog']"));
        Assert.Empty(cut.FindAll("textarea"));
        Assert.Equal("Quads and glutes", data.Snapshot.Guides.Single().Works);
        Assert.DoesNotContain("Half typed", cut.Markup);
    }

    [Fact]
    public async Task Leaving_the_page_with_unsaved_changes_asks_first_and_Discard_then_goes()
    {
        var cut = await RenderPageAsync(SquatGuide());
        var navigation = Services.GetRequiredService<NavigationManager>();
        var start = navigation.Uri;
        ButtonNamed(cut, "Edit").Click();
        Area(cut, "guide-works").Input("Half typed");

        navigation.NavigateTo("exercises");

        cut.WaitForAssertion(() => Assert.Equal("Discard changes?", cut.Find("[role='dialog'] .fa-sheet__title").TextContent));
        Assert.Equal(start, navigation.Uri);
        cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == "Discard").Click();
        cut.WaitForAssertion(() => Assert.EndsWith("/exercises", navigation.Uri));
    }

    [Fact]
    public async Task Leaving_the_page_with_no_edits_just_goes()
    {
        var cut = await RenderPageAsync(SquatGuide());
        var navigation = Services.GetRequiredService<NavigationManager>();

        navigation.NavigateTo("exercises");

        Assert.EndsWith("/exercises", navigation.Uri);
        Assert.Empty(cut.FindAll("[role='dialog']"));
    }

    // ---- Notes

    [Fact]
    public async Task Notes_save_on_their_own_and_leave_the_sections_alone()
    {
        var data = await OpenSeededAsync(plan: Plan());
        var cut = await RenderPageAsync(SquatGuide(), data: data);

        ButtonNamed(cut, "Edit notes").Click();
        cut.Find("#guide-notes").Input("Left knee: keep the box high");
        ButtonNamed(cut, "Save notes").Click();

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[role='dialog']")));
        var guide = data.Snapshot.Guides.Single();
        Assert.Equal("Left knee: keep the box high", guide.PersonalNotes);
        Assert.Equal("Quads and glutes", guide.Works);
        Assert.Equal(["Knees out", "Brace first"], guide.Cues);
        Assert.Equal(Video, guide.VideoUrl);
        Assert.Contains("Left knee: keep the box high", cut.Find("section[aria-label='My notes']").TextContent);
    }

    // ---- Read-only

    [Fact]
    public async Task A_read_only_window_disables_Edit_and_Edit_notes_and_says_why()
    {
        var store = new InMemoryDataStore();
        await OpenSeededAsync(store, Plan());
        var locked = await AppData.OpenAsync(store, new InMemoryStorageEnvironment { WriterLock = WriterLockResult.HeldElsewhere });
        var cut = await RenderPageAsync(data: locked);

        Assert.True(ButtonNamed(cut, "Write a Guide").HasAttribute("disabled"));
        Assert.Contains("read-only", cut.Find(".fa-well[role='status']").TextContent);
    }

    [Fact]
    public async Task A_read_only_window_disables_Edit_and_Edit_notes_on_an_existing_Guide()
    {
        var store = new InMemoryDataStore();
        var writable = await OpenSeededAsync(store, Plan());
        Assert.IsType<CommitResult.Committed>(await writable.CommitAsync([Change.Put(SquatGuide())]));
        var locked = await AppData.OpenAsync(store, new InMemoryStorageEnvironment { WriterLock = WriterLockResult.HeldElsewhere });
        var cut = await RenderPageAsync(data: locked);

        Assert.True(ButtonNamed(cut, "Edit").HasAttribute("disabled"));
        Assert.True(ButtonNamed(cut, "Edit notes").HasAttribute("disabled"));
        Assert.Contains("read-only", cut.Find(".fa-well[role='status']").TextContent);
    }

    // ---- Video

    [Fact]
    public async Task Watch_a_demo_opens_the_link()
    {
        var module = JSInterop.SetupModule(VideoModule);
        module.Setup<bool>("openVideo", Video).SetResult(true);
        var cut = await RenderPageAsync(SquatGuide());

        ButtonNamed(cut, "Watch a demo").Click();

        cut.WaitForAssertion(() => Assert.Single(module.Invocations["openVideo"]));
        Assert.Empty(cut.FindAll(".empty"));
    }

    [Fact]
    public async Task When_the_video_cannot_open_the_offline_state_says_the_rest_of_the_app_still_works()
    {
        var module = JSInterop.SetupModule(VideoModule);
        module.Setup<bool>("openVideo", Video).SetResult(false);
        var cut = await RenderPageAsync(SquatGuide());

        ButtonNamed(cut, "Watch a demo").Click();

        cut.WaitForAssertion(() => Assert.Contains("rest of the app still works", cut.Find(".empty").TextContent));
        Assert.NotEmpty(cut.FindAll("h1"));
    }

    // ---- Unknown Exercise

    [Fact]
    public async Task An_Exercise_that_is_not_in_the_library_says_so()
    {
        var cut = await RenderPageAsync(exerciseId: "nope");

        Assert.Contains("isn't in your Exercise library", cut.Markup.Replace("&#39;", "'"));
    }

    // ---- From the log

    [Fact]
    public async Task The_log_Guide_sheet_offers_Open_full_Guide_to_the_page()
    {
        var data = await OpenSeededAsync(plan: Plan(), workouts: InProgress());
        Assert.IsType<CommitResult.Committed>(await data.CommitAsync([Change.Put(SquatGuide())]));
        Wire(this, data);
        JSInterop.SetupModule(SheetModule);
        var log = Render<WorkoutLog>(p => p.Add(l => l.WorkoutId, "w-now"));

        log.Find("button[aria-label='Menu for Goblet squat']").Click();
        log.FindAll("[role='dialog'] .fa-menuitem").Single(i => i.TextContent.Contains("View Guide")).Click();

        var link = log.FindAll("[role='dialog'] a").Single(a => a.TextContent.Trim() == "Open full Guide");
        Assert.Equal($"exercises/{Squat}/guide", link.GetAttribute("href"));
    }

    [Fact]
    public async Task Opening_the_full_Guide_and_coming_back_leaves_the_Workout_and_the_Set_as_they_were()
    {
        var data = await OpenSeededAsync(plan: Plan(), workouts: InProgress());
        Assert.IsType<CommitResult.Committed>(await data.CommitAsync([Change.Put(SquatGuide())]));
        Wire(this, data, _notice);
        JSInterop.SetupModule(SheetModule);
        var log = Render<WorkoutLog>(p => p.Add(l => l.WorkoutId, "w-now"));
        log.FindAll(".pager__chip")[1].Click();
        log.Find("input[aria-label='Weight, Set 1']").Input("42");
        var workoutBefore = data.Snapshot.Workouts.Single();

        await DisposeComponentsAsync();
        var guide = Render<GuidePage>(p => p.Add(g => g.ExerciseId, Squat));
        Assert.Equal("Goblet squat", guide.Find("h1").TextContent);
        await DisposeComponentsAsync();
        var back = Render<WorkoutLog>(p => p.Add(l => l.WorkoutId, "w-now"));

        Assert.Equal("Walking lunge", back.Find(".fa-exercise__name").TextContent);
        Assert.Equal("42", back.Find("input[aria-label='Weight, Set 1']").GetAttribute("value"));
        Assert.Equal(workoutBefore, data.Snapshot.Workouts.Single());
    }

    /// <summary>A store that can hold its writes until released, and counts them, so a test can tap while a save is running.</summary>
    private sealed class GateStore(IDataStore inner) : IDataStore
    {
        private TaskCompletionSource? _gate;

        public int Writes { get; private set; }

        public void Hold() => _gate = new TaskCompletionSource();

        public void Release() => _gate?.SetResult();

        public Task<StoreResult<StoredData>> LoadAllAsync() => inner.LoadAllAsync();

        public async Task<StoreResult<Unit>> ApplyAsync(IReadOnlyList<StoreChange> changes, StoreMetadata? metadata = null)
        {
            if (_gate is { } gate)
            {
                Writes++;
                await gate.Task;
            }

            return await inner.ApplyAsync(changes, metadata);
        }

        public Task<StoreResult<Unit>> ReplaceAllAsync(IReadOnlyList<StoredRecord> records, StoreMetadata? metadata = null) =>
            inner.ReplaceAllAsync(records, metadata);

        public Task<StoreResult<Unit>> WriteMetadataAsync(StoreMetadata metadata) => inner.WriteMetadataAsync(metadata);
    }
}
