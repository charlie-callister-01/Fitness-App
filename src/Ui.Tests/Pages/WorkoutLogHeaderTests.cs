using Bunit;
using Core.Model;
using Core.Storage;
using Ui.Pages;
using Ui.Saving;
using static Ui.Tests.Pages.PageFixture;

namespace Ui.Tests.Pages;

/// <summary>The Exercise header (Last, Best, Suggested target) and the RPE selector on the log page.</summary>
public sealed class WorkoutLogHeaderTests : BunitContext
{
    private async Task<(AppData Data, IRenderedComponent<WorkoutLog> Cut)> OpenAsync(
        Func<AppData, Task>? tweak = null, params Workout[] history)
    {
        var data = await OpenSeededAsync(plan: Plan(), workouts: [.. history, InProgress()]);
        if (tweak is not null)
        {
            await tweak(data);
        }

        Wire(this, data, new SaveFailureNotice());
        return (data, Render<WorkoutLog>(p => p.Add(l => l.WorkoutId, "w-now")));
    }

    private static Workout History() =>
        Finished("w-old", Today.AddDays(-7), Set("o1", 20, 8), Set("o2", 22.5, 6));

    private static Workout Stored(AppData data) => data.Snapshot.Workouts.Single(w => w.Id == "w-now");

    private static void Confirm(IRenderedComponent<WorkoutLog> cut) => cut.Find(".fa-confirmbar .fa-btn--primary").Click();

    // ---- Last, Best and the Suggested target

    [Fact]
    public async Task An_exercise_with_history_shows_last_best_and_the_target_with_its_reason()
    {
        var (_, cut) = await OpenAsync(null, History());

        Assert.Equal("Last 22.5 kg × 6 · Best 22.5 kg × 6", cut.Find(".fa-exercise__lastbest").TextContent);
        var target = cut.Find(".fa-well.fa-target");
        Assert.Contains("kg ×", target.QuerySelector("b")!.TextContent);
        Assert.Contains("—", target.TextContent);
        Assert.Contains("Hit", target.TextContent);
    }

    [Fact]
    public async Task A_new_exercise_shows_no_last_best_target_or_empty_labels()
    {
        var (_, cut) = await OpenAsync();

        Assert.Empty(cut.FindAll(".fa-exercise__lastbest"));
        Assert.Empty(cut.FindAll(".fa-target"));
        Assert.DoesNotContain("Last", cut.Find(".fa-exercise").TextContent);
        Assert.DoesNotContain("Best", cut.Find(".fa-exercise").TextContent);
    }

    [Fact]
    public async Task The_header_reads_in_pounds_when_the_owner_uses_lb()
    {
        var (_, cut) = await OpenAsync(
            async d => await d.CommitAsync(Change.Put(d.Snapshot.Settings with { DisplayWeightUnit = WeightUnit.Lb })),
            Finished("w-old", Today.AddDays(-7), Set("o1", 20, 8)));

        Assert.Equal("Last 44 lb × 8 · Best 44 lb × 8", cut.Find(".fa-exercise__lastbest").TextContent);
        Assert.Contains("lb ×", cut.Find(".fa-target b").TextContent);
    }

    [Fact]
    public async Task The_suggested_target_reason_reads_in_pounds_for_an_lb_owner()
    {
        var (_, cut) = await OpenAsync(
            async d => await d.CommitAsync(Change.Put(d.Snapshot.Settings with { DisplayWeightUnit = WeightUnit.Lb })),
            Finished("w-old", Today.AddDays(-7), Set("o1", 20, 8), Set("o2", 20, 8), Set("o3", 20, 8)));

        var target = cut.Find(".fa-well.fa-target span").TextContent;
        Assert.Contains("lb", target);
        Assert.DoesNotContain("kg", target);
    }

    [Fact]
    public async Task The_suggested_target_reason_reads_in_kilograms_for_a_kg_owner()
    {
        var (_, cut) = await OpenAsync(null, Finished("w-old", Today.AddDays(-7), Set("o1", 20, 8), Set("o2", 20, 8), Set("o3", 20, 8)));

        var target = cut.Find(".fa-well.fa-target span").TextContent;
        Assert.Contains("kg", target);
        Assert.DoesNotContain("lb", target);
    }

    [Fact]
    public async Task The_header_ignores_the_workout_in_progress()
    {
        var (_, cut) = await OpenAsync();

        cut.Find("input[aria-label='Weight, Set 1']").Input("30");
        cut.Find("input[aria-label='Reps, Set 1']").Input("8");
        Confirm(cut);

        Assert.Empty(cut.FindAll(".fa-exercise__lastbest"));
    }

