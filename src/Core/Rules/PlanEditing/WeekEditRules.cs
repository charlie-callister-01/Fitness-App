using System.Globalization;
using Core.Ids;
using Core.Model;
using Core.Rules.Rearrangement;
using Core.Rules.Schedule;
using Core.Rules.Tracks;
using Core.Rules.Weeks;
using Core.Storage;

namespace Core.Rules.PlanEditing;

/// <summary>One row of a suggested week: a Session, the day it is on now, and the day the suggestion puts it on.</summary>
public sealed record DraftRow(string SessionId, string SessionName, DateOnly CurrentDate, DateOnly ProposedDate, bool IsUndone)
{
    /// <summary>The row would move the Session (and has not been undone).</summary>
    public bool IsChanged => !IsUndone && ProposedDate != CurrentDate;

    /// <summary>The day the Session ends up on if the draft is applied.</summary>
    public DateOnly FinalDate => IsUndone ? CurrentDate : ProposedDate;
}

/// <summary>
/// A suggested re-arranged week held while the owner decides. It only proposes: nothing is stored until <see cref="Apply"/>'s result
/// is committed, and cancelling is simply dropping the draft.
/// </summary>
public sealed record WeekDraft(DateOnly Monday, IReadOnlyList<DraftRow> Rows, IReadOnlyList<string> Unplaced)
{
    public IReadOnlyList<DraftRow> ChangedRows => [.. Rows.Where(r => r.IsChanged)];

    /// <summary>One line saying what the draft would do, for the top of the draft.</summary>
    public string Summary
    {
        get
        {
            var count = ChangedRows.Count;
            return count == 0
                ? "No Sessions would move."
                : $"{count} {(count == 1 ? "Session" : "Sessions")} would move.";
        }
    }

    /// <summary>The draft with one row put back where the Session is now (its suggestion undone).</summary>
    public WeekDraft Undo(string sessionId) =>
        this with { Rows = [.. Rows.Select(r => r.SessionId == sessionId ? r with { IsUndone = true } : r)] };

    /// <summary>
    /// The Week schedule to save: the week's existing moves, with each changed row's Session moved to its proposed day (a move to the
    /// Session's own default day is no move at all). Undone rows and Sessions the draft does not mention keep what they have.
    /// </summary>
    public WeekSchedule Apply(Plan plan, IReadOnlyList<WeekSchedule> schedules)
    {
        var moves = (schedules.FirstOrDefault(s => s.WeekStart == Monday)?.Moves ?? []).ToList();
        foreach (var row in ChangedRows)
        {
            moves.RemoveAll(m => m.SessionId == row.SessionId);
            var session = plan.Sessions.FirstOrDefault(s => s.Id == row.SessionId);
            if (session is null || row.ProposedDate != WeekCalendar.DefaultDate(Monday, session.DefaultDay))
            {
                moves.Add(new SessionMove(row.SessionId, row.ProposedDate));
            }
        }

        return new WeekSchedule(Monday, moves);
    }
}

/// <summary>What moving a Session changes, to commit together: the Plan (new default day) and/or the week's Week schedule. Null means no change to that record.</summary>
public sealed record SessionMoveChanges(Plan? ChangedPlan, WeekSchedule? ChangedSchedule)
{
    /// <summary>The records to commit in one save.</summary>
    public IReadOnlyList<Change> ToChanges() =>
        [.. new[] { ChangedPlan is { } plan ? Change.Put(plan) : null, ChangedSchedule is { } schedule ? Change.Put(schedule) : null }.OfType<Change>()];
}

/// <summary>The rules behind the Week view: moving a Session, adding a Break, and the suggested week.</summary>
public static class WeekEditRules
{
    /// <summary>
    /// A move of a Session to <paramref name="date"/> for the week starting on <paramref name="monday"/> only. The date must be in that
    /// week. Two Sessions on one day are allowed (the layout rule). Moving back to the default day removes the move. Returns the week's
    /// whole Week schedule to save.
    /// </summary>
    public static PlanEditResult<WeekSchedule> MoveThisWeek(
        Plan plan, IReadOnlyList<WeekSchedule> schedules, string sessionId, DateOnly monday, DateOnly date)
    {
        if (plan.Sessions.FirstOrDefault(s => s.Id == sessionId) is not { } session)
        {
            return new PlanEditResult<WeekSchedule>.Invalid("That Session is no longer in the Plan.");
        }

        if (IsDaily(plan, session))
        {
            return new PlanEditResult<WeekSchedule>.Invalid("A daily Session is on every day and cannot be moved.");
        }

        if (!WeekCalendar.InWeek(monday, date))
        {
            return new PlanEditResult<WeekSchedule>.Invalid("Pick a day in this week.");
        }

        var moves = (schedules.FirstOrDefault(s => s.WeekStart == monday)?.Moves ?? []).Where(m => m.SessionId != sessionId).ToList();
        if (date != WeekCalendar.DefaultDate(monday, session.DefaultDay))
        {
            moves.Add(new SessionMove(sessionId, date));
        }

        return new PlanEditResult<WeekSchedule>.Valid(new WeekSchedule(monday, moves));
    }

