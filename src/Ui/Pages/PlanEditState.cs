using Core.Ids;
using Core.Model;
using Core.Rules.PlanEditing;
using Core.Rules.Tracks;

namespace Ui.Pages;

/// <summary>
/// The Plan being edited, shared by the Plan editor's tabs: the last saved Plan, the draft the owner is changing, the selected Track, the
/// Settings fields as typed, the Benchmark flags ticked and any number text that is not a number. Nothing is stored until the page commits
/// <see cref="PlanEditSnapshot.Composed"/> (and the flags); leaving drops the draft. Each tab changes only its own part, and the page
/// asks <see cref="Snapshot"/> whether there is anything wrong and <see cref="IsDirty"/> whether there is anything to save.
/// </summary>
public sealed class PlanEditState
{
    private readonly Dictionary<string, bool> _benchmarks = [];
    private PlanEditSnapshot? _snapshot;
    private Plan? _snapshotDraft;
    private PlanSettingsForm? _snapshotForm;
    private string? _snapshotSelectedTrackId;
    private int _snapshotInputs;

    public PlanEditState(Plan saved)
    {
        Saved = saved;
        Draft = saved;
        SelectedTrackId = TrackRules.Main(saved).Id;
        Form = PlanSettingsForm.From(saved);
    }

    /// <summary>The Plan as last saved (or as opened).</summary>
    public Plan Saved { get; private set; }

    /// <summary>The Plan with the Tracks, Phases and Sessions as edited. Its Settings values are read from <see cref="Form"/>.</summary>
    public Plan Draft { get; private set; }

    /// <summary>The id of the Track currently selected in the Track selector.</summary>
    public string SelectedTrackId { get; private set; }

    /// <summary>The Track currently selected in the Track selector.</summary>
    public Track SelectedTrack => Draft.Tracks.FirstOrDefault(t => t.Id == SelectedTrackId) ?? TrackRules.Main(Draft);

    /// <summary>The zero-based position of <see cref="SelectedTrack"/> in <see cref="Draft"/>'s Track list.</summary>
    public int SelectedTrackIndex
    {
        get
        {
            for (var i = 0; i < Draft.Tracks.Count; i++)
            {
                if (Draft.Tracks[i].Id == SelectedTrack.Id)
                {
                    return i;
                }
            }

            return 0;
        }
    }

    /// <summary>Whether the currently selected Track is the main Track (the first Track in the list).</summary>
    public bool IsMainTrackSelected => SelectedTrackIndex == 0;

    /// <summary>The Settings tab's fields as typed.</summary>
    public PlanSettingsForm Form { get; private set; }

    /// <summary>The Settings tab's fields for the currently selected Track.</summary>
    public TrackSettingsForm SelectedTrackForm => Form.ForTrack(SelectedTrack);

    /// <summary>Row number boxes holding text that is not a number.</summary>
    public RowNumberInputs Inputs { get; } = new();

    /// <summary>Benchmark flags the owner has changed, Exercise id to the flag wanted. They are saved with the Plan, not before.</summary>
    public IReadOnlyDictionary<string, bool> BenchmarkOverrides => _benchmarks;

    /// <summary>Replaces the draft with a changed copy (a tab's edit to Phases or Sessions).</summary>
    public void Change(Func<Plan, Plan> change) => Draft = change(Draft);

    /// <summary>Selects a Track in the Plan editor's Track selector.</summary>
    public void SelectTrack(string trackId)
    {
        if (Draft.Tracks.Any(t => t.Id == trackId))
        {
            SelectedTrackId = trackId;
        }
    }

    /// <summary>Updates the selected Track's name in the Settings form and syncs any valid Track renames into <see cref="Draft"/>.</summary>
    public void SetSelectedTrackName(string name)
    {
        SelectedTrackForm.Name = name;
        if (TrackEditRules.RenameTrack(Draft, SelectedTrack.Id, name).TryGet(out var renamed, out _))
        {
            Draft = renamed;
        }

        foreach (var track in Draft.Tracks)
        {
            var trackName = Form.ForTrack(track).Name;
            if (TrackEditRules.RenameTrack(Draft, track.Id, trackName).TryGet(out var next, out _))
            {
                Draft = next;
            }
        }
    }

    /// <summary>Adds a new Track with default settings, selects it, and populates its Settings form.</summary>
    public PlanEditResult<Plan> AddTrack(string? name, TrackCadence cadence, IIdGenerator ids)
    {
        var result = TrackEditRules.AddTrack(Draft, name, cadence, ids);
        if (result.TryGet(out var updated, out _))
        {
            Draft = updated;
            var added = updated.Tracks[^1];
            Form.ForTrack(added);
            SelectedTrackId = added.Id;
        }

        return result;
    }

    /// <summary>Moves the selected Track one position earlier.</summary>
    public PlanEditResult<Plan> MoveSelectedTrackUp()
    {
        var result = TrackEditRules.MoveUp(Draft, SelectedTrack.Id);
        if (result.TryGet(out var updated, out _))
        {
            Draft = updated;
        }

        return result;
    }

