using System.Globalization;
using Core.Ids;
using Core.Model;
using Core.Rules.Home;
using Core.Rules.Starting;
using Core.Rules.Units;
using Core.Storage;
using Core.Time;
using Microsoft.AspNetCore.Components;
using Ui.Preferences;
using Ui.Saving;

namespace Ui.Pages;

public sealed partial class Home : IDisposable
{
    private HomeView _view = default!;
    private IReadOnlySet<string> _dismissed = new HashSet<string>();
    private PageContext Page = default!;
    private GuardedSave _save = default!;
    private bool _differentOpen;
    private bool _pastOpen;
    private bool _weightOpen;
    private string? _pastMessage;
    private string? _weightMessage;

    [Inject] private AppData Data { get; set; } = default!;

    [Inject] private IClock Clock { get; set; } = default!;

    [Inject] private IIdGenerator Ids { get; set; } = default!;

    [Inject] private SaveFailureNotice Notice { get; set; } = default!;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Inject] private IDevicePreferences Preferences { get; set; } = default!;

    private NudgeDismissals Dismissals => new(Preferences);

    private UnitConverter Converter => new(Data.Snapshot.Settings);

    private string DateText => _view.Header.PhaseLabel.Length == 0
        ? _view.Header.Date.ToString("ddd d MMM", CultureInfo.InvariantCulture)
        : $"{_view.Header.Date.ToString("ddd d MMM", CultureInfo.InvariantCulture)} · {_view.Header.PhaseLabel}";

    private IReadOnlyList<Session> UndoneSessions => [.. _view.UndoneSessions.Select(s => s.Session)];

    private IReadOnlyList<Session> PlanSessions => Page.ActivePlan?.Sessions ?? [];

    private string LatestWeightText =>
        _view.Weight.Latest is { } latest ? Converter.DisplayWeight(latest.WeightKg).ToString("0.0", CultureInfo.InvariantCulture) : "";

    private string LastWeightText =>
        _view.Weight.Latest is { } latest
            ? $"Last: {LatestWeightText} {Converter.WeightLabel} on {latest.Date.ToString("ddd d MMM", CultureInfo.InvariantCulture)}"
            : "";

    private static string DailyStreakText(int days) => $"{days} {(days == 1 ? "day" : "days")}";

    private static string ExtraLineSummary(ExtraWeekLine line)
    {
        var parts = new List<string>
        {
            $"{line.TrackName}: {line.Done} of {line.Sessions.Count}",
            $"target {line.Target}",
            $"Streak {line.Streak} {(line.Streak == 1 ? "week" : "weeks")}",
        };
        if (line.MissedSessionIds.Count > 0)
        {
            var missedNames = line.Sessions
                .Where(s => line.MissedSessionIds.Contains(s.Session.Id))
                .Select(s => HomeWeekRules.SessionCode(s.Session));
            parts.Add($"Missed: {string.Join(", ", missedNames)}");
        }

        return string.Join(" · ", parts);
    }

    private static string ExtraStateWords(TileState state) => state switch
    {
        TileState.Done => "done",
        TileState.Missed => "missed",
        TileState.Partial => "partial",
        TileState.BreakDay => "Break day",
        TileState.Upcoming => "upcoming",
        _ => "rest day",
    };

    private static string ExtraSessionLabel(string trackName, ExtraWeekSession item)
    {
        var day = item.Date.DayOfWeek.ToString();
        var lead = item.IsToday ? $"Today, {day}" : day;
        return $"{trackName}: {item.Session.Name}, {lead}, {ExtraStateWords(item.State)}";
    }

    protected override async Task OnInitializedAsync()
    {
        Page = new PageContext(Data, Clock);
        _save = new GuardedSave(Data, Notice);
        Data.Changed += OnDataChanged;
        Rebuild();
        await RefreshAsync();
    }

    public void Dispose() => Data.Changed -= OnDataChanged;

    private void OnDataChanged() => _ = InvokeAsync(RefreshAsync);

    private void Rebuild() =>
        _view = HomeViewBuilder.Build(Data.Snapshot, Clock.UtcNow, Page.Today, Page.LocalNow.Hour, _dismissed, Data.LastBackupAt);

    private async Task RefreshAsync()
    {
        Rebuild();
        _dismissed = await Dismissals.LoadAsync(_view.NudgeKeys);
        Rebuild();
        StateHasChanged();
    }

    private void OnContinue(Workout workout) => Navigation.NavigateTo($"log/{workout.Id}");

    private void OnOpenSummary()
    {
        if (_view.Today.DoneSummary is { } done)
        {
            Navigation.NavigateTo($"log/{done.WorkoutId}");
        }
    }

    private void OnOpenDay(DateOnly day) => Navigation.NavigateTo("week");

    private void OnOpenProgress() => Navigation.NavigateTo("progress");

    private void OnOpenCalendar() => Navigation.NavigateTo("progress?tab=calendar");

    private void OnOpenReview() => Navigation.NavigateTo("weekly-review");

    /// <summary>Sunday, and today's Weigh-in is logged: the Today card leads on to the Weekly review (F-07).</summary>
    private bool ReviewReady => Page.Today.DayOfWeek == DayOfWeek.Sunday && _view.Weight.Latest?.Date == Page.Today;

    /// <summary>Starts a Workout under the guarded save and opens the log when there is a Workout to open (null when the save was refused).</summary>
    private async Task<StartOutcome?> StartAndOpenAsync(Func<Task<StartOutcome>> start)
    {
        StartOutcome? outcome = null;
        var result = await _save.RunAsync(async () =>
        {
            outcome = await start();
            return outcome.Commit is { } commit ? SaveWork.Committed(commit) : SaveWork.None;
        });
        if (result.Status is SaveStatus.Busy or SaveStatus.ReadOnly)
        {
            return null;
        }

        if (outcome?.Workout is { } workout)
        {
            Navigation.NavigateTo($"log/{workout.Id}");
        }

        return outcome;
    }

    private async Task StartAsync(Session session) => await StartSessionAsync(session);

    private async Task<bool> StartSessionAsync(Session session)
    {
        if (Page.ActivePlan is not { } plan)
        {
            return false;
        }

        var outcome = await StartAndOpenAsync(() => new WorkoutStarter(Data, Ids, Clock).StartAsync(plan, session, Page.Today));
        return outcome?.Workout is not null;
    }

    // Do a different Session.
    private Task OnDifferentSessionAsync()
    {
        _differentOpen = true;
        return Task.CompletedTask;
    }

    private void CloseDifferent() => _differentOpen = false;

    private async Task PickDifferentAsync(Session session)
    {
        if (await StartSessionAsync(session))
        {
            _differentOpen = false;
        }
    }

    private void BuildCustomFromSheet()
    {
        _differentOpen = false;
        OnBuildCustom();
    }

    private void OnBuildCustom() => Navigation.NavigateTo("custom");

    // Log a past Workout.
    private void OnLogPastWorkout()
    {
        _pastMessage = null;
        _pastOpen = true;
    }

    private void ClosePast() => _pastOpen = false;

    private async Task StartPastAsync((DateOnly Date, Session? Session) choice)
    {
        var outcome = await StartAndOpenAsync(() =>
            new WorkoutStarter(Data, Ids, Clock).StartLoggedAfterwardsAsync(Page.ActivePlan, choice.Session, choice.Date, Page.Today));
        if (outcome is { Workout: null, Commit: null })
        {
            _pastMessage = "That day hasn't happened yet.";
        }
        else if (outcome?.Workout is not null)
        {
            _pastOpen = false;
        }
    }

    // Nudges.
    private async Task OnNudgeAction(HomeNudge item)
    {
        switch (item.Action)
        {
            case NudgeAction.MakeUp when item.Nudge.SessionId is { } sessionId:
                Navigation.NavigateTo($"custom/makeup/{sessionId}");
                break;
            case NudgeAction.DifferentSession:
                await OnDifferentSessionAsync();
                break;
            case NudgeAction.BackUp:
                Navigation.NavigateTo("backup");
                break;
        }
    }

    private async Task OnNudgeDismiss(HomeNudge item)
    {
        await Dismissals.DismissAsync(item.Key);
        await RefreshAsync();
    }

    // Log weight.
    private void OnLogWeight()
    {
        _weightMessage = null;
        _weightOpen = true;
    }

    private void CloseWeight() => _weightOpen = false;

    private async Task<bool> SaveWeightAsync(string text)
    {
        switch (WeighInRules.Create(text, Converter, Page.Today, Ids))
        {
            case WeighInResult.Invalid invalid:
                _weightMessage = invalid.Message;
                return false;
            case WeighInResult.Valid valid:
                _weightMessage = null;
                var result = await _save.CommitAsync(Change.Put(valid.WeighIn));
                _weightOpen = _weightOpen && !result.IsSaved;
                return result.IsSaved;
            default:
                return false;
        }
    }
}
