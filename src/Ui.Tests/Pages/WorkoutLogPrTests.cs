using AngleSharp.Dom;
using Bunit;
using Core.Model;
using Core.Storage;
using Ui.Pages;
using Ui.Saving;
using static Ui.Tests.Pages.LogFixture;

namespace Ui.Tests.Pages;

/// <summary>PR feedback on the log page (C-03, 8.3): marker, reps label, toast and the PR count, all derived from the open Workout.</summary>
public sealed class WorkoutLogPrTests : BunitContext
{
    private static Workout Previous(params WorkoutSet[] sets) => Finished("w-old", Today.AddDays(-7), sets);

    private async Task<(AppData Data, IRenderedComponent<WorkoutLog> Cut)> OpenAsync(Workout? previous, Workout? open = null)
    {
        Workout[] workouts = previous is null ? [open ?? InProgress()] : [previous, open ?? InProgress()];
        var data = await OpenSeededAsync(plan: Plan(), workouts: workouts);
        Wire(this, data, new SaveFailureNotice());
        return (data, Render<WorkoutLog>(p => p.Add(l => l.WorkoutId, "w-now")));
    }

    private static void Log(IRenderedComponent<WorkoutLog> cut, int set, string kg, string reps)
    {
        cut.Find($"input[aria-label^='Weight, set {set}']").Input(kg);
        cut.Find($"input[aria-label^='Reps, set {set}']").Input(reps);
        cut.Find(".fa-confirmbar .fa-btn--primary").Click();
    }

    private static string Progress(IRenderedComponent<WorkoutLog> cut) => cut.Find(".log__progress").TextContent;

    [Fact]
    public async Task The_first_ever_result_is_a_baseline_and_shows_no_PR()
    {
        var (_, cut) = await OpenAsync(null);

        Log(cut, 1, "22", "8");

        Assert.Empty(cut.FindAll(".fa-pr"));
        Assert.Empty(cut.FindAll(".fa-toast"));
        Assert.Equal("Reps, set 1", cut.Find("input[aria-label^='Reps, set 1']").GetAttribute("aria-label"));
        Assert.Contains("0 PR", Progress(cut));
    }

    [Fact]
    public async Task A_strictly_better_Set_shows_the_marker_the_label_the_toast_and_the_count()
    {
        var (_, cut) = await OpenAsync(Previous(Set("o1", 22, 8)));

        Log(cut, 1, "22", "9");

        var marker = cut.Find(".fa-setrow:nth-child(1) .fa-pr");
        Assert.Equal("PR", marker.GetAttribute("aria-label"));
        Assert.Contains("PR", marker.TextContent);
        Assert.Equal("Reps, set 1, PR", cut.Find("input[aria-label^='Reps, set 1']").GetAttribute("aria-label"));
        var toast = cut.Find(".fa-toast");
        Assert.Equal("PR! 22 kg × 9 beats 22 × 8", toast.TextContent.Trim());
        Assert.Equal("status", toast.GetAttribute("role"));
        Assert.Contains("1 PR", Progress(cut));
    }

    [Fact]
    public async Task An_equal_Set_is_not_a_PR()
    {
        var (_, cut) = await OpenAsync(Previous(Set("o1", 22, 8)));

        Log(cut, 1, "22", "8");

        Assert.Empty(cut.FindAll(".fa-pr"));
        Assert.Contains("0 PR", Progress(cut));
    }

    [Fact]
    public async Task The_marker_moves_to_a_later_better_Set()
    {
        var (_, cut) = await OpenAsync(Previous(Set("o1", 22, 8)));

        Log(cut, 1, "22", "9");
        Log(cut, 2, "22", "10");

        Assert.Single(cut.FindAll(".fa-pr"));
        Assert.Empty(cut.FindAll(".fa-setrow:nth-child(1) .fa-pr"));
        Assert.Single(cut.FindAll(".fa-setrow:nth-child(2) .fa-pr"));
        Assert.Contains("1 PR", Progress(cut));
        Assert.Contains("beats 22 × 8", cut.Find(".fa-toast").TextContent);
    }

    [Fact]
    public async Task Reopening_a_PR_Set_clears_the_marker_and_editing_it_down_keeps_it_cleared()
    {
        var (_, cut) = await OpenAsync(Previous(Set("o1", 22, 8)));
        Log(cut, 1, "22", "9");

        cut.Find("button[aria-label^='Edit set 1']").Click();
        Assert.Empty(cut.FindAll(".fa-pr"));
        Assert.Contains("0 PR", Progress(cut));

        Log(cut, 1, "22", "7");
        Assert.Empty(cut.FindAll(".fa-pr"));
        Assert.Empty(cut.FindAll(".fa-toast"));
        Assert.Contains("0 PR", Progress(cut));
    }

    [Fact]
    public async Task A_Max_set_in_history_outranks_a_heavier_ordinary_Set()
    {
        var max = Set("o1", 20, 5) with { IsMax = true };
        var (_, cut) = await OpenAsync(Previous(max));

        Log(cut, 1, "25", "5");

        Assert.Empty(cut.FindAll(".fa-pr"));
    }

    [Fact]
    public async Task A_Max_set_in_the_open_Workout_is_compared_with_the_Max_set_in_history()
    {
        var open = InProgress("w-now", new WorkoutEntry(Squat, EntryStatus.Done, null, null, null,
            [Set("c1", 22, 5) with { IsMax = true }]));
        var (_, cut) = await OpenAsync(Previous(Set("o1", 20, 5) with { IsMax = true }), open);

        Assert.Single(cut.FindAll(".fa-setrow:nth-child(1) .fa-pr"));
        Assert.Contains("1 PR", Progress(cut));
    }

    [Fact]
    public async Task The_current_Workout_is_not_part_of_the_history_it_is_compared_to()
    {
        var open = InProgress("w-now", new WorkoutEntry(Squat, EntryStatus.Done, null, null, null, [Set("c1", 22, 8)]));
        var (_, cut) = await OpenAsync(null, open);

        Assert.Empty(cut.FindAll(".fa-pr"));
    }

    [Fact]
    public async Task The_toast_is_cleared_when_moving_on()
    {
        var (_, cut) = await OpenAsync(Previous(Set("o1", 22, 8)));
        Log(cut, 1, "22", "9");

        cut.Find("nav.pager button:nth-child(2)").Click();

        Assert.Empty(cut.FindAll(".fa-toast"));
    }
}
