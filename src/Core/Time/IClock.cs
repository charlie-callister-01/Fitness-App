namespace Core.Time;

/// <summary>Source of the current time. Rules take this instead of reading the system clock, so they stay testable.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
