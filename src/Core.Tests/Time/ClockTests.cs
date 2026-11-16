using Core.Time;

namespace Core.Tests.Time;

public class ClockTests
{
    [Fact]
    public void SystemClock_returns_a_time_close_to_now()
    {
        IClock clock = new SystemClock();

        var before = DateTimeOffset.UtcNow;
        var reading = clock.UtcNow;
        var after = DateTimeOffset.UtcNow;

        Assert.InRange(reading, before, after);
    }
}
