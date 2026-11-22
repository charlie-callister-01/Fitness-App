using Core.Model;
using Core.Rules.Schedule;
using Core.Rules.PlanEditing;
using Core.Tests.Ids;
using Core.Tests.Rules.TestData;
using static Core.Tests.Rules.TestData.Dates;
using static Core.Tests.Rules.TestData.HistoryBuilder;

namespace Core.Tests.Rules.PlanEditing;

public class WeekEditRulesTests
{
    private static readonly DateOnly Start = D(2026, 1, 5);

    private static Exercise Ex(string id, params string[] groups) =>
        new(id, id, LogKind.Load, new ExerciseTags(groups, [], []), false);

    private static readonly IReadOnlyList<Exercise> Library = [Ex("squat", "quads", "glutes"), Ex("rdl", "glutes"), Ex("bench", "chest"), Ex("row", "back")];

    private static Session S(string id, DayOfWeek day, string exercise) =>
        PlanFixture.Session(id, day, exercises: [PlanFixture.Row(exercise, reps: 5)]);

    private static readonly Plan Standard = PlanFixture.Make(start: Start, sessions:
    [
        S("legsA", DayOfWeek.Monday, "squat"), S("push", DayOfWeek.Tuesday, "bench"),
        S("legsB", DayOfWeek.Wednesday, "rdl"), S("pull", DayOfWeek.Thursday, "row"),
    ]);

    private static T Valid<T>(PlanEditResult<T> result) => Assert.IsType<PlanEditResult<T>.Valid>(result).Value;

    private static string Reason<T>(PlanEditResult<T> result) => Assert.IsType<PlanEditResult<T>.Invalid>(result).Message;

    // Moving a Session

    [Fact]
    public void Moving_this_week_only_writes_a_move_into_that_weeks_schedule_and_the_layout_follows()
    {
        var schedule = Valid(WeekEditRules.MoveThisWeek(Standard, [], "push", Mon, Mon.AddDays(4)));

        var layout = WeekScheduleRules.Layout(Standard, [schedule], [], Mon);
        Assert.Equal(Mon, schedule.WeekStart);
        Assert.Equal(Mon.AddDays(4), layout.Sessions.Single(s => s.SessionId == "push").Date);
        Assert.Equal(Mon.AddDays(8), WeekScheduleRules.Layout(Standard, [schedule], [], Mon.AddDays(7)).Sessions.Single(s => s.SessionId == "push").Date);
    }

    [Fact]
    public void A_second_move_of_the_same_Session_replaces_the_first_and_other_moves_are_kept()
    {
        var first = Valid(WeekEditRules.MoveThisWeek(Standard, [], "push", Mon, Mon.AddDays(4)));
        var other = Valid(WeekEditRules.MoveThisWeek(Standard, [first], "pull", Mon, Mon.AddDays(5)));

        var again = Valid(WeekEditRules.MoveThisWeek(Standard, [other], "push", Mon, Mon.AddDays(6)));

        Assert.Equal(
            [("pull", Mon.AddDays(5)), ("push", Mon.AddDays(6))],
            again.Moves.Select(m => (m.SessionId, m.Date)).OrderBy(m => m.SessionId));
    }

    [Fact]
    public void Moving_back_to_the_default_day_removes_the_move()
    {
        var moved = Valid(WeekEditRules.MoveThisWeek(Standard, [], "push", Mon, Mon.AddDays(4)));

        var back = Valid(WeekEditRules.MoveThisWeek(Standard, [moved], "push", Mon, Mon.AddDays(1)));

        Assert.Empty(back.Moves);
    }

    [Fact]
    public void A_day_outside_the_week_or_an_unknown_Session_is_refused()
    {
        Assert.Contains("this week", Reason(WeekEditRules.MoveThisWeek(Standard, [], "push", Mon, Mon.AddDays(7))));
        Assert.Contains("this week", Reason(WeekEditRules.MoveThisWeek(Standard, [], "push", Mon, Mon.AddDays(-1))));
        Assert.Contains("no longer", Reason(WeekEditRules.MoveThisWeek(Standard, [], "gone", Mon, Mon)));
    }

