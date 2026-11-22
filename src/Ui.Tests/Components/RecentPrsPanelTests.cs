using Bunit;
using Core.Model;
using Core.Rules.Home;
using Core.Rules.Performance;
using Core.Rules.Units;
using Ui.Components;

namespace Ui.Tests.Components;

public sealed class RecentPrsPanelTests : BunitContext
{
    private static readonly Exercise Squat = new("squat", "Squat", LogKind.Load, null, false);

    private static WorkoutSet Set(double kg, int reps) => new("s", kg, reps, null, null, null, false);

    private static RecentPr Pr(WorkoutSet set, WorkoutSet? previous) => new(
        new DateOnly(2026, 11, 15), "squat", "Squat", set,
        previous is null ? null : new BestResult(previous, "w0", new DateOnly(2026, 11, 1), false));

    private IRenderedComponent<RecentPrsPanel> Show(IReadOnlyList<RecentPr> prs, Settings? settings = null) =>
        Render<RecentPrsPanel>(p => p.Add(c => c.Prs, prs).Add(c => c.Exercises, [Squat])
            .Add(c => c.Converter, new UnitConverter(settings ?? Settings.Default)));

    [Fact]
    public void A_row_shows_name_new_best_previous_best_and_date()
    {
        var cut = Show([Pr(Set(22.5, 8), Set(20, 9))]);

        var row = cut.Find(".fa-prpanel__row").TextContent;
        Assert.Contains("Squat", row);
        Assert.Contains("22.5", row);
        Assert.Contains("was", row);
        Assert.Contains("20", row);
        Assert.Contains("Sun 15 Nov", row);
    }

    [Fact]
    public void A_first_ever_PR_has_no_previous_best()
    {
        var cut = Show([Pr(Set(22.5, 8), null)]);

        Assert.DoesNotContain("was", cut.Find(".fa-prpanel__row").TextContent);
    }

    [Fact]
    public void Empty_list_shows_the_quiet_line()
    {
        var cut = Show([]);

        Assert.Contains("No PRs yet. Your first one will show here.", cut.Markup);
        Assert.Empty(cut.FindAll(".fa-prpanel__row"));
    }

    [Fact]
    public void Pounds_are_used_when_the_display_unit_is_lb()
    {
        var kg = Show([Pr(Set(100, 5), null)]).Find(".fa-prpanel__row").TextContent;
        var lb = Show([Pr(Set(100, 5), null)], Settings.Default with { DisplayWeightUnit = WeightUnit.Lb }).Find(".fa-prpanel__row").TextContent;

        Assert.Contains("kg", kg);
        Assert.Contains("lb", lb);
        Assert.Contains("220.5", lb);
    }
}
