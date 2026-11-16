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

    [Fact]
    public void Consumer_reads_today_from_an_injected_fixed_clock()
    {
        var clock = new FixedClock(new DateTimeOffset(2027, 4, 24, 9, 30, 0, TimeSpan.Zero));

        var service = new DayLabeller(clock);

        Assert.Equal("2027-04-24", service.Today());
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    // Demonstrates the injection style later stages use: rules take IClock, never DateTime.UtcNow.
    private sealed class DayLabeller(IClock clock)
    {
        public string Today() => clock.UtcNow.ToString("yyyy-MM-dd");
    }
}
