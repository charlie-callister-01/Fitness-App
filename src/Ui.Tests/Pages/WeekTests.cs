using Bunit;
using Core.Model;
using Core.Rules.Schedule;
using Core.Rules.Tracks;
using Core.Storage;
using Core.Tests.Storage;
using Microsoft.Extensions.DependencyInjection;
using Ui.Pages;
using Ui.Saving;
using static Ui.Tests.Pages.PageFixture;

namespace Ui.Tests.Pages;

/// <summary>The Week page: rows and chips, Move, Add a Break and the suggested week.</summary>
public sealed class WeekTests : BunitContext
{
    private static readonly DateOnly Wednesday = Today.AddDays(2);
    private readonly SaveFailureNotice _notice = new();

    /// <summary>Lower on Monday and Upper on Wednesday, both for the whole Plan.</summary>
    private static Plan TwoSessionPlan() => Plan(
    [
        Session("lower", "Lower", DayOfWeek.Monday, SessionType.Strength, null, [new SessionExercise(Squat, 3, 8, null, null)]),
        Session("upper", "Upper", DayOfWeek.Wednesday, SessionType.Strength, null, [new SessionExercise(Lunge, 2, 10, null, null)]),
    ]);

    private IRenderedComponent<Week> RenderWeek(AppData data, int daysLater = 0)
    {
        Wire(this, data, _notice);
        Services.GetRequiredService<TestClock>().Advance(TimeSpan.FromDays(daysLater));
        return Render<Week>();
    }

