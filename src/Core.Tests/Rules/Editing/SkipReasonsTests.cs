using Core.Rules.Editing;

namespace Core.Tests.Rules.Editing;

public class SkipReasonsTests
{
    [Fact]
    public void The_reasons_offered_are_in_order()
    {
        Assert.Equal(["Short on time", "Pain", "No equipment", "Other"], SkipReasons.All);
    }
}
