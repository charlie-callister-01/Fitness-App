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
    public static PlanEditResult<Plan> FromScratch(string? name, DateOnly startDate, IIdGenerator ids) =>
        string.IsNullOrWhiteSpace(name)
            ? new PlanEditResult<Plan>.Invalid(NameNeeded)
            : new PlanEditResult<Plan>.Valid(
                new Plan(ids.NewId(), name.Trim(), false, startDate, null, null, 3, 2, false, 2.5, [], []));

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
        var newSessionIds = plan.Sessions.ToDictionary(s => s.Id, _ => ids.NewId());
        var sessions = plan.Sessions.Select(s => s with { Id = newSessionIds[s.Id] }).ToList();
        var phases = plan.Phases.Select(p => p with
        {
            Id = ids.NewId(),
            SessionOverrides = [.. p.SessionOverrides.Select(o => o with { SessionId = newSessionIds.GetValueOrDefault(o.SessionId, o.SessionId) })],
        }).ToList();
        return plan with { Id = ids.NewId(), Name = name.Trim(), IsActive = false, Phases = phases, Sessions = sessions };
    }
}
