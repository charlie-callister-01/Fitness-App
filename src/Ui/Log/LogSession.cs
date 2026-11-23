using Core.Model;
using Core.Storage;
using Core.Time;
using Ui.Preferences;
using Ui.Saving;

namespace Ui.Log;

/// <summary>
/// What lives for as long as the log page is open: the rest countdown, the screen lock and the subscriptions that redraw the page and
/// stop the rest. Created once with the page and disposed once, which unsubscribes everything and releases the lock.
/// </summary>
public sealed class LogSession : IDisposable
{
    private readonly AppData _data;
    private readonly IWakeLock _wakeLock;
    private readonly RestTimerSetting _restSetting;
    private readonly FinishFlow _finish;
    private readonly LoggedAfterwardsFlow _afterwards;
    private readonly Announcer _announcer;
    private readonly Action _redraw;
    private bool _screenLocked;

    /// <param name="redraw">Asks the page to draw again; may be called from any thread.</param>
    public LogSession(
        AppData data, IClock clock, ISecondTicker ticker, IWakeLock wakeLock, RestTimerSetting restSetting,
        FinishFlow finish, LoggedAfterwardsFlow afterwards, Announcer announcer, Action redraw)
    {
        _data = data;
        _wakeLock = wakeLock;
        _restSetting = restSetting;
        _finish = finish;
        _afterwards = afterwards;
        _announcer = announcer;
        _redraw = redraw;
        Rest = new RestCountdown(clock, ticker);

        data.Changed += redraw;
        Rest.Changed += redraw;
        Rest.Ended += OnRestEnded;
        finish.Changed += redraw;
        finish.Finished += Rest.Stop;
        afterwards.Changed += redraw;
        afterwards.Redated += Rest.Stop;
        restSetting.Changed += OnRestSettingChanged;
    }

    public RestCountdown Rest { get; }

    /// <summary>Starts the rest after a confirmed Set: the chosen length, and none for a Workout logged afterwards.</summary>
    public void StartRest(bool loggedAfterwards) => Rest.Start(loggedAfterwards ? 0 : _restSetting.Seconds);

    /// <summary>
    /// The screen stays on while a Workout with timers is open (not one logged afterwards), and is released the moment it finishes
    /// or is gone. Call after each render with the open Workout, or null.
    /// </summary>
    public async Task SyncScreenLockAsync(Workout? open)
    {
        var wantsLock = open is { LoggedAfterwards: false };
        if (!_screenLocked && wantsLock)
        {
            _screenLocked = true;
            await _wakeLock.AcquireAsync();
        }
        else if (_screenLocked && !wantsLock)
        {
            _screenLocked = false;
            await _wakeLock.ReleaseAsync();
        }
    }

    public void Dispose()
    {
        _data.Changed -= _redraw;
        Rest.Changed -= _redraw;
        Rest.Ended -= OnRestEnded;
        _finish.Changed -= _redraw;
        _finish.Finished -= Rest.Stop;
        _afterwards.Changed -= _redraw;
        _afterwards.Redated -= Rest.Stop;
        _restSetting.Changed -= OnRestSettingChanged;
        Rest.Dispose();
        if (_screenLocked)
        {
            _screenLocked = false;
            _ = _wakeLock.ReleaseAsync();
        }
    }

    // Announced once, politely, when the countdown reaches zero; the readout itself stays aria-live="off".
    private void OnRestEnded() => _announcer.Announce("Rest over");

    private void OnRestSettingChanged()
    {
        if (_restSetting.Seconds == 0)
        {
            Rest.Stop();
        }
    }
}
