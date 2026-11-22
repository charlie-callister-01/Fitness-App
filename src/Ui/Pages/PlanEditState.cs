using Core.Model;
using Core.Rules.PlanEditing;

namespace Ui.Pages;

/// <summary>
/// The Plan being edited, shared by the Plan editor's tabs: the last saved Plan, the draft the owner is changing, the Settings fields as
/// typed, the Benchmark flags ticked and any number text that is not a number. Nothing is stored until the page commits
/// <see cref="PlanEditSnapshot.Composed"/> (and the flags); leaving drops the draft. Each tab changes only its own part, and the page
/// asks <see cref="Snapshot"/> whether there is anything wrong and <see cref="IsDirty"/> whether there is anything to save.
/// </summary>
public sealed class PlanEditState
{
    private readonly Dictionary<string, bool> _benchmarks = [];
    private PlanEditSnapshot? _snapshot;
    private Plan? _snapshotDraft;
    private PlanSettingsForm? _snapshotForm;
    private int _snapshotInputs;

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

    /// <summary>Row number boxes holding text that is not a number.</summary>
    public RowNumberInputs Inputs { get; } = new();

    /// <summary>Benchmark flags the owner has changed, Exercise id to the flag wanted. They are saved with the Plan, not before.</summary>
    public IReadOnlyDictionary<string, bool> BenchmarkOverrides => _benchmarks;

    /// <summary>Replaces the draft with a changed copy (a tab's edit to Phases or Sessions).</summary>
    public void Change(Func<Plan, Plan> change) => Draft = change(Draft);

    /// <summary>Whether the Exercise shows as a Benchmark exercise: the owner's tick if there is one, else what the library says.</summary>
    public bool IsBenchmark(string exerciseId, bool libraryValue) => _benchmarks.GetValueOrDefault(exerciseId, libraryValue);

    /// <summary>Ticks or unticks the Benchmark flag. Setting it back to what the library holds drops the override.</summary>
    public void SetBenchmark(string exerciseId, bool wanted, bool libraryValue)
    {
        if (wanted == libraryValue)
        {
            _benchmarks.Remove(exerciseId);
        }
        else
        {
            _benchmarks[exerciseId] = wanted;
        }
    }

    /// <summary>The reason this row cannot be saved (text that is not a number, or its numbers), or null.</summary>
    public string? RowProblem(string sessionId, SessionExercise row) =>
        Inputs.HasUnreadable(sessionId, row.ExerciseId) ? RowNumberInputs.Message : SessionRowRules.ValidateRow(row);

    /// <summary>The editor read once; the same reading is returned until the draft, the Settings fields or the typed numbers change.</summary>
    public PlanEditSnapshot Snapshot
    {
        get
        {
            if (_snapshot is null || !ReferenceEquals(_snapshotDraft, Draft) || _snapshotForm != Form || _snapshotInputs != Inputs.Version)
            {
                _snapshot = Read();
                _snapshotDraft = Draft;
                _snapshotForm = Form with { };
                _snapshotInputs = Inputs.Version;
            }

            return _snapshot;
        }
    }

    /// <summary>Something differs from what is saved.</summary>
    public bool IsDirty =>
        Form != PlanSettingsForm.From(Saved)
        || !Draft.Phases.SequenceEqual(Saved.Phases)
        || !Draft.Sessions.SequenceEqual(Saved.Sessions)
        || _benchmarks.Count > 0
        || Inputs.Any;

    /// <summary>A save went through: the composed Plan is now the saved one and the draft starts over from it.</summary>
    public void MarkSaved(Plan saved)
    {
        Saved = saved;
        Draft = saved;
        Form = PlanSettingsForm.From(saved);
        _benchmarks.Clear();
        Inputs.Clear();
    }

    private PlanEditSnapshot Read()
    {
        var settings = Form.Read(Draft);
        var phasesProblem = PlanPhaseRules.ValidatePhases(Draft.Phases);
        var sessionsProblem = Draft.Sessions
            .Select(s => s.Exercises.Select(r => RowProblem(s.Id, r)).FirstOrDefault(p => p is not null) is { } reason ? $"{s.Name}: {reason}" : null)
            .FirstOrDefault(p => p is not null);

        if (!settings.TryGet(out var read, out var settingsProblem))
        {
            return new PlanEditSnapshot(settingsProblem, phasesProblem, sessionsProblem, null, Draft);
        }

        var preview = Draft with { StartDate = read.StartDate };
        Plan? composed = null;
        if (sessionsProblem is null
            && PlanSettingsRules.WithSettings(Draft, read).TryGet(out var withSettings, out _)
            && PlanPhaseRules.WithPhases(withSettings, Draft.Phases).TryGet(out var withPhases, out _))
        {
            composed = withPhases;
        }

        return new PlanEditSnapshot(null, phasesProblem, sessionsProblem, composed, preview);
    }
}
