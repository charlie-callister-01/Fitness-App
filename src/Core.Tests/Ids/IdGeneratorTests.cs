using Core.Ids;

namespace Core.Tests.Ids;

public class IdGeneratorTests
{
    [Fact]
    public void Guid_generator_makes_distinct_canonical_lowercase_ids()
    {
        IIdGenerator generator = new GuidIdGenerator();

        var a = generator.NewId();
        var b = generator.NewId();

        Assert.NotEqual(a, b);
        Assert.Equal(a.ToLowerInvariant(), a);
        Assert.True(Guid.TryParseExact(a, "D", out _));
    }

    [Fact]
    public void Sequential_test_double_is_deterministic()
    {
        IIdGenerator generator = new SequentialIdGenerator("ex");

        Assert.Equal(["ex-1", "ex-2", "ex-3"], [generator.NewId(), generator.NewId(), generator.NewId()]);
    }
}
