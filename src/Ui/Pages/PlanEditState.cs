using Core.Model;
using Core.Rules.PlanEditing;

namespace Ui.Pages;

/// <summary>
/// The Plan being edited, shared by the Plan editor's tabs: the last saved Plan, the draft the owner is changing, and the Settings
/// fields as typed. Nothing is stored until the page commits <see cref="Compose"/>; leaving drops the draft. Each tab changes only its
/// own part (Settings through <see cref="Form"/>, Phases and Sessions through <see cref="Change"/>), and the page asks here whether
/// there is anything to save.
/// </summary>
public sealed class PlanEditState
{
    public PlanEditState(Plan saved)
    {
        Saved = saved;
        Draft = saved;
        Form = PlanSettingsForm.From(saved);
    }

    /// <summary>The Plan as last saved (or as opened).</summary>
    public Plan Saved { get; private set; }

    /// <summary>The Plan with the Phases and Sessions as edited. Its Settings values are stale: read them from <see cref="Form"/>.</summary>
    public Plan Draft { get; private set; }

    /// <summary>The Settings tab's fields as typed.</summary>
    public PlanSettingsForm Form { get; private set; }

    /// <summary>Replaces the draft with a changed copy (a tab's edit to Phases or Sessions).</summary>
    public void Change(Func<Plan, Plan> change) => Draft = change(Draft);

    /// <summary>The reason the Settings cannot be saved, or null.</summary>
    public string? SettingsProblem => Form.Read(Draft) is PlanEditResult<PlanSettings>.Invalid invalid ? invalid.Message : null;

    /// <summary>The reason the Phases cannot be saved, or null.</summary>
    public string? PhasesProblem => PlanEditRules.ValidatePhases(Draft.Phases);

    /// <summary>The tab to open to fix the first problem, or null when there is none.</summary>
    public string? ProblemTab => SettingsProblem is not null ? "Settings" : PhasesProblem is not null ? "Phases" : SessionsProblem is not null ? "Sessions" : null;

    /// <summary>The reason the Sessions cannot be saved (a row with bad Sets or Reps, named by its Session), or null.</summary>
    public string? SessionsProblem => Draft.Sessions
        .Select(s => PlanEditRules.ValidateRows(s.Exercises) is { } reason ? $"{s.Name}: {reason}" : null)
        .FirstOrDefault(r => r is not null);

    public bool IsValid => ProblemTab is null;

    /// <summary>Something differs from what is saved.</summary>
    public bool IsDirty =>
        Form != PlanSettingsForm.From(Saved)
        || !Draft.Phases.SequenceEqual(Saved.Phases)
        || !Draft.Sessions.SequenceEqual(Saved.Sessions);

    /// <summary>
    /// The Plan to save: the draft with the Settings read from the form and the Phases in week order. Null while there is a problem.
    /// </summary>
    public Plan? Compose()
    {
        if (SessionsProblem is not null
            || Form.Read(Draft) is not PlanEditResult<PlanSettings>.Valid settings
            || PlanEditRules.WithSettings(Draft, settings.Value) is not PlanEditResult<Plan>.Valid withSettings
            || PlanEditRules.WithPhases(withSettings.Value, Draft.Phases) is not PlanEditResult<Plan>.Valid composed)
        {
            return null;
        }

        return composed.Value;
    }

    /// <summary>
    /// The Plan as the owner sees it now, for showing dates: the draft with the start date from the form when that is readable. Unlike
    /// <see cref="Compose"/> it works while there are problems.
    /// </summary>
    public Plan Preview => Form.Read(Draft) is PlanEditResult<PlanSettings>.Valid settings ? Draft with { StartDate = settings.Value.StartDate } : Draft;

    /// <summary>A save went through: the composed Plan is now the saved one and the draft starts over from it.</summary>
    public void MarkSaved(Plan saved)
    {
        Saved = saved;
        Draft = saved;
        Form = PlanSettingsForm.From(saved);
    }
}