    private static void ClickInSheet(IRenderedComponent<Week> cut, string text) =>
        cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == text).Click();

    private static void ClickRowMove(IRenderedComponent<Week> cut, string session) =>
        cut.Find($"button[aria-label='Move {session}']").Click();

    private static IReadOnlyList<string> RowNames(IRenderedComponent<Week> cut) =>
        [.. cut.FindAll("ul[aria-label='This week'] li").Select(li => li.QuerySelector(".fa-week__name")!.TextContent)];

    private static string ChipOfRow(IRenderedComponent<Week> cut, string session) =>
        cut.Find($"ul[aria-label='This week'] li[data-session='{session}'] .fa-chip__word").TextContent;

    private static IReadOnlyList<WeekSchedule> Schedules(AppData data) => data.Snapshot.WeekSchedules;

    private static Break TuesdayToWednesday() => new("b1", Today.AddDays(1), Today.AddDays(2), "Flu");

    private async Task<AppData> OpenLockedAsync(InMemoryDataStore store) =>
        await AppData.OpenAsync(store, new InMemoryStorageEnvironment { WriterLock = WriterLockResult.HeldElsewhere });

    // Rows and chips.

    [Fact]
    public async Task Rows_show_the_day_the_Session_and_a_Done_Missed_or_Today_chip()
    {
        var done = Finished("w-wed", Wednesday, Set("s1", 20, 8)) with { SessionId = "upper" };
        var data = await OpenSeededAsync(plan: TwoSessionPlan(), workouts: done);
        var cut = RenderWeek(data, daysLater: 2);

        Assert.Equal(["Lower", "Upper"], RowNames(cut));
        Assert.Contains("Mon 16 Nov", cut.Markup);
        Assert.Equal("Missed", ChipOfRow(cut, "lower"));
        Assert.Equal("Done", ChipOfRow(cut, "upper"));
    }

    [Fact]
    public async Task Todays_row_wears_the_Today_chip_and_a_later_one_wears_none()
    {
        var cut = RenderWeek(await OpenSeededAsync(plan: TwoSessionPlan()));

        Assert.Equal("Today", ChipOfRow(cut, "lower"));
        Assert.Empty(cut.FindAll("li[data-session='upper'] .fa-chip"));
    }

    [Fact]
    public async Task With_no_active_Plan_the_page_says_so()
    {
        var cut = RenderWeek(await OpenSeededAsync());

        Assert.Contains("No active Plan", cut.Markup);
        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Contains("Add a Break"));
    }

    // Move.

    [Fact]
    public async Task Moving_this_week_only_writes_the_Week_schedule_and_leaves_the_Plan_and_next_week_alone()
    {
        var data = await OpenSeededAsync(plan: TwoSessionPlan());
        var cut = RenderWeek(data);

        ClickRowMove(cut, "Upper");
        ClickInSheet(cut, "Fri 20 Nov");
        ClickInSheet(cut, "Move");

        cut.WaitForAssertion(() => Assert.Single(Schedules(data)));
        var move = Assert.Single(Schedules(data)[0].Moves);
        Assert.Equal(("upper", Today.AddDays(4)), (move.SessionId, move.Date));
        var plan = data.Snapshot.Plans.Single();
        Assert.Equal(DefaultDay.Wednesday, plan.Sessions.Single(s => s.Id == "upper").DefaultDay);
        var nextWeek = WeekScheduleRules.Layout(plan, TrackRules.Main(plan), Schedules(data), [], Today.AddDays(7));
        Assert.Equal(Wednesday.AddDays(7), nextWeek.Sessions.Single(s => s.SessionId == "upper").Date);
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[role='dialog']")));
        Assert.Contains("Moved this week", cut.Markup);
    }

    [Fact]
    public async Task Making_a_day_the_default_changes_the_Plan_so_next_week_follows()
    {
        var data = await OpenSeededAsync(plan: TwoSessionPlan());
        var cut = RenderWeek(data);

        ClickRowMove(cut, "Upper");
        ClickInSheet(cut, "Fri 20 Nov");
        cut.FindAll("[role='dialog'] input[type='radio']")[1].Change(true);
        ClickInSheet(cut, "Move");

        cut.WaitForAssertion(() => Assert.Equal(DefaultDay.Friday, data.Snapshot.Plans.Single().Sessions.Single(s => s.Id == "upper").DefaultDay));
        Assert.Empty(Schedules(data));
        var savedPlan = data.Snapshot.Plans.Single();
        var nextWeek = WeekScheduleRules.Layout(savedPlan, TrackRules.Main(savedPlan), [], [], Today.AddDays(7));
        Assert.Equal(Today.AddDays(11), nextWeek.Sessions.Single(s => s.SessionId == "upper").Date);
        cut.WaitForAssertion(() => Assert.Contains("Fri 20 Nov", cut.Find("li[data-session='upper']").TextContent));
    }

    [Fact]
    public async Task A_failed_Move_keeps_the_sheet_and_choices_and_writes_nothing()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        var data = await OpenSeededAsync(store, TwoSessionPlan());
        var cut = RenderWeek(data);
        ClickRowMove(cut, "Upper");
        ClickInSheet(cut, "Fri 20 Nov");
        store.FailAllWrites(StoreFailureKind.StorageFull);

        ClickInSheet(cut, "Move");

        cut.WaitForAssertion(() => Assert.Contains("out of storage", _notice.Message));
        Assert.Empty(Schedules(data));
        Assert.NotEmpty(cut.FindAll("[role='dialog']"));
        Assert.Equal("true", cut.Find("[role='dialog'] button[aria-label='Fri 20 Nov']").GetAttribute("aria-pressed"));
    }

    // Break.

    private static void FillBreak(IRenderedComponent<Week> cut, string from, string to, string reason = "")
    {
        var inputs = cut.FindAll("[role='dialog'] input");
        inputs[0].Input(from);
        inputs[1].Input(to);
        inputs[2].Input(reason);
    }

    [Fact]
    public async Task Adding_a_Break_stores_it_and_Home_goes_quiet()
    {
        var data = await OpenSeededAsync(plan: TwoSessionPlan());
        var cut = RenderWeek(data);

        cut.FindAll("button").First(b => b.TextContent.Trim() == "Add a Break").Click();
        Assert.Contains("Streak is protected", cut.Find("[role='dialog']").TextContent);
        FillBreak(cut, "2026-11-16", "2026-11-18", "  Flu ");
        ClickInSheet(cut, "Start Break");

        cut.WaitForAssertion(() => Assert.Single(data.Snapshot.Breaks));
        var brk = data.Snapshot.Breaks[0];
        Assert.Equal((Today, Wednesday, "Flu"), (brk.StartDate, brk.EndDate, brk.Reason));
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[role='dialog']")));
        Assert.Contains("On a Break", Render<Home>().Markup);
    }

    [Fact]
    public async Task A_Break_that_overlaps_another_is_refused_with_the_Core_message_and_the_form_is_kept()
    {
        var data = await OpenSeededAsync(plan: TwoSessionPlan());
        await data.CommitAsync([Change.Put(new Break("old", Today.AddDays(3), Today.AddDays(5), ""))]);
        var cut = RenderWeek(data);
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Add a Break").Click();
        FillBreak(cut, "2026-11-18", "2026-11-20", "Trip");

        ClickInSheet(cut, "Start Break");

        cut.WaitForAssertion(() => Assert.Contains("That overlaps your Break from 19 Nov 2026 to 21 Nov 2026.", cut.Find("[role='dialog'] [role='alert']").TextContent));
        Assert.Single(data.Snapshot.Breaks);
        Assert.Equal("Trip", cut.FindAll("[role='dialog'] input")[2].GetAttribute("value"));
    }

    [Fact]
    public async Task A_Break_that_ends_before_it_starts_is_refused_and_nothing_is_stored()
    {
        var data = await OpenSeededAsync(plan: TwoSessionPlan());
        var cut = RenderWeek(data);
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Add a Break").Click();
        FillBreak(cut, "2026-11-20", "2026-11-18");

        ClickInSheet(cut, "Start Break");

        cut.WaitForAssertion(() => Assert.Contains("A Break cannot end before it starts.", cut.Find("[role='dialog'] [role='alert']").TextContent));
        Assert.Empty(data.Snapshot.Breaks);
    }

    [Fact]
    public async Task A_missing_date_is_refused_in_plain_words()
    {
        var cut = RenderWeek(await OpenSeededAsync(plan: TwoSessionPlan()));
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Add a Break").Click();
        FillBreak(cut, "", "2026-11-18");

        ClickInSheet(cut, "Start Break");

        cut.WaitForAssertion(() => Assert.Contains("Pick both dates.", cut.Find("[role='dialog'] [role='alert']").TextContent));
    }

    // The suggested week. A Break on Tuesday and Wednesday leaves Upper (Wednesday) on a Break day, so it is proposed for a later day.

    private async Task<(AppData Data, IRenderedComponent<Week> Cut)> RenderDraftAsync(IDataStore? store = null)
    {
        var data = await OpenSeededAsync(store, TwoSessionPlan());
        await data.CommitAsync([Change.Put(TuesdayToWednesday())]);
        return (data, RenderWeek(data));
    }

    private static IReadOnlyList<string> ChangedDraftRows(IRenderedComponent<Week> cut) =>
        [.. cut.FindAll(".fa-week__draftrow--changed").Select(r => r.GetAttribute("data-session")!)];

    [Fact]
    public async Task The_draft_shows_one_summary_line_and_highlights_the_changed_rows()
    {
        var (_, cut) = await RenderDraftAsync();

        var changed = ChangedDraftRows(cut);
        Assert.Contains("upper", changed);
        var summary = cut.Find(".fa-week__summary").TextContent;
        Assert.Equal(changed.Count == 1 ? "1 Session would move." : $"{changed.Count} Sessions would move.", summary);
        Assert.Single(cut.FindAll(".fa-week__summary"));
    }

    [Fact]
    public async Task Undo_puts_a_row_back_and_the_summary_follows()
    {
        var (_, cut) = await RenderDraftAsync();
        var before = ChangedDraftRows(cut).Count;

        cut.Find("button[aria-label='Undo move of Upper']").Click();

        Assert.DoesNotContain("upper", ChangedDraftRows(cut));
        Assert.Equal(before - 1, ChangedDraftRows(cut).Count);
        Assert.Empty(cut.FindAll("button[aria-label='Undo move of Upper']"));
    }

    [Fact]
    public async Task Cancel_closes_the_draft_and_writes_nothing()
    {
        var (data, cut) = await RenderDraftAsync();

        cut.FindAll("button").First(b => b.TextContent.Trim() == "Cancel").Click();

        Assert.Empty(cut.FindAll(".fa-week__draft"));
        Assert.Empty(Schedules(data));
    }

    [Fact]
    public async Task Apply_stores_the_Week_schedule_in_one_save_and_the_draft_goes_away()
    {
        var counting = new CountingStore(new InMemoryDataStore());
        var (data, cut) = await RenderDraftAsync(counting);
        var writesBefore = counting.Writes;
        var expected = cut.FindAll(".fa-week__draftrow--changed").Count;

        cut.FindAll("button").First(b => b.TextContent.Trim() == "Apply").Click();

        cut.WaitForAssertion(() => Assert.Single(Schedules(data)));
        Assert.Equal(1, counting.Writes - writesBefore);
        Assert.True(Schedules(data)[0].Moves.Count >= 1 && Schedules(data)[0].Moves.Count <= expected);
        Assert.DoesNotContain(Schedules(data)[0].Moves, m => InBreak(m.Date));
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".fa-week__draft")));
        Assert.DoesNotContain("Missed", cut.Markup);
    }

    private static bool InBreak(DateOnly date) => date >= Today && date <= Today.AddDays(1);

    [Fact]
    public async Task A_double_tap_on_Apply_saves_once()
    {
        var gated = new CountingStore(new InMemoryDataStore());
        var (data, cut) = await RenderDraftAsync(gated);
        var writesBefore = gated.Writes;
        gated.Hold();
        var apply = cut.FindAll("button").First(b => b.TextContent.Trim() == "Apply");

        apply.Click();
        apply.Click();
        gated.Release();

        cut.WaitForAssertion(() => Assert.Single(Schedules(data)));
        Assert.Equal(1, gated.Writes - writesBefore);
    }

    [Fact]
    public async Task A_failed_Apply_keeps_the_draft_on_screen()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        var (data, cut) = await RenderDraftAsync(store);
        var changed = ChangedDraftRows(cut);
        store.FailAllWrites(StoreFailureKind.StorageFull);

        cut.FindAll("button").First(b => b.TextContent.Trim() == "Apply").Click();

        cut.WaitForAssertion(() => Assert.Contains("out of storage", _notice.Message));
        Assert.Empty(Schedules(data));
        Assert.Equal(changed, ChangedDraftRows(cut));
        Assert.NotEmpty(cut.FindAll(".fa-week__draft"));
    }

    [Fact]
    public async Task A_failed_Apply_keeps_the_earlier_Undo_and_shows_the_notice()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        var data = await OpenSeededAsync(store, TwoSessionPlan());
        await data.CommitAsync([Change.Put(new Break("b1", Today, Today.AddDays(2), "Flu"))]);
        var cut = RenderWeek(data);
        Assert.Equal(2, ChangedDraftRows(cut).Count);
        cut.Find("button[aria-label='Undo move of Upper']").Click();
        var changed = ChangedDraftRows(cut);
        store.FailAllWrites(StoreFailureKind.StorageFull);

        cut.FindAll("button").First(b => b.TextContent.Trim() == "Apply").Click();

        cut.WaitForAssertion(() => Assert.Contains("out of storage", _notice.Message));
        Assert.Empty(Schedules(data));
        Assert.Equal(changed, ChangedDraftRows(cut));
        Assert.Contains("lower", ChangedDraftRows(cut));
        Assert.DoesNotContain("upper", ChangedDraftRows(cut));
        Assert.NotEmpty(cut.FindAll(".fa-week__draft"));
    }

    [Fact]
    public async Task Cancel_stays_put_on_a_new_page_load_until_a_Break_is_added()
    {
        var (data, cut) = await RenderDraftAsync();
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Cancel").Click();

        var reloaded = Render<Week>();
        Assert.Empty(reloaded.FindAll(".fa-week__draft"));

        await data.CommitAsync([Change.Put(new Break("b2", Today.AddDays(4), Today.AddDays(4), "Trip"))]);

        reloaded.WaitForAssertion(() => Assert.NotEmpty(reloaded.FindAll(".fa-week__draft")));
        Assert.NotEmpty(Render<Week>().FindAll(".fa-week__draft"));
    }

    [Fact]
    public async Task A_week_with_no_Break_offers_no_draft_even_with_a_Missed_Session()
    {
        var cut = RenderWeek(await OpenSeededAsync(plan: TwoSessionPlan()), daysLater: 2);

        Assert.Empty(cut.FindAll(".fa-week__draft"));
        Assert.Equal("Missed", ChipOfRow(cut, "lower"));
    }

    // Read-only.

    [Fact]
    public async Task Read_only_disables_Move_Add_a_Break_and_Apply_and_says_why()
    {
        var store = new InMemoryDataStore();
        var seeded = await OpenSeededAsync(store, TwoSessionPlan());
        await seeded.CommitAsync([Change.Put(TuesdayToWednesday())]);
        var locked = await OpenLockedAsync(store);
        var cut = RenderWeek(locked);

        Assert.NotNull(cut.Find("p[role='status']"));
        Assert.True(cut.Find("button[aria-label='Move Upper']").HasAttribute("disabled"));
        Assert.True(cut.FindAll("button").First(b => b.TextContent.Trim() == "Add a Break").HasAttribute("disabled"));
        Assert.True(cut.FindAll("button").First(b => b.TextContent.Trim() == "Apply").HasAttribute("disabled"));
        Assert.Empty(Schedules(locked));
    }

    /// <summary>A store that counts writes and can hold them until released, to prove a double tap saves once.</summary>
    private sealed class CountingStore(IDataStore inner) : IDataStore
    {
        private TaskCompletionSource? _gate;

        public int Writes { get; private set; }

        public void Hold() => _gate = new TaskCompletionSource();

        public void Release() => _gate?.SetResult();

        public Task<StoreResult<StoredData>> LoadAllAsync() => inner.LoadAllAsync();

        public async Task<StoreResult<Unit>> ApplyAsync(IReadOnlyList<StoreChange> changes, StoreMetadata? metadata = null)
        {
            Writes++;
            if (_gate is { } gate)
            {
                await gate.Task;
            }

            return await inner.ApplyAsync(changes, metadata);
        }

        public Task<StoreResult<Unit>> ReplaceAllAsync(IReadOnlyList<StoredRecord> records, StoreMetadata? metadata = null) =>
            inner.ReplaceAllAsync(records, metadata);

        public Task<StoreResult<Unit>> WriteMetadataAsync(StoreMetadata metadata) => inner.WriteMetadataAsync(metadata);
    }
}
