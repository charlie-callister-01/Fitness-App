using Core.Model;
using Core.Rules.Phases;
using Core.Rules.Tracks;
using Core.Tests.Rules.TestData;

namespace Core.Tests.Rules.Phases;

public class PhaseRulesTests
{
    // Plan starts Mon 2026-01-05. Phase A = weeks 1-4, Phase B = weeks 5-6.
    private static readonly DateOnly Start = PlanFixture.DefaultStart;

    private static SessionExercise Row(string id, int sets = 3) => PlanFixture.Row(id, sets);

    private static Plan MakePlan(params Phase[] phases) => PlanFixture.Make(phases: phases, sessions:
    [
        PlanFixture.Session("s1", DayOfWeek.Monday, name: "Lower", exercises: [Row("squat"), Row("lunge")]),
    ]);

    private static Phase PhaseOf(string id, int from, int to, PhaseGoal[]? goals = null, SessionOverride[]? overrides = null) =>
        new(id, id, from, to, goals ?? [], overrides ?? []);

    private static readonly Plan TwoPhases = MakePlan(PhaseOf("A", 1, 4), PhaseOf("B", 5, 6));

    // ---- position

    [Fact]
    public void Before_the_start_date_is_an_explicit_state()
    {
        var p = PhaseRules.Position(TwoPhases, TrackRules.Main(TwoPhases), Start.AddDays(-1));
        Assert.Equal(PhaseState.BeforeStart, p.State);
        Assert.Null(p.Phase);
        Assert.Null(p.PlanWeek);
    }

    [Fact]
    public void First_day_is_week_1_of_the_first_Phase()
    {
        var p = PhaseRules.Position(TwoPhases, TrackRules.Main(TwoPhases), Start);
        Assert.Equal(PhaseState.InPhase, p.State);
        Assert.Equal("A", p.Phase!.Id);
        Assert.Equal((1, 1, 4), (p.PlanWeek, p.WeekInPhase, p.PhaseLengthWeeks));
    }

    [Theory]
    [InlineData(27, "A", 4, 4)] // Sunday of week 4
    [InlineData(28, "B", 5, 1)] // Monday of week 5
    [InlineData(41, "B", 6, 2)] // Sunday of week 6
    public void Phase_boundaries_switch_on_the_Monday(int dayOffset, string phase, int planWeek, int weekInPhase)
    {
        var p = PhaseRules.Position(TwoPhases, TrackRules.Main(TwoPhases), Start.AddDays(dayOffset));
        Assert.Equal(PhaseState.InPhase, p.State);
        Assert.Equal(phase, p.Phase!.Id);
        Assert.Equal((planWeek, weekInPhase), (p.PlanWeek, p.WeekInPhase));
    }

    [Fact]
    public void Week_three_of_eleven()
    {
        var plan = MakePlan(PhaseOf("Long", 1, 11));
        var p = PhaseRules.Position(plan, TrackRules.Main(plan), Start.AddDays(14));
        Assert.Equal((3, 11), (p.WeekInPhase, p.PhaseLengthWeeks));
    }

    [Fact]
    public void After_the_last_Phase_is_an_explicit_state_and_keeps_the_plan_week()
    {
        var p = PhaseRules.Position(TwoPhases, TrackRules.Main(TwoPhases), Start.AddDays(42));
        Assert.Equal(PhaseState.AfterEnd, p.State);
        Assert.Null(p.Phase);
        Assert.Equal(7, p.PlanWeek);
    }

    [Fact]
    public void A_week_in_a_gap_between_Phases_has_no_Phase()
    {
        var plan = MakePlan(PhaseOf("A", 1, 2), PhaseOf("B", 4, 5));
        var p = PhaseRules.Position(plan, TrackRules.Main(plan), Start.AddDays(14));
        Assert.Equal(PhaseState.NoPhase, p.State);
        Assert.Equal(3, p.PlanWeek);
    }

    [Fact]
    public void A_Plan_with_no_Phases_has_no_Phase()
    {
        var plan = MakePlan();
        Assert.Equal(PhaseState.NoPhase, PhaseRules.Position(plan, TrackRules.Main(plan), Start).State);
    }

    // ---- goal readiness

    private static readonly PhaseGoal Squat100x5 = new("squat", 100, 5);

