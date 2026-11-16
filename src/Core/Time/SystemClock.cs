namespace Core.Time;

/// <summary>The real clock, used by the app hosts.</summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