    /// <summary>Moves the selected Track one position later.</summary>
    public PlanEditResult<Plan> MoveSelectedTrackDown()
    {
        var result = TrackEditRules.MoveDown(Draft, SelectedTrack.Id);
        if (result.TryGet(out var updated, out _))
        {
            Draft = updated;
        }

        return result;
    }

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
            if (_snapshot is null
                || !ReferenceEquals(_snapshotDraft, Draft)
                || _snapshotForm != Form
                || _snapshotSelectedTrackId != SelectedTrackId
                || _snapshotInputs != Inputs.Version)
            {
                _snapshot = Read();
                _snapshotDraft = Draft;
                _snapshotForm = Form.Copy();
                _snapshotSelectedTrackId = SelectedTrackId;
                _snapshotInputs = Inputs.Version;
            }

            return _snapshot;
        }
    }

    /// <summary>Something differs from what is saved.</summary>
    public bool IsDirty =>
        Form != PlanSettingsForm.From(Saved)
        || PlanSaveRules.TracksDiffer(Draft.Tracks, Saved.Tracks)
        || !Draft.Sessions.SequenceEqual(Saved.Sessions)
        || _benchmarks.Count > 0
        || Inputs.Any;

    /// <summary>A save went through: the composed Plan is now the saved one and the draft starts over from it.</summary>
    public void MarkSaved(Plan saved)
    {
        Saved = saved;
        Draft = saved;
        if (saved.Tracks.All(t => t.Id != SelectedTrackId))
        {
            SelectedTrackId = TrackRules.Main(saved).Id;
        }

        Form = PlanSettingsForm.From(saved);
        _benchmarks.Clear();
        Inputs.Clear();
    }

    private PlanEditSnapshot Read()
    {
        var selectedTrack = SelectedTrack;
        var settings = Form.ApplyTo(Draft, selectedTrack.Id, out var settingsTrack);

        var selectedPhasesProblem = PlanPhaseRules.ValidatePhases(Draft, selectedTrack.Id, selectedTrack.Phases);
        var anyPhasesProblem = selectedPhasesProblem;
        var phasesProblemTrack = selectedPhasesProblem is not null ? selectedTrack : null;
        if (anyPhasesProblem is null)
        {
            foreach (var track in Draft.Tracks)
            {
                if (PlanPhaseRules.ValidatePhases(Draft, track.Id, track.Phases) is { } reason)
                {
                    anyPhasesProblem = reason;
                    phasesProblemTrack = track;
                    break;
                }
            }
        }

        string? selectedSessionsProblem = null;
        string? anySessionsProblem = null;
        Track? sessionsProblemTrack = null;
        foreach (var session in Draft.Sessions)
        {
            if (session.Exercises.Select(r => RowProblem(session.Id, r)).FirstOrDefault(p => p is not null) is not { } reason)
            {
                continue;
            }

            var message = $"{session.Name}: {reason}";
            if (session.TrackId == selectedTrack.Id && selectedSessionsProblem is null)
            {
                selectedSessionsProblem = message;
            }

            if (anySessionsProblem is null || session.TrackId == selectedTrack.Id && sessionsProblemTrack?.Id != selectedTrack.Id)
            {
                anySessionsProblem = message;
                sessionsProblemTrack = TrackRules.TrackOf(Draft, session) ?? selectedTrack;
            }
        }

        var phasesProblem = selectedPhasesProblem ?? anyPhasesProblem;
        var sessionsProblem = selectedSessionsProblem ?? anySessionsProblem;

        string? problemTab = null;
        if (!settings.TryGet(out var withSettings, out var settingsProblem))
        {
            problemTab = FormatProblemTab("Settings", settingsTrack);
            return new PlanEditSnapshot(settingsProblem, phasesProblem, sessionsProblem, null, Draft, problemTab);
        }

        if (anyPhasesProblem is not null)
        {
            problemTab = FormatProblemTab("Phases", phasesProblemTrack);
        }
        else if (anySessionsProblem is not null)
        {
            problemTab = FormatProblemTab("Sessions", sessionsProblemTrack);
        }

        var preview = Draft with { StartDate = withSettings.StartDate };
        Plan? composed = null;
        if (problemTab is null)
        {
            var current = withSettings;
            var allPhasesValid = true;
            foreach (var track in Draft.Tracks)
            {
                if (!PlanPhaseRules.WithPhases(current, track.Id, track.Phases).TryGet(out var next, out _))
                {
                    allPhasesValid = false;
                    break;
                }

                current = next;
            }

            if (allPhasesValid)
            {
                composed = current;
            }
        }

        return new PlanEditSnapshot(null, phasesProblem, sessionsProblem, composed, preview, problemTab);
    }

    private string FormatProblemTab(string tab, Track? track) =>
        Draft.Tracks.Count > 1 && track is not null && track.Id != SelectedTrack.Id ? $"{tab} ({track.Name})" : tab;
}
