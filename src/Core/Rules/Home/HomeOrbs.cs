using Core.Model;
using Core.Rules.Phases;
using Core.Rules.Weeks;
using Core.Storage;

namespace Core.Rules.Home;

public sealed record StreakOrb(int Current, int ThisWeekCount, int Minimum, bool OnBreak)
{
    public double Fraction => Minimum == 0 ? 0.0 : Math.Min((double)ThisWeekCount / Minimum, 1.0);
    public double RingFraction => Fraction;
}

public sealed record PhaseOrb(int WeekInPhase, int PhaseLengthWeeks)
{
    public double Fraction => PhaseLengthWeeks <= 0 ? 0.0 : Math.Clamp((double)WeekInPhase / PhaseLengthWeeks, 0.0, 1.0);
}

public sealed record RaceOrb(int DaysToGo, double ElapsedShare)
{
    public double Fraction => ElapsedShare;
}

public sealed record HomeOrbs(StreakOrb StreakOrb, PhaseOrb? PhaseOrb, RaceOrb? RaceOrb)
{
    public StreakOrb Streak => StreakOrb;
    public PhaseOrb? Phase => PhaseOrb;
    public RaceOrb? Race => RaceOrb;

    public static HomeOrbs? Compute(DataSnapshot snapshot, DateOnly today)
    {
        var plan = snapshot.Plans.FirstOrDefault(p => p.IsActive);
        if (plan is null) return null;

        var monday = WeekCalendar.MondayOf(today);
        var streakResult = WeekRules.Streak(plan, snapshot.Workouts, snapshot.Breaks, today);
        var thisWeekCount = WeekRules.CountedWorkouts(plan, snapshot.Workouts, monday);
        var onBreak = BreakCalendar.IsBreakDay(snapshot.Breaks, today);
        var streakOrb = new StreakOrb(streakResult.Current, thisWeekCount, plan.StreakMinWorkoutsPerWeek, onBreak);

        var phasePosition = PhaseRules.Position(plan, today);
        PhaseOrb? phaseOrb = phasePosition is { State: PhaseState.InPhase, WeekInPhase: { } weekInPhase, PhaseLengthWeeks: { } phaseLength }
            ? new PhaseOrb(weekInPhase, phaseLength)
            : null;

        RaceOrb? raceOrb = null;
        if (plan.GoalDate is { } goalDate)
        {
            var daysToGo = Math.Max(0, goalDate.DayNumber - today.DayNumber);
            var totalSpan = goalDate.DayNumber - plan.StartDate.DayNumber;
            var elapsed = today.DayNumber - plan.StartDate.DayNumber;
            var elapsedShare = totalSpan <= 0 ? 0.0 : Math.Clamp((double)elapsed / totalSpan, 0.0, 1.0);
            raceOrb = new RaceOrb(daysToGo, elapsedShare);
        }

        return new HomeOrbs(streakOrb, phaseOrb, raceOrb);
    }
}