    private static PhaseReadiness Readiness(Phase phase, HistoryBuilder h) => PhaseRules.GoalReadiness(phase, h.Workouts);

    [Fact]
    public void Goal_met_by_exact_set()
    {
        var h = new HistoryBuilder().Done("w1", "2026-02-01", "squat", HistoryBuilder.Set(100, 5));
        var r = Readiness(PhaseOf("A", 1, 4, [Squat100x5]), h);
        Assert.True(r.AllMet);
        Assert.True(Assert.Single(r.Goals).Met);
    }

    [Fact]
    public void Goal_met_by_a_heavier_set_with_enough_reps()
    {
        var h = new HistoryBuilder().Done("w1", "2026-02-01", "squat", HistoryBuilder.Set(110, 6));
        Assert.True(Readiness(PhaseOf("A", 1, 4, [Squat100x5]), h).AllMet);
    }

    [Fact]
    public void Goal_met_by_a_qualifying_set_even_when_another_set_ranks_higher()
    {
        var h = new HistoryBuilder().Done("w1", "2026-02-01", "squat", HistoryBuilder.Set(150, 1), HistoryBuilder.Set(100, 5));
        Assert.True(Readiness(PhaseOf("A", 1, 4, [Squat100x5]), h).AllMet);
    }

    [Fact]
    public void Heavier_but_too_few_reps_or_enough_reps_but_too_light_does_not_meet_it()
    {
        var h = new HistoryBuilder().Done("w1", "2026-02-01", "squat", HistoryBuilder.Set(120, 3), HistoryBuilder.Set(90, 10));
        Assert.False(Readiness(PhaseOf("A", 1, 4, [Squat100x5]), h).AllMet);
    }

    [Fact]
    public void Skipped_entries_do_not_count()
    {
        var h = new HistoryBuilder().Skipped("w1", "2026-02-01", "squat");
        Assert.False(Readiness(PhaseOf("A", 1, 4, [Squat100x5]), h).AllMet);
    }

    [Fact]
    public void Reports_which_goals_are_met_and_a_message()
    {
        var goals = new[] { Squat100x5, new PhaseGoal("bench", 80, 5) };
        var h = new HistoryBuilder().Done("w1", "2026-02-01", "squat", HistoryBuilder.Set(100, 5)).Done("w2", "2026-02-02", "bench", HistoryBuilder.Set(70, 5));
        var r = Readiness(PhaseOf("A", 1, 4, goals), h);
        Assert.False(r.AllMet);
        Assert.Equal([true, false], r.Goals.Select(g => g.Met));
        Assert.Contains("1 of 2", r.Message);

        var all = Readiness(PhaseOf("A", 1, 4, goals), h.Done("w3", "2026-02-03", "bench", HistoryBuilder.Set(80, 5)));
        Assert.True(all.AllMet);
        Assert.Contains("move on", all.Message);
    }