    /// <summary>
    /// Moves a Session to <paramref name="date"/> in one save. This week only: the week's Week schedule with the move. Make it the default
    /// day: the Plan with the new default day, plus (when the week already holds a move for that Session, which would still win over the
    /// new default) the week's Week schedule with that move cleared. Commit whatever is not null together.
    /// </summary>
    public static PlanEditResult<SessionMoveChanges> MoveSession(
        Plan plan, IReadOnlyList<WeekSchedule> schedules, string sessionId, DateOnly monday, DateOnly date, bool makeDefaultDay)
    {
        if (!makeDefaultDay)
        {
            return MoveThisWeek(plan, schedules, sessionId, monday, date)
                .Match<PlanEditResult<SessionMoveChanges>>(
                    schedule => new PlanEditResult<SessionMoveChanges>.Valid(new SessionMoveChanges(null, schedule)),
                    message => new PlanEditResult<SessionMoveChanges>.Invalid(message));
        }

        if (!MakeDefaultDay(plan, sessionId, date.DayOfWeek.ToDefaultDay()).TryGet(out var newPlan, out var reason))
        {
            return new PlanEditResult<SessionMoveChanges>.Invalid(reason);
        }

        var hasMove = schedules.FirstOrDefault(s => s.WeekStart == monday)?.Moves.Any(m => m.SessionId == sessionId) == true;
        var cleared = hasMove && MoveThisWeek(newPlan, schedules, sessionId, monday, date).TryGet(out var schedule, out _) ? schedule : null;
        return new PlanEditResult<SessionMoveChanges>.Valid(new SessionMoveChanges(newPlan, cleared));
    }

    /// <summary>The Plan with the Session's default day changed ("Make this the default day"). Refuses daily Sessions.</summary>
    public static PlanEditResult<Plan> MakeDefaultDay(Plan plan, string sessionId, DefaultDay day)
    {
        if (plan.Sessions.FirstOrDefault(s => s.Id == sessionId) is not { } session)
        {
            return new PlanEditResult<Plan>.Invalid("That Session is no longer in the Plan.");
        }

        if (IsDaily(plan, session))
        {
            return new PlanEditResult<Plan>.Invalid("A daily Session is on every day and its default day cannot be changed.");
        }

        if (day == DefaultDay.Daily)
        {
            return new PlanEditResult<Plan>.Invalid("A weekly Track Session must be on a day of the week, not daily.");
        }

        return new PlanEditResult<Plan>.Valid(plan with { Sessions = [.. plan.Sessions.Select(s => s.Id == sessionId ? s with { DefaultDay = day } : s)] });
    }

    /// <summary>
    /// A new Break for the dates, both included. An end before the start is refused, and so is any overlap with a Break that exists
    /// (the message names its dates). The reason is trimmed and may be empty.
    /// </summary>
    public static PlanEditResult<Break> CreateBreak(
        DateOnly start, DateOnly end, string? reason, IReadOnlyList<Break> existing, IIdGenerator ids)
    {
        if (end < start)
        {
            return new PlanEditResult<Break>.Invalid("A Break cannot end before it starts.");
        }

        if (existing.FirstOrDefault(b => start <= b.EndDate && end >= b.StartDate) is { } clash)
        {
            return new PlanEditResult<Break>.Invalid(
                $"That overlaps your Break from {Text(clash.StartDate)} to {Text(clash.EndDate)}.");
        }

        return new PlanEditResult<Break>.Valid(new Break(ids.NewId(), start, end, reason?.Trim() ?? ""));
    }

    /// <summary>
    /// The suggested week as a draft (null when the week is already workable), from the existing re-arrangement rule. Every Session in
    /// the suggestion becomes a row, so the owner sees what moves and what stays.
    /// </summary>
    public static WeekDraft? SuggestDraft(
        Plan plan, IReadOnlyList<Exercise> exercises, IReadOnlyList<WeekSchedule> schedules,
        IReadOnlyList<Break> breaks, IReadOnlyList<Workout> workouts, DateOnly today)
    {
        if (WeekRearrangement.Suggest(plan, TrackRules.Main(plan), exercises, schedules, breaks, workouts, today) is not RearrangementResult.Proposal proposal)
        {
            return null;
        }

        var layout = WeekScheduleRules.Layout(plan, TrackRules.Main(plan), schedules, breaks, today);
        var rows = proposal.Placements
            .Select(p => (Placement: p, Now: layout.Sessions.FirstOrDefault(s => s.SessionId == p.SessionId)))
            .Where(x => x.Now is not null)
            .Select(x => new DraftRow(x.Placement.SessionId, x.Now!.Session.Name, x.Now.Date, x.Placement.Date, false))
            .ToList();
        return new WeekDraft(proposal.Monday, rows, proposal.Unplaced);
    }

    private static bool IsDaily(Plan plan, Session session) =>
        TrackRules.TrackOf(plan, session)?.Rhythm is TrackRhythm.Daily || session.DefaultDay == DefaultDay.Daily;

    private static string Text(DateOnly date) => date.ToString("d MMM yyyy", CultureInfo.InvariantCulture);
}
