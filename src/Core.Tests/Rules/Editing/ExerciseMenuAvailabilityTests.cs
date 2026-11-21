using Core.Rules.Editing;

namespace Core.Tests.Rules.Editing;

public class ExerciseMenuAvailabilityTests
{
    private static ExerciseMenuFacts Plain() => new(
        IsSkipped: false, IsSwapped: false, IsAdded: false, HasSets: false, IsBenchmark: false, CanWrite: true, HasChoices: true);

    [Fact]
    public void A_plain_Exercise_offers_Swap_Skip_Add_and_Benchmark_and_hides_the_rest()
    {
        var menu = ExerciseMenuAvailability.Of(Plain());

        Assert.Equal(MenuItemAvailability.Enabled, menu.Swap);
        Assert.Equal(MenuItemAvailability.Enabled, menu.Skip);
        Assert.Equal(MenuItemAvailability.Enabled, menu.Add);
        Assert.Equal(MenuItemAvailability.Enabled, menu.Benchmark);
        Assert.Equal(MenuItemAvailability.Hidden, menu.UndoSwap);
        Assert.Equal(MenuItemAvailability.Hidden, menu.Unskip);
        Assert.Equal(MenuItemAvailability.Hidden, menu.Remove);
    }

    [Fact]
    public void A_skipped_Exercise_offers_Un_skip_in_place_of_Skip()
    {
        var menu = ExerciseMenuAvailability.Of(Plain() with { IsSkipped = true });

        Assert.Equal(MenuItemAvailability.Enabled, menu.Unskip);
        Assert.Equal(MenuItemAvailability.Hidden, menu.Skip);
    }

    [Fact]
    public void An_added_Exercise_hides_Skip_disables_Swap_and_offers_Remove()
    {
        var menu = ExerciseMenuAvailability.Of(Plain() with { IsAdded = true });

        Assert.Equal(MenuItemAvailability.Hidden, menu.Skip);
        Assert.Equal(MenuItemAvailability.Disabled, menu.Swap);
        Assert.Equal(MenuItemAvailability.Enabled, menu.Remove);
    }

    [Fact]
    public void A_swapped_Exercise_offers_Undo_swap()
    {
        var menu = ExerciseMenuAvailability.Of(Plain() with { IsSwapped = true });

        Assert.Equal(MenuItemAvailability.Enabled, menu.UndoSwap);
    }

    [Fact]
    public void Sets_disable_Undo_swap_and_Remove_but_not_Skip_or_Swap()
    {
        var swapped = ExerciseMenuAvailability.Of(Plain() with { IsSwapped = true, HasSets = true });
        var added = ExerciseMenuAvailability.Of(Plain() with { IsAdded = true, HasSets = true });

        Assert.Equal(MenuItemAvailability.Disabled, swapped.UndoSwap);
        Assert.Equal(MenuItemAvailability.Enabled, swapped.Swap);
        Assert.Equal(MenuItemAvailability.Enabled, swapped.Skip);
        Assert.Equal(MenuItemAvailability.Disabled, added.Remove);
    }

    [Fact]
    public void Without_choices_Swap_and_Add_are_disabled()
    {
        var menu = ExerciseMenuAvailability.Of(Plain() with { HasChoices = false });

        Assert.Equal(MenuItemAvailability.Disabled, menu.Swap);
        Assert.Equal(MenuItemAvailability.Disabled, menu.Add);
        Assert.Equal(MenuItemAvailability.Enabled, menu.Skip);
    }

    [Fact]
    public void A_read_only_window_disables_every_item_that_is_shown_and_keeps_the_same_ones_shown()
    {
        var all = Plain() with { IsSwapped = true, IsSkipped = true, IsAdded = true };
        var writable = ExerciseMenuAvailability.Of(all);
        var readOnly = ExerciseMenuAvailability.Of(all with { CanWrite = false });

        foreach (var (open, shut) in Items(writable).Zip(Items(readOnly)))
        {
            Assert.Equal(open == MenuItemAvailability.Hidden ? MenuItemAvailability.Hidden : MenuItemAvailability.Disabled, shut);
        }
    }

    private static MenuItemAvailability[] Items(ExerciseMenuAvailability m) =>
        [m.Swap, m.UndoSwap, m.Skip, m.Unskip, m.Add, m.Remove, m.Benchmark];
}
