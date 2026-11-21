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
    private bool _saving;
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

    private DateTimeOffset LocalNow => Clock.UtcNow.ToLocalTime();

    private DateOnly Today => DateOnly.FromDateTime(LocalNow.DateTime);

    private bool CanWrite => Data.Health.CanWrite;

    private UnitConverter Converter => new(Data.Snapshot.Settings);

    private Plan? ActivePlan => Data.Snapshot.Plans.FirstOrDefault(p => p.IsActive);

    private string DateText => _view.Header.PhaseLabel.Length == 0
        ? _view.Header.Date.ToString("ddd d MMM", CultureInfo.InvariantCulture)
        : $"{_view.Header.Date.ToString("ddd d MMM", CultureInfo.InvariantCulture)} · {_view.Header.PhaseLabel}";

    private IReadOnlyList<Session> UndoneSessions => [.. _view.UndoneSessions.Select(s => s.Session)];

    private IReadOnlyList<Session> PlanSessions => ActivePlan?.Sessions ?? [];

    private string? ReadOnlyMessage => ReadOnlyWording.For(Data, "Home");

    private string LatestWeightText =>
        _view.Weight.Latest is { } latest ? Converter.DisplayWeight(latest.WeightKg).ToString("0.0", CultureInfo.InvariantCulture) : "";

    private string LastWeightText =>
        _view.Weight.Latest is { } latest
            ? $"Last: {LatestWeightText} {Converter.WeightLabel} on {latest.Date.ToString("ddd d MMM", CultureInfo.InvariantCulture)}"
            : "";

    protected override async Task OnInitializedAsync()
    {
        Data.Changed += OnDataChanged;
        Rebuild();
        await RefreshAsync();
    }

    public void Dispose() => Data.Changed -= OnDataChanged;

    private void OnDataChanged() => _ = InvokeAsync(RefreshAsync);

    private void Rebuild() =>
        _view = HomeViewBuilder.Build(Data.Snapshot, Clock.UtcNow, Today, LocalNow.Hour, _dismissed, Data.LastBackupAt);

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

    /// <summary>Runs a save with the page marked busy. A second tap while one is running does nothing (returns null).</summary>
    private async Task<T?> WhileSavingAsync<T>(Func<Task<T>> work)
        where T : class
    {
        if (_saving)
        {
            return null;
        }

        _saving = true;
        try
        {
            return await work();
        }
        finally
        {
            _saving = false;
        }
    }

    /// <summary>Starts a Workout, reports a failed save, and opens the log when there is a Workout to open (null when busy).</summary>
    private Task<StartOutcome?> StartAndOpenAsync(Func<Task<StartOutcome>> start) =>
        WhileSavingAsync(async () =>
        {
            var outcome = await start();
            if (outcome.Commit is { } commit)
            {
                Notice.Report(commit);
            }

            if (outcome.Workout is { } workout)
            {
                Navigation.NavigateTo($"log/{workout.Id}");
            }

            return outcome;
        });

    private async Task StartAsync(Session session) => await StartSessionAsync(session);

    private async Task<bool> StartSessionAsync(Session session)
    {
        if (ActivePlan is not { } plan)
        {
            return false;
        }

        var outcome = await StartAndOpenAsync(() => new WorkoutStarter(Data, Ids, Clock).StartAsync(plan, session, Today));
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
            new WorkoutStarter(Data, Ids, Clock).StartLoggedAfterwardsAsync(ActivePlan, choice.Session, choice.Date, Today));
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
        switch (WeighInRules.Create(text, Converter, Today, Ids))
        {
            case WeighInResult.Invalid invalid:
                _weightMessage = invalid.Message;
                return false;
            case WeighInResult.Valid valid:
                _weightMessage = null;
                var result = await WhileSavingAsync(async () =>
                {
                    var committed = await Data.CommitAsync(Change.Put(valid.WeighIn));
                    Notice.Report(committed);
                    return committed;
                });
                _weightOpen = _weightOpen && result is not CommitResult.Committed;
                return result is CommitResult.Committed;
            default:
                return false;
        }
    }
}
