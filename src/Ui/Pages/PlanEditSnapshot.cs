using Core.Model;

namespace Ui.Pages;

/// <summary>
/// One reading of the Plan editor, taken once per change: what is wrong on each tab, the Plan as it would be saved, and the Plan as the
/// owner sees it now (for dates). <see cref="Composed"/> is null exactly when <see cref="ProblemTab"/> is not.
/// </summary>
public sealed record PlanEditSnapshot(
    string? SettingsProblem, string? PhasesProblem, string? SessionsProblem, Plan? Composed, Plan Preview)
{
    /// <summary>The tab to open to fix the first problem, or null when there is none.</summary>
    public string? ProblemTab =>
        SettingsProblem is not null ? "Settings" : PhasesProblem is not null ? "Phases" : SessionsProblem is not null ? "Sessions" : null;

    public bool IsValid => ProblemTab is null;
}
