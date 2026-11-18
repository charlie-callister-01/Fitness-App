using Core.Exchange;

namespace Core.Tests.Exchange;

/// <summary>A sink that records what it was handed and can be told to fail. <see cref="OnDelivering"/> runs at delivery time, so a test can look at the store then.</summary>
internal sealed class RecordingBackupSink : IBackupSink
{
    public List<BackupFile> Delivered { get; } = [];

    public bool Succeeds { get; set; } = true;

    public Action? OnDelivering { get; set; }

    public Task<bool> DeliverAsync(BackupFile file)
    {
        OnDelivering?.Invoke();
        if (Succeeds)
        {
            Delivered.Add(file);
        }

        return Task.FromResult(Succeeds);
    }
}