    [Fact]
    public void Two_Sessions_may_share_a_day()
    {
        var schedule = Valid(WeekEditRules.MoveThisWeek(Standard, [], "push", Mon, Mon));

        var layout = WeekScheduleRules.Layout(Standard, [schedule], [], Mon);
        Assert.Equal(2, layout.Sessions.Count(s => s.Date == Mon));
    }

    [Fact]
    public void Making_a_day_the_default_changes_the_Session_in_the_Plan_for_every_week()
    {
        var plan = Valid(WeekEditRules.MakeDefaultDay(Standard, "push", DayOfWeek.Friday));

        Assert.Equal(DayOfWeek.Friday, plan.Sessions.Single(s => s.Id == "push").DefaultDay);
        Assert.Equal(Mon.AddDays(11), WeekScheduleRules.Layout(plan, [], [], Mon.AddDays(7)).Sessions.Single(s => s.SessionId == "push").Date);
        Assert.Contains("no longer", Reason(WeekEditRules.MakeDefaultDay(Standard, "gone", DayOfWeek.Friday)));
    }

    // Breaks

    private static readonly IReadOnlyList<Break> Existing = [new Break("b1", Mon.AddDays(5), Mon.AddDays(8), "Holiday")];

    [Fact]
    public void A_Break_gets_a_fresh_id_and_a_trimmed_reason()
    {
        var created = Valid(WeekEditRules.CreateBreak(Mon, Mon.AddDays(2), "  Flu ", Existing, new SequentialIdGenerator("b")));

        Assert.Equal(("b-1", Mon, Mon.AddDays(2), "Flu"), (created.Id, created.StartDate, created.EndDate, created.Reason));
    }

    [Fact]
    public void A_one_day_Break_and_one_with_no_reason_are_allowed()
    {
        var created = Valid(WeekEditRules.CreateBreak(Mon, Mon, null, [], new SequentialIdGenerator("b")));

        Assert.Equal("", created.Reason);
    }

    [Fact]
    public void A_Break_ending_before_it_starts_is_refused() =>
        Assert.Contains("before it starts", Reason(WeekEditRules.CreateBreak(Mon.AddDays(2), Mon, "x", [], new SequentialIdGenerator("b"))));

    [Theory]
    [InlineData(3, 5)]
    [InlineData(5, 6)]
    [InlineData(7, 12)]
    [InlineData(8, 20)]
    [InlineData(0, 30)]
    public void A_Break_that_shares_any_day_with_an_existing_Break_is_refused(int startOffset, int endOffset) =>
        Assert.Contains("overlaps", Reason(WeekEditRules.CreateBreak(Mon.AddDays(startOffset), Mon.AddDays(endOffset), "x", Existing, new SequentialIdGenerator("b"))));

    [Theory]
    [InlineData(0, 4)]
    [InlineData(9, 12)]
    public void A_Break_next_to_an_existing_Break_is_allowed(int startOffset, int endOffset) =>
        Valid(WeekEditRules.CreateBreak(Mon.AddDays(startOffset), Mon.AddDays(endOffset), "x", Existing, new SequentialIdGenerator("b")));

    // The suggested week

    // Today is Tuesday; a Break covers Wednesday and Thursday, legsA was done, so push, legsB and pull are outstanding.
    private static WeekDraft Draft(DateOnly? today = null, IReadOnlyList<WeekSchedule>? schedules = null) =>
        WeekEditRules.SuggestDraft(Standard, Library, schedules ?? [], [BreakOf(Mon.AddDays(2), Mon.AddDays(3))], [Logged(Mon, "legsA")], today ?? Mon.AddDays(1))!;

    [Fact]
    public void A_workable_week_has_no_draft() =>
        Assert.Null(WeekEditRules.SuggestDraft(Standard, Library, [], [], [Logged(Mon, "legsA")], Mon.AddDays(1)));

