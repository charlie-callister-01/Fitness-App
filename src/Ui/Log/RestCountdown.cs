using Core.Time;

namespace Ui.Log;

/// <summary>
/// The rest countdown shown in the ConfirmBar. It remembers when it ends and reads <see cref="IClock"/>, so the number is right however
/// late a redraw comes (a phone that slept); the ticker only says when to redraw. Reaching zero clears it quietly.
/// </summary>
public sealed class RestCountdown(IClock clock, ISecondTicker ticker) : IDisposable
{
    private CancellationTokenSource? _running;
    private DateTimeOffset _endsAt;

    /// <summary>Raised when the readout changes; may come from any thread.</summary>
    public event Action? Changed;

    /// <summary>"Rest 1:12" while counting down, else null.</summary>
    public string? Label
    {
        get
        {
            if (_running is null)
            {
                return null;
            }

            var left = _endsAt - clock.UtcNow;
            if (left <= TimeSpan.Zero)
            {
                return null;
            }

            var seconds = (int)Math.Ceiling(left.TotalSeconds);
            return $"Rest {seconds / 60}:{seconds % 60:00}";
        }
    }

    /// <summary>Starts, or restarts, a countdown of this many seconds; 0 or less stops it.</summary>
    public void Start(int seconds)
    {
        Stop();
        if (seconds <= 0)
        {
            return;
        }

        _endsAt = clock.UtcNow.AddSeconds(seconds);
        _running = new CancellationTokenSource();
        _ = RunAsync(_running.Token);
        Changed?.Invoke();
    }

    public void Stop()
    {
        if (_running is null)
        {
            return;
        }

        _running.Cancel();
        _running.Dispose();
        _running = null;
        Changed?.Invoke();
    }

    public void Dispose()
    {
        _running?.Cancel();
        _running?.Dispose();
        _running = null;
    }

    private async Task RunAsync(CancellationToken cancellation)
    {
        try
        {
            while (clock.UtcNow < _endsAt)
            {
                await ticker.WaitNextAsync(cancellation);
                Changed?.Invoke();
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (!cancellation.IsCancellationRequested)
        {
            Stop();
        }
    }
}
