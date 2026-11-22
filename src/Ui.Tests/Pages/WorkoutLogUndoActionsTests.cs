using AngleSharp.Dom;
using Bunit;
using Core.Model;
using Core.Storage;
using Core.Tests.Storage;
using Ui.Pages;
using Ui.Saving;
using static Ui.Tests.Pages.PageFixture;

namespace Ui.Tests.Pages;

/// <summary>
/// The Exercise menu's undoing items (Un-skip, Undo swap, Remove Exercise) when the window cannot save and when a save fails. The doing
/// items have the same two tests next to their own feature; these three had none.
/// </summary>
public sealed class WorkoutLogUndoActionsTests : BunitContext
{
    private readonly SaveFailureNotice _notice = new();

    /// <summary>Goblet squat skipped, Walking lunge swapped for Plank, and a Pull-up added after them with no Sets.</summary>
    private static readonly WorkoutEntry[] Entries =
    [
        new(Squat, EntryStatus.Skipped, null, null, null, []),
        new("plank", EntryStatus.Swapped, null, Lunge, null, []),
        new("pullup", EntryStatus.Done, null, null, null, []),
    ];

    private async Task<IRenderedComponent<WorkoutLog>> OpenAsync(AppData data)
    {
        Wire(this, data, _notice);
        return Render<WorkoutLog>(p => p.Add(l => l.WorkoutId, "w-now"));
    }

    private static IElement Item(IRenderedComponent<WorkoutLog> cut, string exercise, string item)
    {
        cut.Find($"button[aria-label='Menu for {exercise}']").Click();
        return cut.FindAll("[role='dialog'] .fa-menuitem").Single(i => i.TextContent.Contains(item));
    }

    private static void Show(IRenderedComponent<WorkoutLog> cut, int chip) => cut.FindAll(".pager__chip")[chip].Click();

    private static IReadOnlyList<WorkoutEntry> Stored(AppData data) => data.Snapshot.Workouts.Single(w => w.Id == "w-now").Entries;

    [Fact]
    public async Task When_the_window_cannot_save_Un_skip_Undo_swap_and_Remove_are_disabled()
    {
        var store = new InMemoryDataStore();
        await OpenSeededAsync(store, Plan(), InProgress("w-now", Entries));
        var locked = await AppData.OpenAsync(store, new InMemoryStorageEnvironment { WriterLock = WriterLockResult.HeldElsewhere });
        var cut = await OpenAsync(locked);

        Show(cut, 0);
        Assert.True(Item(cut, "Goblet squat", "Un-skip").HasAttribute("disabled"));
        cut.Find("[role='dialog'] .fa-btn").Click();
        Show(cut, 1);
        Assert.True(Item(cut, "Plank", "Undo swap").HasAttribute("disabled"));
        cut.Find("[role='dialog'] .fa-btn").Click();
        Show(cut, 2);
        Assert.True(Item(cut, "Pull-up", "Remove Exercise").HasAttribute("disabled"));
    }

    [Fact]
    public async Task A_failed_Un_skip_Undo_swap_or_Remove_shows_the_banner_and_leaves_the_Workout_as_it_was()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        var data = await OpenSeededAsync(store, Plan(), InProgress("w-now", Entries));
        var cut = await OpenAsync(data);
        store.FailAllWrites(StoreFailureKind.StorageFull);

        foreach (var (chip, exercise, item) in new[] { (0, "Goblet squat", "Un-skip"), (1, "Plank", "Undo swap"), (2, "Pull-up", "Remove Exercise") })
        {
            Show(cut, chip);
            Item(cut, exercise, item).Click();

            Assert.Contains("out of storage", _notice.Message);
            Assert.Equal(Entries, Stored(data));
            Assert.Equal(3, cut.FindAll(".pager__chip").Count);
            Assert.Equal(exercise, cut.Find(".fa-exercise__name").TextContent);
        }
    }
}