    [Fact]
    public void A_Phase_without_goals_is_never_ready()
    {
        var r = Readiness(PhaseOf("A", 1, 4), new HistoryBuilder());
        Assert.False(r.AllMet);
        Assert.Empty(r.Goals);
        Assert.Contains("no goals", r.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ---- row resolution

    private static readonly SessionOverride Override = new("s1", [Row("deadlift", 5)]);

    [Fact]
    public void Rows_use_the_current_Phase_Override_when_there_is_one()
    {
        var plan = MakePlan(PhaseOf("A", 1, 4, overrides: [Override]), PhaseOf("B", 5, 6));
        var rows = PhaseRules.ExerciseRows(plan, "s1", Start.AddDays(7));
        Assert.Equal(["deadlift"], rows.Select(r => r.ExerciseId));
    }

    [Fact]
    public void Rows_use_the_Plan_default_when_the_Phase_has_no_Override_for_that_Session()
    {
        var plan = MakePlan(PhaseOf("A", 1, 4, overrides: [Override]), PhaseOf("B", 5, 6));
        Assert.Equal(["squat", "lunge"], PhaseRules.ExerciseRows(plan, "s1", Start.AddDays(28)).Select(r => r.ExerciseId));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100)]
    public void Rows_use_the_Plan_default_before_the_start_and_after_the_end(int dayOffset)
    {
        var plan = MakePlan(PhaseOf("A", 1, 4, overrides: [Override]));
        Assert.Equal(["squat", "lunge"], PhaseRules.ExerciseRows(plan, "s1", Start.AddDays(dayOffset)).Select(r => r.ExerciseId));
    }

    [Fact]
    public void Unknown_Session_has_no_rows()
    {
        Assert.Empty(PhaseRules.ExerciseRows(TwoPhases, "nope", Start));
    }

    // ---- per-Track Phases

    [Fact]
    public void Each_Track_uses_its_own_Phases_with_Plan_wide_week_numbering()
    {
        var gym = new Track("gym", "Gym", new TrackRhythm.Weekly(3, 2, false),
            [PhaseOf("g1", 1, 10, overrides: [new SessionOverride("s-gym", [Row("front-squat")])])]);
        var mobility = new Track("mob", "Mobility", new TrackRhythm.Daily(),
            [PhaseOf("m1", 1, 4, overrides: [new SessionOverride("s-mob", [Row("hip-90-90")])]),
             PhaseOf("m2", 5, 8)]);
        var emptyTrack = new Track("run", "Run", new TrackRhythm.Weekly(2, 1, false), []);

        var plan = PlanFixture.Make(start: Start, tracks: [gym, mobility, emptyTrack], sessions:
        [
            PlanFixture.Session("s-gym", DayOfWeek.Monday, trackId: "gym", exercises: [Row("squat")]),
            PlanFixture.Session("s-mob", DefaultDay.Daily, SessionType.Mobility, trackId: "mob", exercises: [Row("stretch")]),
            PlanFixture.Session("s-run", DayOfWeek.Wednesday, SessionType.Cardio, trackId: "run", exercises: [Row("jog")]),
        ]);

        var week3 = Start.AddDays(14); // Plan week 3
        var gymPos = PhaseRules.Position(plan, gym, week3);
        var mobPos = PhaseRules.Position(plan, mobility, week3);
        var runPos = PhaseRules.Position(plan, emptyTrack, week3);

        Assert.Equal(("g1", 3, 10), (gymPos.Phase!.Id, gymPos.WeekInPhase, gymPos.PhaseLengthWeeks));
        Assert.Equal(("m1", 3, 4), (mobPos.Phase!.Id, mobPos.WeekInPhase, mobPos.PhaseLengthWeeks));
        Assert.Equal(PhaseState.NoPhase, runPos.State);
        Assert.Null(PhaseRules.CurrentPhase(plan, emptyTrack, week3));

        // Week 5: mobility switches to m2 (no override -> default rows), gym stays in g1 (override active).
        var week5 = Start.AddDays(28);
        Assert.Equal("m2", PhaseRules.CurrentPhase(plan, mobility, week5)!.Id);
        Assert.Equal(["front-squat"], PhaseRules.ExerciseRows(plan, "s-gym", week5).Select(r => r.ExerciseId));
        Assert.Equal(["stretch"], PhaseRules.ExerciseRows(plan, "s-mob", week5).Select(r => r.ExerciseId));
        Assert.Equal(["hip-90-90"], PhaseRules.ExerciseRows(plan, "s-mob", week3).Select(r => r.ExerciseId));
    }

    [Fact]
    public void Goal_readiness_and_move_to_next_Phase_follow_the_Tracks_own_Phase_goals()
    {
        var main = new Track("main", "Main", new TrackRhythm.Weekly(3, 2, false),
            [PhaseOf("m1", 1, 8, [new PhaseGoal("squat", 140, 5)])]);
        var extra = new Track("extra", "Extra", new TrackRhythm.Weekly(2, 1, false),
            [PhaseOf("e1", 1, 4, [new PhaseGoal("bench", 80, 5)])]);
        var plan = PlanFixture.Make(start: Start, tracks: [main, extra], sessions: []);

        var h = new HistoryBuilder().Done("w1", "2026-01-12", "bench", HistoryBuilder.Set(80, 5));

        Assert.False(PhaseRules.GoalReadiness(PhaseRules.CurrentPhase(plan, main, Start)!, h.Workouts).AllMet);
        var extraReady = PhaseRules.GoalReadiness(PhaseRules.CurrentPhase(plan, extra, Start)!, h.Workouts);
        Assert.True(extraReady.AllMet);
        Assert.Contains("move on", extraReady.Message);
    }
}
