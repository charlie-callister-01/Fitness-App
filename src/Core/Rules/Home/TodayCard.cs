using Core.Model;

namespace Core.Rules.Home;

/// <summary>The state of Home's TodayCard, deciding what action or status is shown.</summary>
public enum TodayKind
{
    NoPlan,
    Break,
    InProgress,
    DoneToday,
    PlanFinished,
    NotStarted,
    Start,
    RestDay,
}

/// <summary>Summary metrics for a Workout completed today.</summary>
public sealed record TodayDoneSummary(
    string WorkoutId,
    TimeSpan? Duration,
    int SetCount,
    int PrCount,
    bool IsPartial);

/// <summary>The state and data for Home's TodayCard.</summary>
public sealed record TodayCard(
    TodayKind Kind,
    Session? Session = null,
    Workout? InProgressWorkout = null,
    int? DoneCount = null,
    int? PlannedCount = null,
    TimeSpan? Elapsed = null,
    TodayDoneSummary? DoneSummary = null,
    Session? NextSession = null,
    DateOnly? NextDate = null,
    DateOnly? PlanStartDate = null)
{
    public static TodayCard NoPlan() => new(TodayKind.NoPlan);
    public static TodayCard Break() => new(TodayKind.Break);
    public static TodayCard InProgress(Workout workout, int doneCount, int plannedCount, TimeSpan? elapsed) =>
        new(TodayKind.InProgress, InProgressWorkout: workout, DoneCount: doneCount, PlannedCount: plannedCount, Elapsed: elapsed);
    public static TodayCard DoneToday(TodayDoneSummary summary) =>
        new(TodayKind.DoneToday, DoneSummary: summary);
    public static TodayCard PlanFinished() => new(TodayKind.PlanFinished);
    public static TodayCard NotStarted(DateOnly planStartDate) =>
        new(TodayKind.NotStarted, PlanStartDate: planStartDate);
    public static TodayCard Start(Session session) =>
        new(TodayKind.Start, Session: session);
    public static TodayCard RestDay(Session? nextSession = null, DateOnly? nextDate = null) =>
        new(TodayKind.RestDay, NextSession: nextSession, NextDate: nextDate);
}
