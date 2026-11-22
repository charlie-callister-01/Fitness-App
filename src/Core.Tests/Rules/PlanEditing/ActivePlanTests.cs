using Core.Model;
using Core.Rules.PlanEditing;
using Core.Tests.Rules.TestData;

namespace Core.Tests.Rules.PlanEditing;

public class ActivePlanTests
{
    private static Plan P(string id, bool active) => PlanFixture.Make() with { Id = id, IsActive = active };

    [Fact]
    public void The_active_Plan_is_the_one_marked_active() => Assert.Equal("b", ActivePlan.Of([P("a", false), P("b", true)])!.Id);

    [Fact]
    public void With_no_active_Plan_there_is_none() => Assert.Null(ActivePlan.Of([P("a", false)]));

    [Fact]
    public void With_no_Plans_there_is_none() => Assert.Null(ActivePlan.Of([]));
}
