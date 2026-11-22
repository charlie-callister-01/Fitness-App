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

/// <summary>The Exercise library page (design S-04) and the New Exercise form it opens.</summary>
public sealed class ExerciseLibraryTests : BunitContext
{
    private readonly SaveFailureNotice _notice = new();

    private IRenderedComponent<Exercises> RenderLibrary(AppData data)
    {
        Wire(this, data, _notice);
        return Render<Exercises>();
    }

    private string CurrentPath => Services.GetRequiredService<NavigationManager>().Uri[Services.GetRequiredService<NavigationManager>().BaseUri.Length..];

    private static async Task<AppData> SeedAsync(int extraLibrary = 0, params Guide[] guides)
    {
        var data = await OpenSeededAsync(plan: Plan());
        var changes = new List<Change>
        {
            Change.Put(new Exercise(Squat, "Goblet squat", LogKind.Load, new ExerciseTags(["legs"], ["kettlebell"], []), false)),
        };
        changes.AddRange(guides.Select(Change.Put));
        changes.AddRange(Enumerable.Range(0, extraLibrary).Select(i => Change.Put(new Exercise($"x{i:00}", $"Filler {i:00}", LogKind.Reps, null, false))));
        Assert.IsType<CommitResult.Committed>(await data.CommitAsync(changes));
        return data;
    }

    private static string[] Headings(IRenderedComponent<Exercises> cut) =>
        [.. cut.FindAll(".fa-lib__heading").Select(h => h.TextContent.Trim())];

    private static string[] Names(IRenderedComponent<Exercises> cut) =>
        [.. cut.FindAll(".fa-lib__name").Select(n => n.TextContent.Trim())];

    private static IElement Field(IRenderedComponent<Exercises> cut, string label) => cut.Find($"[role='dialog'] [aria-label='{label}']");