    [Fact]
    public void A_draft_has_a_row_for_each_outstanding_Session_with_where_it_is_now_and_where_it_would_go()
    {
        var draft = Draft();

        Assert.Equal(Mon, draft.Monday);
        Assert.Equal(["legsB", "pull", "push"], draft.Rows.Select(r => r.SessionId).Order(StringComparer.Ordinal));
        var legsB = draft.Rows.Single(r => r.SessionId == "legsB");
        Assert.Equal(Mon.AddDays(2), legsB.CurrentDate);
        Assert.NotEqual(legsB.CurrentDate, legsB.ProposedDate);
        Assert.True(legsB.IsChanged);
        Assert.All(draft.Rows, r => Assert.True(r.ProposedDate >= Mon.AddDays(1)));
        Assert.Contains("would move", draft.Summary);
    }

    [Fact]
    public void Undoing_one_row_puts_that_Session_back_and_leaves_the_others()
    {
        var draft = Draft();
        var changedBefore = draft.ChangedRows.Select(r => r.SessionId).ToList();
        var undo = changedBefore[0];

        var after = draft.Undo(undo);

        var row = after.Rows.Single(r => r.SessionId == undo);
        Assert.True(row.IsUndone);
        Assert.False(row.IsChanged);
        Assert.Equal(row.CurrentDate, row.FinalDate);
        Assert.Equal(changedBefore.Skip(1), after.ChangedRows.Select(r => r.SessionId));
        Assert.Equal(changedBefore.Count - 1, after.ChangedRows.Count);
    }

    [Fact]
    public void Applying_writes_moves_only_for_changed_rows_and_the_layout_matches_the_draft()
    {
        var draft = Draft();

        var schedule = draft.Apply(Standard, []);

        var layout = WeekScheduleRules.Layout(Standard, [schedule], [BreakOf(Mon.AddDays(2), Mon.AddDays(3))], Mon);
        foreach (var row in draft.Rows)
        {
            Assert.Equal(row.FinalDate, layout.Sessions.Single(s => s.SessionId == row.SessionId).Date);
        }

        Assert.DoesNotContain(schedule.Moves, m => m.SessionId == "legsA");
    }

    [Fact]
    public void Applying_after_undo_leaves_the_undone_Session_where_it_is()
    {
        var draft = Draft();
        var undo = draft.ChangedRows[0].SessionId;

        var schedule = draft.Undo(undo).Apply(Standard, []);

        Assert.DoesNotContain(schedule.Moves, m => m.SessionId == undo);
    }

    [Fact]
    public void Applying_keeps_existing_moves_of_Sessions_the_draft_does_not_change()
    {
        var existing = new WeekSchedule(Mon, [new SessionMove("legsA", Mon.AddDays(6))]);
        var draft = Draft(schedules: [existing]);

        var schedule = draft.Apply(Standard, [existing]);

        Assert.Contains(schedule.Moves, m => m.SessionId == "legsA" && m.Date == Mon.AddDays(6));
    }

    [Fact]
    public void Proposing_a_Sessions_default_day_writes_no_move_for_it()
    {
        var draft = new WeekDraft(Mon, [new DraftRow("push", "push", Mon.AddDays(3), Mon.AddDays(1), false)], []);

        var schedule = draft.Apply(Standard, [new WeekSchedule(Mon, [new SessionMove("push", Mon.AddDays(3))])]);

        Assert.Empty(schedule.Moves);
    }

    [Fact]
    public void A_draft_changes_nothing_until_applied_so_cancel_is_just_dropping_it()
    {
        var schedules = new List<WeekSchedule>();

        var draft = Draft(schedules: schedules);
        draft.Undo("push");

        Assert.Empty(schedules);
    }

    [Fact]
    public void A_Session_not_yet_due_is_never_Missed_in_a_draft()
    {
        var draft = Draft();

        var missed = WeekScheduleRules.MissedInWeek(Standard, [draft.Apply(Standard, [])], [BreakOf(Mon.AddDays(2), Mon.AddDays(3))], [Logged(Mon, "legsA")], Mon, Mon.AddDays(1));

        Assert.Empty(missed);
    }
}
