using Core.Ids;
using Core.Model;

namespace Core.Rules.PlanEditing;

// Kept for Ui; the Ui fix pass moves callers to PlanSettingsRules, PlanPhaseRules, SessionRowRules and PlanLifecycleRules, then deletes this class.
public static class PlanEditRules
{
    public static PlanEditResult<Plan> WithSettings(Plan plan, PlanSettings settings) => PlanSettingsRules.WithSettings(plan, settings);

    public static (DateOnly Start, DateOnly End) PhaseDates(Plan plan, Phase phase) => PlanPhaseRules.PhaseDates(plan, phase);

    public static string? ValidatePhases(IReadOnlyList<Phase> phases) => PlanPhaseRules.ValidatePhases(phases);

    public static PlanEditResult<Plan> WithPhases(Plan plan, IReadOnlyList<Phase> phases) => PlanPhaseRules.WithPhases(plan, phases);

    public static string? ValidateRow(SessionExercise row) => SessionRowRules.ValidateRow(row);

    public static string? ValidateRows(IEnumerable<SessionExercise> rows) => SessionRowRules.ValidateRows(rows);

    public static IReadOnlyList<SessionExercise> MoveUp(IReadOnlyList<SessionExercise> rows, int index) => SessionRowRules.MoveUp(rows, index);

    public static IReadOnlyList<SessionExercise> MoveDown(IReadOnlyList<SessionExercise> rows, int index) => SessionRowRules.MoveDown(rows, index);

    public static IReadOnlyList<SessionExercise> Remove(IReadOnlyList<SessionExercise> rows, int index) => SessionRowRules.Remove(rows, index);

    public static IReadOnlyList<SessionExercise> AddFromLibrary(IReadOnlyList<SessionExercise> rows, string exerciseId) =>
        SessionRowRules.AddFromLibrary(rows, exerciseId);

    public static Plan WithRows(Plan plan, string sessionId, IReadOnlyList<SessionExercise> rows) => SessionRowRules.WithRows(plan, sessionId, rows);

    public static PlanEditResult<Plan> EditSession(Plan plan, string sessionId, string? name, DayOfWeek day, SessionType type) =>
        SessionRowRules.EditSession(plan, sessionId, name, day, type);

    public static PlanEditResult<Plan> AddSession(Plan plan, string? name, DayOfWeek day, SessionType type, IIdGenerator ids) =>
        SessionRowRules.AddSession(plan, name, day, type, ids);

    /// <summary>The old shape of <see cref="PlanLifecycleRules.Copy"/>: no blank-name check, returns the Plan.</summary>
    public static Plan Copy(Plan plan, string name, IIdGenerator ids) => PlanLifecycleRules.Duplicate(plan, name, ids);

    public static PlanEditResult<Plan> FromScratch(string? name, DateOnly startDate, IIdGenerator ids) => PlanLifecycleRules.FromScratch(name, startDate, ids);

    public static IReadOnlyList<Plan> MakeActive(IReadOnlyList<Plan> plans, string planId) => PlanLifecycleRules.MakeActive(plans, planId);
}