    // ---- RPE

    private static AngleSharp.Dom.IElement Rpe(IRenderedComponent<WorkoutLog> cut, int value) =>
        cut.Find($".fa-rpe button[aria-label='RPE {value}']");

    [Fact]
    public async Task Rpe_offers_six_to_ten_and_none_is_pressed_at_first()
    {
        var (_, cut) = await OpenAsync();

        var buttons = cut.FindAll(".fa-rpe button");
        Assert.Equal(["6", "7", "8", "9", "10"], buttons.Select(b => b.TextContent));
        Assert.All(buttons, b => Assert.Equal("false", b.GetAttribute("aria-pressed")));
    }

    [Fact]
    public async Task Tapping_an_rpe_presses_it_and_saves_it_on_the_entry()
    {
        var (data, cut) = await OpenAsync();

        Rpe(cut, 8).Click();

        Assert.Equal("true", Rpe(cut, 8).GetAttribute("aria-pressed"));
        Assert.Equal("false", Rpe(cut, 9).GetAttribute("aria-pressed"));
        var entry = Assert.Single(Stored(data).Entries);
        Assert.Equal(Squat, entry.ExerciseId);
        Assert.Equal(8, entry.Rpe);
    }

    [Fact]
    public async Task Tapping_the_selected_rpe_clears_it()
    {
        var (data, cut) = await OpenAsync();
        Rpe(cut, 8).Click();

        Rpe(cut, 8).Click();

        Assert.Equal("false", Rpe(cut, 8).GetAttribute("aria-pressed"));
        Assert.Null(Assert.Single(Stored(data).Entries).Rpe);
    }

    [Fact]
    public async Task Rpe_never_blocks_confirm_and_is_kept_with_the_set()
    {
        var (data, cut) = await OpenAsync();

        Confirm(cut); // nothing typed, no RPE: the parser's message shows, RPE is not what blocks it
        Assert.Empty(Stored(data).Entries);

        cut.Find("input[aria-label='Weight, Set 1']").Input("20");
        cut.Find("input[aria-label='Reps, Set 1']").Input("8");
        Confirm(cut);
        Rpe(cut, 7).Click();

        var entry = Assert.Single(Stored(data).Entries);
        Assert.Single(entry.Sets);
        Assert.Equal(7, entry.Rpe);
    }

    [Fact]
    public async Task Rpe_belongs_to_each_exercise_and_the_pressed_state_follows_the_exercise_shown()
    {
        var (_, cut) = await OpenAsync();
        Rpe(cut, 9).Click();

        // Move on to the next Exercise: finish the squat sets then Next.
        for (var set = 1; set <= 3; set++)
        {
            cut.Find($"input[aria-label='Weight, Set {set}']").Input("20");
            cut.Find($"input[aria-label='Reps, Set {set}']").Input("8");
            Confirm(cut);
        }

        Confirm(cut); // Next: Walking lunge
        Assert.All(cut.FindAll(".fa-rpe button"), b => Assert.Equal("false", b.GetAttribute("aria-pressed")));
    }

    [Fact]
    public async Task The_next_suggested_target_reflects_the_saved_rpe()
    {
        var (data, cut) = await OpenAsync();
        cut.Find("input[aria-label='Weight, Set 1']").Input("20");
        cut.Find("input[aria-label='Reps, Set 1']").Input("8");
        Confirm(cut);
        Rpe(cut, 9).Click();
        await data.CommitAsync(Change.Put(Stored(data) with { EndedAt = Now.AddMinutes(30) }));

        var next = await OpenSeededNextAsync(data);

        Assert.Contains("RPE 9", next.Find(".fa-target").TextContent);
        Assert.Contains("hold", next.Find(".fa-target").TextContent);
    }

    private Task<IRenderedComponent<WorkoutLog>> OpenSeededNextAsync(AppData data)
    {
        var plan = Plan();
        var next = Core.Rules.Starting.WorkoutStart.Create(plan, plan.Sessions[0], Today.AddDays(7), new CountingIds(), new FixedClock(Now));
        return data.CommitAsync(Change.Put(next with { Id = "w-next" })).ContinueWith(_ =>
        {
            return Render<WorkoutLog>(p => p.Add(l => l.WorkoutId, "w-next"));
        });
    }
}
