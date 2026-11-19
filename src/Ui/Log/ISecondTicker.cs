namespace Ui.Log;

/// <summary>Waits for the next second. Only paces how often the rest readout redraws; what it says always comes from the clock.</summary>
public interface ISecondTicker
{
    Task WaitNextAsync(CancellationToken cancellation);
}

/// <summary>The real thing: a one second delay.</summary>
public sealed class SystemSecondTicker : ISecondTicker
{
    public Task WaitNextAsync(CancellationToken cancellation) => Task.Delay(TimeSpan.FromSeconds(1), cancellation);
}
