using Core.Model;

namespace Core.Rules.PlanEditing;

/// <summary>The one rule for which Plan is the active one. Only one Plan is active at a time.</summary>
public static class ActivePlan
{
    /// <summary>The active Plan, or null when there is none.</summary>
    public static Plan? Of(IEnumerable<Plan> plans) => plans.FirstOrDefault(p => p.IsActive);
}
