using Core.Ids;
using Core.Model;

namespace Core.Rules.PlanEditing;

/// <summary>The rules for creating Plans and choosing the active one.</summary>
public static class PlanLifecycleRules
{
    private const string NameNeeded = "Give the Plan a name.";

    /// <summary>
    /// A copy of the Plan under <paramref name="name"/>: same content, inactive, with fresh ids for the Plan, its Sessions and its Phases
    /// (a Phase's overrides follow their Sessions' new ids). Exercises are shared, so their ids stay. A blank name is refused.
    /// </summary>
    public static PlanEditResult<Plan> Copy(Plan plan, string? name, IIdGenerator ids) =>
        string.IsNullOrWhiteSpace(name)
            ? new PlanEditResult<Plan>.Invalid(NameNeeded)
            : new PlanEditResult<Plan>.Valid(Duplicate(plan, name, ids));

    /// <summary>
    /// A new, inactive Plan with no Phases and no Sessions, starting on <paramref name="startDate"/> with the app's defaults. A blank name
    /// is refused.
    /// </summary>
    public static PlanEditResult<Plan> FromScratch(string? name, DateOnly startDate, IIdGenerator ids)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return new PlanEditResult<Plan>.Invalid(NameNeeded);
        }

        var planId = ids.NewId();
        var mainTrack = new Track($"{planId}-main", "Main", new TrackRhythm.Weekly(3, 2, false), []);
        return new PlanEditResult<Plan>.Valid(
            new Plan(planId, name.Trim(), false, startDate, null, null, 2.5, [mainTrack], []));
    }

    /// <summary>
    /// The Plans that change when <paramref name="planId"/> becomes the active one: that Plan and the one that was active, so one commit
    /// leaves exactly one active Plan. Empty when it already is the active one or does not exist.
    /// </summary>
    public static IReadOnlyList<Plan> MakeActive(IReadOnlyList<Plan> plans, string planId)
    {
        if (plans.FirstOrDefault(p => p.Id == planId) is not { IsActive: false } target)
        {
            return [];
        }

        return [.. plans.Where(p => p.IsActive && p.Id != planId).Select(p => p with { IsActive = false }), target with { IsActive = true }];
    }

    internal static Plan Duplicate(Plan plan, string name, IIdGenerator ids)
    {
        var newTrackIds = plan.Tracks.ToDictionary(t => t.Id, _ => ids.NewId());
        var newSessionIds = plan.Sessions.ToDictionary(s => s.Id, _ => ids.NewId());
        var sessions = plan.Sessions
            .Select(s => s with { Id = newSessionIds[s.Id], TrackId = newTrackIds.GetValueOrDefault(s.TrackId, s.TrackId) })
            .ToList();
        var tracks = plan.Tracks.Select(t => t with
        {
            Id = newTrackIds[t.Id],
            Phases =
            [
                .. t.Phases.Select(p => p with
                {
                    Id = ids.NewId(),
                    SessionOverrides = [.. p.SessionOverrides.Select(o => o with { SessionId = newSessionIds.GetValueOrDefault(o.SessionId, o.SessionId) })],
                }),
            ],
        }).ToList();
        return plan with { Id = ids.NewId(), Name = name.Trim(), IsActive = false, Tracks = tracks, Sessions = sessions };
    }
}
