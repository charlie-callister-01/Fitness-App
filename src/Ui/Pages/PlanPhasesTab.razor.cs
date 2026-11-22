using System.Globalization;
using Core.Ids;
using Core.Model;
using Core.Rules.Parsing;
using Core.Rules.PlanEditing;
using Core.Rules.Tracks;
using Core.Storage;
using Microsoft.AspNetCore.Components;

namespace Ui.Pages;

public partial class PlanPhasesTab
{
    private string? _expandedId;

    [Inject]
    private AppData Data { get; set; } = default!;

    [Inject]
    private IIdGenerator Ids { get; set; } = default!;

    [Parameter, EditorRequired]
    public PlanEditState State { get; set; } = default!;

    /// <summary>The whole editor cannot be changed (read-only window or a save running).</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Something in the tab changed, so the page can re-check Save.</summary>
    [Parameter]
    public EventCallback OnEdited { get; set; }

    private IReadOnlyList<Phase> SelectedPhases => State.SelectedTrack.Phases;

    private IReadOnlyList<Session> TrackSessions => TrackRules.SessionsOf(State.Draft, State.SelectedTrack);

    private static string NameOf(Phase phase) => string.IsNullOrWhiteSpace(phase.Focus) ? "Unnamed Phase" : phase.Focus;

    private string WeeksText(Phase phase)
    {
        var weeks = phase.StartWeek == phase.EndWeek ? $"Week {phase.StartWeek}" : $"Weeks {phase.StartWeek} to {phase.EndWeek}";
        if (phase.StartWeek < 1 || phase.EndWeek < phase.StartWeek)
        {
            return weeks;
        }

        var (start, end) = PlanPhaseRules.PhaseDates(State.Snapshot.Preview, phase);
        return $"{weeks} · {start.ToString("d MMM", CultureInfo.InvariantCulture)} to {end.ToString("d MMM yyyy", CultureInfo.InvariantCulture)}";
    }

    private void Toggle(string id) => _expandedId = _expandedId == id ? null : id;

    private Plan WithSelectedPhases(Plan plan, Func<IReadOnlyList<Phase>, IReadOnlyList<Phase>> change)
    {
        var trackId = State.SelectedTrack.Id;
        return plan with
        {
            Tracks = [.. plan.Tracks.Select(t => t.Id == trackId ? t with { Phases = change(t.Phases) } : t)],
        };
    }

    private Task ChangePhase(string id, Func<Phase, Phase> change)
    {
        State.Change(plan => WithSelectedPhases(plan, phases => [.. phases.Select(p => p.Id == id ? change(p) : p)]));
        return OnEdited.InvokeAsync();
    }

    private Task ChangeGoal(string id, int index, Func<PhaseGoal, PhaseGoal> change) =>
        ChangePhase(id, p => p with { Goals = [.. p.Goals.Select((g, i) => i == index ? change(g) : g)] });

    private Task ChangeOverride(string id, int index, Func<SessionOverride, SessionOverride> change) =>
        ChangePhase(id, p => p with { SessionOverrides = [.. p.SessionOverrides.Select((o, i) => i == index ? change(o) : o)] });

    private Task SetFocus(string id, ChangeEventArgs e) => ChangePhase(id, p => p with { Focus = e.Value?.ToString() ?? "" });

    private Task SetWeek(string id, ChangeEventArgs e, bool first) =>
        int.TryParse(e.Value?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var week)
            ? ChangePhase(id, p => first ? p with { StartWeek = week } : p with { EndWeek = week })
            : Task.CompletedTask;

    private Task SetGoalExercise(string id, int index, ChangeEventArgs e) =>
        ChangeGoal(id, index, g => g with { ExerciseId = e.Value?.ToString() ?? "" });

    private Task SetGoalWeight(string id, int index, ChangeEventArgs e) =>
        DecimalText.TryParse(e.Value?.ToString(), out var kg)
            ? ChangeGoal(id, index, g => g with { WeightKg = kg })
            : Task.CompletedTask;

    private Task SetGoalReps(string id, int index, ChangeEventArgs e) =>
        int.TryParse(e.Value?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var reps)
            ? ChangeGoal(id, index, g => g with { Reps = reps })
            : Task.CompletedTask;

    private Task AddGoal(string id)
    {
        var library = Data.Snapshot.Exercises;
        var first = library.FirstOrDefault(e => e.LogKind == LogKind.Load) ?? library.FirstOrDefault();
        return first is null
            ? Task.CompletedTask
            : ChangePhase(id, p => p with { Goals = [.. p.Goals, new PhaseGoal(first.Id, 0, 1)] });
    }

    private Task RemoveGoal(string id, int index) =>
        ChangePhase(id, p => p with { Goals = [.. p.Goals.Where((_, i) => i != index)] });

    private Task AddOverride(string id)
    {
        var first = TrackSessions.FirstOrDefault();
        return first is null
            ? Task.CompletedTask
            : ChangePhase(id, p => p with { SessionOverrides = [.. p.SessionOverrides, new SessionOverride(first.Id, first.Exercises)] });
    }

    private Task SetOverrideSession(string id, int index, ChangeEventArgs e)
    {
        var sessionId = e.Value?.ToString() ?? "";
        var exercises = TrackSessions.FirstOrDefault(s => s.Id == sessionId)?.Exercises ?? [];
        return ChangeOverride(id, index, o => o with { SessionId = sessionId, Exercises = o.SessionId == sessionId ? o.Exercises : exercises });
    }

    private Task RemoveOverride(string id, int index) =>
        ChangePhase(id, p => p with { SessionOverrides = [.. p.SessionOverrides.Where((_, i) => i != index)] });

    private Task AddPhase()
    {
        var start = SelectedPhases.Count == 0 ? 1 : SelectedPhases.Max(p => p.EndWeek) + 1;
        var phase = new Phase(Ids.NewId(), "New Phase", start, start + 3, [], []);
        State.Change(plan => WithSelectedPhases(plan, phases => [.. phases, phase]));
        _expandedId = phase.Id;
        return OnEdited.InvokeAsync();
    }

    private Task RemovePhase(string id)
    {
        State.Change(plan => WithSelectedPhases(plan, phases => [.. phases.Where(p => p.Id != id)]));
        _expandedId = null;
        return OnEdited.InvokeAsync();
    }
}
