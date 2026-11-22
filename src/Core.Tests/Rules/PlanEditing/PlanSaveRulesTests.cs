using Core.Model;
using Core.Rules.PlanEditing;
using Core.Storage;
using Core.Tests.Rules.TestData;
using static Core.Tests.Rules.TestData.Dates;

namespace Core.Tests.Rules.PlanEditing;

public class PlanSaveRulesTests
{
    private static Exercise Ex(string id, bool benchmark = false) =>
        new(id, id, LogKind.Load, new ExerciseTags([], [], []), benchmark);

    private static readonly IReadOnlyList<Exercise> Library = [Ex("squat"), Ex("bench", benchmark: true)];

    private static readonly Plan Saved = PlanFixture.Make(start: Mon);

    private static IEnumerable<(RecordKind Kind, string Key)> Keys(IReadOnlyList<Change> changes) => changes.Select(c => (c.Kind, c.Key));

    [Fact]
    public void A_changed_Plan_and_each_flag_that_really_changes_are_put_together()
    {
        var composed = Saved with { Name = "Renamed" };

        var changes = PlanSaveRules.Changes(composed, Saved, Library, new Dictionary<string, bool> { ["squat"] = true, ["bench"] = false });

        Assert.Equal([(RecordKind.Plan, Saved.Id), (RecordKind.Exercise, "squat"), (RecordKind.Exercise, "bench")], Keys(changes));
    }

    [Fact]
    public void Overrides_that_change_nothing_or_name_unknown_Exercises_are_ignored()
    {
        var changes = PlanSaveRules.Changes(
            Saved with { Name = "Renamed" }, Saved, Library, new Dictionary<string, bool> { ["squat"] = false, ["bench"] = true, ["ghost"] = true });

        Assert.Equal([(RecordKind.Plan, Saved.Id)], Keys(changes));
    }

    [Fact]
    public void An_unchanged_Plan_is_not_put_again_but_a_flag_change_still_is()
    {
        var changes = PlanSaveRules.Changes(Saved with { }, Saved, Library, new Dictionary<string, bool> { ["squat"] = true });

        Assert.Equal([(RecordKind.Exercise, "squat")], Keys(changes));
    }

    [Fact]
    public void Nothing_changed_means_nothing_to_commit()
    {
        Assert.Empty(PlanSaveRules.Changes(Saved, Saved, Library, new Dictionary<string, bool>()));
    }
}
