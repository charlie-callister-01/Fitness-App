using Core.Ids;
using Core.Model;
using Core.Rules.PlanEditing;
using Core.Tests.Ids;
using Core.Tests.Rules.TestData;
using static Core.Tests.Rules.TestData.Dates;

namespace Core.Tests.Rules.PlanEditing;

public class PlanEditResultTests
{
    [Fact]
    public void A_valid_result_gives_its_value_and_no_message()
    {
        PlanEditResult<int> result = new PlanEditResult<int>.Valid(7);

        Assert.True(result.IsValid);
        Assert.Null(result.Message);
        Assert.True(result.TryGet(out var value, out var message));
        Assert.Equal(7, value);
        Assert.Equal("", message);
    }

    [Fact]
    public void An_invalid_result_gives_its_message_and_no_value()
    {
        PlanEditResult<int> result = new PlanEditResult<int>.Invalid("Nope.");

        Assert.False(result.IsValid);
        Assert.Equal("Nope.", result.Message);
        Assert.False(result.TryGet(out _, out var message));
        Assert.Equal("Nope.", message);
    }

    [Fact]
    public void Match_picks_the_branch_for_the_result()
    {
        PlanEditResult<int> valid = new PlanEditResult<int>.Valid(7);
        PlanEditResult<int> invalid = new PlanEditResult<int>.Invalid("Nope.");

        Assert.Equal("7", valid.Match(v => v.ToString(), m => m));
        Assert.Equal("Nope.", invalid.Match(v => v.ToString(), m => m));
    }

    [Fact]
    public void Copying_a_Plan_with_a_blank_name_is_refused_like_a_new_Plan()
    {
        var plan = PlanFixture.Make(start: Mon);

        var copy = PlanLifecycleRules.Copy(plan, "  ", new SequentialIdGenerator("n"));
        var scratch = PlanLifecycleRules.FromScratch("  ", Mon, new SequentialIdGenerator("n"));

        Assert.Equal(scratch.Message, copy.Message);
        Assert.False(copy.IsValid);
    }

    [Fact]
    public void Copying_a_Plan_gives_an_inactive_copy_under_the_trimmed_name()
    {
        var plan = PlanFixture.Make(start: Mon);

        Assert.True(PlanLifecycleRules.Copy(plan, " Variation ", new SequentialIdGenerator("n")).TryGet(out var copy, out _));

        Assert.Equal("Variation", copy.Name);
        Assert.False(copy.IsActive);
        Assert.NotEqual(plan.Id, copy.Id);
    }
}
