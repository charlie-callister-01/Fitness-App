using Core.Model;
using Core.Rules.Nudges;
using Core.Tests.Rules.TestData;
using static Core.Tests.Rules.TestData.Dates;
using static Core.Tests.Rules.TestData.HistoryBuilder;

namespace Core.Tests.Rules.Nudges;

public class NudgeRulesTests
{
    // Plan starts Mon 2026-01-05, target 3. Session "a-lower" (Lower) is on Mondays. Mon 2026-11-16 starts the week under test.
    private static readonly DateOnly Tue = Mon.AddDays(1);
    private static readonly DateOnly Wed = Mon.AddDays(2);
    private static readonly DateOnly Thu = Mon.AddDays(3);
    private static readonly DateTimeOffset RecentBackup = new(2026, 11, 15, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset OldBackup = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static Plan LowerOnly(Plan plan) => plan with { Sessions = [plan.Sessions[0]] };

    private static IReadOnlyList<Nudge> Compute(HistoryBuilder h, DateOnly today, DateTimeOffset? backup = null,
        params Workout[] extra) =>
        NudgeRules.Compute(LowerOnly(h.Plan), [], h.Breaks, [.. h.Workouts, .. extra], backup ?? RecentBackup, today);

    private static HistoryBuilder Done(params DateOnly[] dates) => new HistoryBuilder().Did(dates);

    [Fact]
    public void A_missed_session_this_week_gets_a_nudge_naming_it()
    {
        var n = Assert.Single(Compute(new HistoryBuilder(), Tue));
        Assert.Equal(NudgeKind.MissedSession, n.Kind);
        Assert.Equal(PlanFixture.Strength, n.SessionId);
        Assert.Null(n.Area);
        Assert.Contains("Lower", n.Message);
    }

    [Fact]
    public void Doing_the_session_clears_its_missed_nudge()
    {
        Assert.Empty(Compute(Done(Mon), Tue));
    }

    [Fact]
    public void One_workout_short_of_target_fires_from_thursday()
    {
        var h = Done(Mon, Tue);
        Assert.Empty(Compute(h, Wed));
        var n = Assert.Single(Compute(h, Thu));
        Assert.Equal(NudgeKind.OneShortOfTarget, n.Kind);
        Assert.Null(n.SessionId);
    }

    [Fact]
    public void Two_short_or_target_hit_gives_no_short_nudge()
    {
        Assert.Empty(Compute(Done(Mon), Thu));
        Assert.Empty(Compute(Done(Mon, Tue, Wed), Thu));
    }

    [Fact]
    public void The_same_area_reported_twice_in_a_week_nudges_once_with_the_area()
    {
        var n = Assert.Single(Compute(Done(Mon), Tue, null,
            Pain(Mon, BodyArea.Shoulder), Pain(Tue, BodyArea.Shoulder, BodyArea.Neck)));
        Assert.Equal(NudgeKind.RepeatedPain, n.Kind);
        Assert.Equal(BodyArea.Shoulder, n.Area);
    }

    [Fact]
    public void One_report_or_reports_in_different_weeks_do_not_nudge()
    {
        Assert.Empty(Compute(Done(Mon), Tue, null, Pain(Mon, BodyArea.Shoulder)));
        Assert.Empty(Compute(Done(Mon), Tue, null, Pain(Mon.AddDays(-1), BodyArea.Shoulder), Pain(Mon, BodyArea.Shoulder)));
    }

    [Fact]
    public void Two_repeated_areas_each_get_a_nudge()
    {
        var nudges = Compute(Done(Mon), Tue, null,
            Pain(Mon, BodyArea.Shoulder, BodyArea.Neck), Pain(Tue, BodyArea.Shoulder, BodyArea.Neck));
        Assert.Equal([BodyArea.Neck, BodyArea.Shoulder], nudges.Select(n => n.Area));
    }

    [Fact]
    public void Backup_nudge_fires_at_exactly_14_days_and_not_at_13()
    {
        var h = Done(Mon);
        var thirteenDaysAgo = new DateTimeOffset(2026, 11, 3, 23, 0, 0, TimeSpan.Zero);
        Assert.Empty(Compute(h, Mon, thirteenDaysAgo));
        var n = Assert.Single(Compute(h, Mon, thirteenDaysAgo.AddDays(-1)));
        Assert.Equal(NudgeKind.NoRecentBackup, n.Kind);
    }

    [Fact]
    public void Backup_age_counts_calendar_days_in_the_offset_the_backup_was_made_in()
    {
        // 01:00 on 4 Nov at +10:00 is still 3 Nov in UTC; the owner's calendar day is the 4th, so 14 days is the 18th.
        var backup = new DateTimeOffset(2026, 11, 4, 1, 0, 0, TimeSpan.FromHours(10));
        var h = Done(Mon);
        Assert.Empty(NudgeRules.Compute(new HistoryBuilder().Plan with { Sessions = [] }, [], [], h.Workouts, backup, Tue));
        var n = Assert.Single(NudgeRules.Compute(new HistoryBuilder().Plan with { Sessions = [] }, [], [], h.Workouts, backup, Wed));
        Assert.Equal(NudgeKind.NoRecentBackup, n.Kind);
    }

    [Fact]
    public void Never_having_backed_up_nudges_for_a_backup()
    {
        var h = Done(Mon);
        var n = Assert.Single(NudgeRules.Compute(LowerOnly(h.Plan), [], [], h.Workouts, null, Tue));
        Assert.Equal(NudgeKind.NoRecentBackup, n.Kind);
    }

    [Fact]
    public void A_break_today_silences_every_nudge()
    {
        var h = Done(Mon).Break(Tue, Tue.AddDays(1));
        Assert.Empty(Compute(h, Tue, OldBackup, Pain(Mon, BodyArea.Neck), Pain(Tue, BodyArea.Neck)));
    }

    [Fact]
    public void Order_is_pain_then_missed_by_date_then_short_then_backup_without_duplicates()
    {
        var plan = new HistoryBuilder().Plan with
        {
            Sessions =
            [
                new Session("s-wed", "Wed", DayOfWeek.Wednesday, SessionType.Strength, null, []),
                new Session("s-mon", "Mon", DayOfWeek.Monday, SessionType.Strength, null, []),
            ],
        };
        // Two counted Custom-free Workouts of an unrelated Session keep the target one short without clearing the misses.
        var counted = new HistoryBuilder().Did(Tue, Wed).Workouts.Select(w => w with { SessionId = "other" });
        Workout[] workouts = [.. counted, Pain(Mon, BodyArea.Knee), Pain(Tue, BodyArea.Knee)];

        var nudges = NudgeRules.Compute(plan, [], [], workouts, OldBackup, Thu);

        Assert.Equal(
            [NudgeKind.RepeatedPain, NudgeKind.MissedSession, NudgeKind.MissedSession, NudgeKind.OneShortOfTarget, NudgeKind.NoRecentBackup],
            nudges.Select(n => n.Kind));
        Assert.Equal(["s-mon", "s-wed"], nudges.Where(n => n.SessionId is not null).Select(n => n.SessionId));
        Assert.Equal(nudges.Count, nudges.Distinct().Count());
    }

    // C-08 copy review (Insight ticket 10): neutral (no exclamation, no blame), specific (names the Session, day, area or number), short.
    [Fact]
    public void Every_Nudge_kind_has_neutral_specific_copy_with_no_exclamation_and_one_sentence_ask_at_most()
    {
        var plan = new HistoryBuilder().Plan with
        {
            Sessions = [new Session("s-mon", "Lower", DayOfWeek.Monday, SessionType.Strength, null, [])],
        };
        var counted = new HistoryBuilder().Did(Tue, Wed).Workouts.Select(w => w with { SessionId = "other" });
        Workout[] workouts = [.. counted, Pain(Mon, BodyArea.Knee), Pain(Tue, BodyArea.Knee)];

        var nudges = NudgeRules.Compute(plan, [], [], workouts, OldBackup, Thu);

        Assert.Equal("Knee pain reported twice this week. Consider an easier variation.", nudges.Single(n => n.Kind == NudgeKind.RepeatedPain).Message);
        Assert.Equal("You missed Lower on Monday. Make it up this week?", nudges.Single(n => n.Kind == NudgeKind.MissedSession).Message);
        Assert.Equal("One workout short of your target of 3 this week.", nudges.Single(n => n.Kind == NudgeKind.OneShortOfTarget).Message);
        Assert.Equal("No Backup in 14 days. Make one so you don't lose your data.", nudges.Single(n => n.Kind == NudgeKind.NoRecentBackup).Message);
        Assert.All(nudges, n => Assert.DoesNotContain('!', n.Message));
        Assert.All(nudges, n => Assert.True(n.Message.Length <= 70, n.Message));
    }
}