    private static IElement DialogButton(IRenderedComponent<Exercises> cut, string text) =>
        cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == text);

    private static void OpenForm(IRenderedComponent<Exercises> cut) => cut.Find("button[aria-label='New Exercise']").Click();

    private static void Fill(IRenderedComponent<Exercises> cut, string name, string? kind = null)
    {
        Field(cut, "Name").Input(name);
        if (kind is not null)
        {
            DialogButton(cut, kind).Click();
        }
    }

    // ---- Browse

    [Fact]
    public async Task Exercises_are_grouped_by_Session_then_Not_in_a_Session_with_kind_tags_and_Guide_status()
    {
        var guide = new Guide(Squat, "Legs", null, null, null, ["Chest up"], [], null, null, [], null, null);
        var cut = RenderLibrary(await SeedAsync(guides: guide));

        Assert.Equal(["Lower", "Not in a Session"], Headings(cut));
        Assert.Equal(["Goblet squat", "Walking lunge", "Journal", "Plank", "Pull-up"], Names(cut));
        var squat = cut.FindAll(".fa-lib__row")[0];
        Assert.Contains("Weight and reps", squat.TextContent);
        Assert.Contains("legs", squat.TextContent);
        Assert.Contains("kettlebell", squat.TextContent);
        Assert.Contains("Guide", cut.FindAll(".fa-lib__guide")[0].TextContent);
        Assert.DoesNotContain("No Guide yet", cut.FindAll(".fa-lib__guide")[0].TextContent);
        Assert.Contains("No Guide yet", cut.FindAll(".fa-lib__guide")[1].TextContent);
    }

    [Fact]
    public async Task Session_group_headings_name_the_Track_when_the_Plan_has_more_than_one_Track()
    {
        var plan = new Plan(
            "plan",
            "Plan",
            true,
            Today,
            null,
            null,
            2,
            [
                new Track(MainTrack, "Main", new TrackRhythm.Weekly(3, 2, false), []),
                new Track("mob", "Morning mobility", new TrackRhythm.Daily(75), []),
            ],
            [
                new Session("lower", "Lower", MainTrack, DefaultDay.Monday, SessionType.Strength, null,
                    [new SessionExercise(Squat, 3, 8, null, null)]),
                new Session("mob-s", "Morning routine", "mob", DefaultDay.Daily, SessionType.Mobility, null,
                    [new SessionExercise(Lunge, 1, 10, null, null)]),
            ]);
        var data = await OpenSeededAsync(plan: plan);
        var cut = RenderLibrary(data);

        Assert.Equal(["Main · Lower", "Morning mobility · Morning routine", "Not in a Session"], Headings(cut));
    }

    [Fact]
    public async Task A_row_links_to_the_Guide_page_of_its_Exercise()
    {
        var cut = RenderLibrary(await SeedAsync());

        var href = cut.FindAll(".fa-lib__row")[0].GetAttribute("href");

        Assert.Equal("exercises/squat/guide", href);
    }

    [Fact]
    public async Task A_small_library_has_no_search_box()
    {
        var cut = RenderLibrary(await SeedAsync());

        Assert.Empty(cut.FindAll("input[type='search']"));
    }

    [Fact]
    public async Task A_big_library_can_be_searched_by_name()
    {
        var cut = RenderLibrary(await SeedAsync(extraLibrary: 20));

        cut.Find("input[type='search']").Input("lunge");

        Assert.Equal(["Walking lunge"], Names(cut));
    }

    [Fact]
    public async Task A_tag_chip_filters_and_a_second_tap_clears_it()
    {
        var cut = RenderLibrary(await SeedAsync());

        cut.FindAll(".fa-lib__tags button").Single(b => b.TextContent.Trim() == "legs").Click();
        Assert.Equal(["Goblet squat"], Names(cut));
        Assert.Equal("true", cut.FindAll(".fa-lib__tags button").Single(b => b.TextContent.Trim() == "legs").GetAttribute("aria-pressed"));

        cut.FindAll(".fa-lib__tags button").Single(b => b.TextContent.Trim() == "legs").Click();
        Assert.Equal(5, Names(cut).Length);
        Assert.Equal("true", cut.FindAll(".fa-lib__tags button").Single(b => b.TextContent.Trim() == "All").GetAttribute("aria-pressed"));
    }

    [Fact]
    public async Task A_search_with_no_match_offers_to_add_it_and_opens_the_form_with_the_text()
    {
        var cut = RenderLibrary(await SeedAsync(extraLibrary: 20));
        cut.Find("input[type='search']").Input("Kettlebell swing");

        cut.FindAll("button").Single(b => b.TextContent.Contains("Add 'Kettlebell swing' as a new Exercise")).Click();

        Assert.Equal("Kettlebell swing", Field(cut, "Name").GetAttribute("value"));
    }

    [Fact]
    public async Task An_empty_library_shows_an_empty_state_that_opens_the_form()
    {
        var cut = RenderLibrary(await AppData.OpenAsync(new InMemoryDataStore()));

        Assert.Contains("No Exercises yet", cut.Markup);
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Add an Exercise").Click();

        Assert.NotEmpty(cut.FindAll("[role='dialog']"));
    }

    // ---- Add

    [Fact]
    public async Task Adding_an_Exercise_stores_it_with_a_permanent_id_its_kind_and_tags_and_closes_the_form()
    {
        var data = await SeedAsync();
        var cut = RenderLibrary(data);
        OpenForm(cut);

        Fill(cut, "  Kettlebell swing ", "Weight and reps");
        Field(cut, "Muscle groups").Input("glutes, hamstrings");
        Field(cut, "Equipment").Input("kettlebell");
        Field(cut, "Movement pattern").Input("hinge");
        DialogButton(cut, "Add Exercise").Click();

        cut.WaitForAssertion(() => Assert.Contains(data.Snapshot.Exercises, e => e.Name == "Kettlebell swing"));
        var added = data.Snapshot.Exercises.Single(e => e.Name == "Kettlebell swing");
        Assert.Equal("id-1", added.Id);
        Assert.Equal(LogKind.Load, added.LogKind);
        Assert.Equal(["glutes", "hamstrings"], added.Tags!.MuscleGroups);
        Assert.Equal(["kettlebell"], added.Tags.Equipment);
        Assert.Equal(["hinge"], added.Tags.MovementPatterns);
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[role='dialog']")));
        Assert.Contains("Kettlebell swing", Names(cut));
        Assert.Empty(data.Snapshot.Guides);
    }

    [Fact]
    public async Task The_chosen_log_kind_is_the_one_stored()
    {
        var data = await SeedAsync();
        var cut = RenderLibrary(data);
        OpenForm(cut);

        Fill(cut, "Farmer walk", "Distance in metres");
        Assert.Equal("true", DialogButton(cut, "Distance in metres").GetAttribute("aria-pressed"));
        DialogButton(cut, "Add Exercise").Click();

        cut.WaitForAssertion(() => Assert.Equal(LogKind.DistanceM, data.Snapshot.Exercises.Single(e => e.Name == "Farmer walk").LogKind));
    }

    [Fact]
    public async Task The_two_body_measurements_are_not_offered()
    {
        var cut = RenderLibrary(await SeedAsync());
        OpenForm(cut);

        Assert.DoesNotContain(cut.FindAll("[role='dialog'] button"), b => b.TextContent.Contains("Body measurement"));
    }

    [Fact]
    public async Task A_cue_is_saved_with_the_Exercise_as_its_Guide_in_one_commit()
    {
        var data = await SeedAsync();
        var cut = RenderLibrary(data);
        OpenForm(cut);

        Fill(cut, "Kettlebell swing");
        Field(cut, "Cue").Input("Snap the hips");
        DialogButton(cut, "Add Exercise").Click();

        cut.WaitForAssertion(() => Assert.Single(data.Snapshot.Guides, g => g.ExerciseId == "id-1"));
        Assert.Equal(["Snap the hips"], data.Snapshot.Guides.Single(g => g.ExerciseId == "id-1").Cues);
    }

    [Fact]
    public async Task Write_a_Guide_now_goes_to_the_Guide_page_of_the_new_Exercise()
    {
        var data = await SeedAsync();
        var cut = RenderLibrary(data);
        OpenForm(cut);

        Fill(cut, "Kettlebell swing");
        cut.Find("[role='dialog'] input[type='checkbox']").Change(true);
        DialogButton(cut, "Add Exercise").Click();

        cut.WaitForAssertion(() => Assert.Equal("exercises/id-1/guide", CurrentPath));
        Assert.Contains(data.Snapshot.Exercises, e => e.Id == "id-1");
    }

    [Fact]
    public async Task Without_the_Guide_box_ticked_the_page_stays_on_the_library()
    {
        var cut = RenderLibrary(await SeedAsync());
        OpenForm(cut);

        Fill(cut, "Kettlebell swing");
        DialogButton(cut, "Add Exercise").Click();

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[role='dialog']")));
        Assert.Equal("", CurrentPath);
    }

    [Fact]
    public async Task A_duplicate_name_is_refused_naming_the_existing_Exercise_and_the_form_stays()
    {
        var data = await SeedAsync();
        var cut = RenderLibrary(data);
        OpenForm(cut);

        Fill(cut, "  goblet SQUAT ");
        DialogButton(cut, "Add Exercise").Click();

        cut.WaitForAssertion(() => Assert.Contains("You already have an Exercise called \"Goblet squat\"", cut.Find("[role='dialog'] [role='alert']").TextContent));
        Assert.Equal(5, data.Snapshot.Exercises.Count);
        Assert.Equal("  goblet SQUAT ", Field(cut, "Name").GetAttribute("value"));
    }

    [Fact]
    public async Task An_empty_name_is_refused_with_a_message()
    {
        var data = await SeedAsync();
        var cut = RenderLibrary(data);
        OpenForm(cut);

        DialogButton(cut, "Add Exercise").Click();

        cut.WaitForAssertion(() => Assert.Contains("Give the Exercise a name.", cut.Find("[role='dialog'] [role='alert']").TextContent));
        Assert.Equal(5, data.Snapshot.Exercises.Count);
    }

    [Fact]
    public async Task Cancel_closes_the_form_and_stores_nothing()
    {
        var data = await SeedAsync();
        var cut = RenderLibrary(data);
        OpenForm(cut);
        Fill(cut, "Kettlebell swing");

        DialogButton(cut, "Cancel").Click();

        Assert.Empty(cut.FindAll("[role='dialog']"));
        Assert.Equal(5, data.Snapshot.Exercises.Count);
    }

    [Fact]
    public async Task Reopening_the_form_starts_blank()
    {
        var cut = RenderLibrary(await SeedAsync());
        OpenForm(cut);
        Fill(cut, "Kettlebell swing");
        DialogButton(cut, "Cancel").Click();

        OpenForm(cut);

        Assert.Equal("", Field(cut, "Name").GetAttribute("value") ?? "");
    }

    [Fact]
    public async Task A_read_only_window_disables_Add_Exercise_and_says_why()
    {
        var store = new InMemoryDataStore();
        await OpenSeededAsync(store, Plan());
        var locked = await AppData.OpenAsync(store, new InMemoryStorageEnvironment { WriterLock = WriterLockResult.HeldElsewhere });
        var cut = RenderLibrary(locked);
        OpenForm(cut);

        Fill(cut, "Kettlebell swing");

        Assert.True(DialogButton(cut, "Add Exercise").HasAttribute("disabled"));
        Assert.Contains("read-only", cut.Find("[role='dialog']").TextContent);
        Assert.DoesNotContain(locked.Snapshot.Exercises, e => e.Name == "Kettlebell swing");
    }

    [Fact]
    public async Task A_failed_save_shows_the_notice_and_keeps_the_form_and_what_was_typed()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        var data = await OpenSeededAsync(store, Plan());
        var cut = RenderLibrary(data);
        OpenForm(cut);
        Fill(cut, "Kettlebell swing");
        store.FailAllWrites(StoreFailureKind.StorageFull);

        DialogButton(cut, "Add Exercise").Click();

        cut.WaitForAssertion(() => Assert.Contains("out of storage", _notice.Message));
        Assert.NotEmpty(cut.FindAll("[role='dialog']"));
        Assert.Equal("Kettlebell swing", Field(cut, "Name").GetAttribute("value"));
        Assert.DoesNotContain(data.Snapshot.Exercises, e => e.Name == "Kettlebell swing");
    }

    [Fact]
    public async Task A_double_tap_saves_once()
    {
        var store = new GatedApplyStore(new InMemoryDataStore());
        var data = await OpenSeededAsync(store, Plan());
        var cut = RenderLibrary(data);
        OpenForm(cut);
        Fill(cut, "Kettlebell swing");
        var add = DialogButton(cut, "Add Exercise");
        store.Hold();

        add.Click();
        cut.WaitForAssertion(() => Assert.Equal(1, store.AppliesReached));
        add.Click();
        store.Release();

        cut.WaitForAssertion(() => Assert.Contains(data.Snapshot.Exercises, e => e.Name == "Kettlebell swing"));
        Assert.Single(data.Snapshot.Exercises, e => e.Name == "Kettlebell swing");
        Assert.Equal(1, store.AppliesReached);
    }

    /// <summary>A store that can hold the next write open until released, counting the writes that reached it after <see cref="Hold"/>.</summary>
    internal sealed class GatedApplyStore(IDataStore inner) : IDataStore
    {
        private TaskCompletionSource? _gate;
        private int _reached;

        public int AppliesReached => Volatile.Read(ref _reached);

        public void Hold() => _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Release() => _gate?.TrySetResult();

        public Task<StoreResult<StoredData>> LoadAllAsync() => inner.LoadAllAsync();

        public async Task<StoreResult<Unit>> ApplyAsync(IReadOnlyList<StoreChange> changes, StoreMetadata? metadata = null)
        {
            if (_gate is { } gate)
            {
                Interlocked.Increment(ref _reached);
                await gate.Task;
            }

            return await inner.ApplyAsync(changes, metadata);
        }

        public Task<StoreResult<Unit>> ReplaceAllAsync(IReadOnlyList<StoredRecord> records, StoreMetadata? metadata = null) =>
            inner.ReplaceAllAsync(records, metadata);

        public Task<StoreResult<Unit>> WriteMetadataAsync(StoreMetadata metadata) => inner.WriteMetadataAsync(metadata);
    }
}
