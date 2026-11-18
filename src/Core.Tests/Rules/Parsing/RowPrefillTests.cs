using Core.Model;
using Core.Rules.Parsing;
using Core.Rules.Performance;
using Core.Rules.Units;
using Core.Tests.Rules.TestData;
using static Core.Tests.Rules.TestData.HistoryBuilder;

namespace Core.Tests.Rules.Parsing;

public class RowPrefillTests
{
    private static readonly UnitConverter Metric = new(WeightUnit.Kg, DistanceUnit.Km, 2);
    private static readonly UnitConverter Imperial = new(WeightUnit.Lb, DistanceUnit.Mi, 2);

    private static LastResult LastOf(params WorkoutSet[] sets) => new("w1", Dates.D(2026, 1, 5), sets, null, null);

    private static ExerciseSnapshot Snap(LastResult? last = null, SuggestedTarget? target = null) =>
        new("ex", last, null, target);

    private static SuggestedTarget Target(double? kg = null, int? reps = null, int? seconds = null) =>
        new(kg, reps, null, seconds, null, "why");

    private static SessionExercise Plan(int? reps = 8, int? repsMax = null) => new("ex", 3, reps, repsMax, null);

    [Fact]
    public void Uses_the_same_numbered_set_of_the_last_workout()
    {
        var snap = Snap(LastOf(Set(kg: 20, reps: 10), Set(kg: 22.5, reps: 8)), Target(30, 5));

        var row = RowPrefill.For(LogKind.Load, 2, snap, Plan(), Metric);

        Assert.Equal(new RowPrefillValues("22.5", "8", null, PrefillSource.LastWorkout), row);
    }

    [Fact]
    public void Falls_back_to_the_target_when_last_workout_had_fewer_sets()
    {
        var snap = Snap(LastOf(Set(kg: 20, reps: 10)), Target(22, 8));

        var row = RowPrefill.For(LogKind.Load, 3, snap, Plan(), Metric);

        Assert.Equal(new RowPrefillValues("22", "8", null, PrefillSource.SuggestedTarget), row);
    }

    [Fact]
    public void Target_alone_fills_every_set()
    {
        var row = RowPrefill.For(LogKind.Load, 1, Snap(target: Target(22, 8)), Plan(), Metric);

        Assert.Equal(PrefillSource.SuggestedTarget, row.Source);
        Assert.Equal("22", row.First);
    }

    [Fact]
    public void No_history_is_empty_with_planned_reps_as_hint()
    {
        var row = RowPrefill.For(LogKind.Load, 1, null, Plan(8), Metric);

        Assert.Equal(new RowPrefillValues("", "", "8", PrefillSource.None), row);
    }

    [Fact]
    public void Planned_range_is_shown_as_a_range()
    {
        var row = RowPrefill.For(LogKind.Load, 1, Snap(), Plan(8, 12), Metric);

        Assert.Equal("8-12", row.RepsHint);
    }

    [Fact]
    public void Null_plan_gives_no_hint()
    {
        var row = RowPrefill.For(LogKind.Load, 1, null, null, Metric);

        Assert.Equal(RowPrefillValues.Empty(), row);
    }

    [Fact]
    public void Plan_row_without_reps_gives_no_hint()
    {
        Assert.Null(RowPrefill.For(LogKind.Reps, 1, null, Plan(null), Metric).RepsHint);
    }

    [Fact]
    public void Weights_are_shown_in_display_units()
    {
        var snap = Snap(LastOf(Set(kg: 45.359237, reps: 5)));

        var row = RowPrefill.For(LogKind.Load, 1, snap, null, Imperial);

        Assert.Equal("100", row.First);
    }

    [Fact]
    public void Bodyweight_set_shows_bw_and_added_weight_shows_the_weight()
    {
        var snap = Snap(LastOf(Set(reps: 12), Set(kg: 10, reps: 8)));

        Assert.Equal(("bw", "12"), Pair(RowPrefill.For(LogKind.BodyweightLoad, 1, snap, null, Metric)));
        Assert.Equal(("10", "8"), Pair(RowPrefill.For(LogKind.BodyweightLoad, 2, snap, null, Metric)));
    }

    [Fact]
    public void Carry_shows_weight_and_metres()
    {
        var snap = Snap(LastOf(Set(kg: 32, m: 40)));

        Assert.Equal(("32", "40"), Pair(RowPrefill.For(LogKind.Carry, 1, snap, null, Metric)));
    }

    [Theory]
    [InlineData(LogKind.Reps, "12")]
    [InlineData(LogKind.Seconds, "45")]
    [InlineData(LogKind.Minutes, "0.75")]
    [InlineData(LogKind.Time, "0:45")]
    public void Single_field_kinds_from_a_set(LogKind kind, string expected)
    {
        var snap = Snap(LastOf(Set(reps: 12, seconds: 45)));

        Assert.Equal(expected, RowPrefill.For(kind, 1, snap, null, Metric).First);
    }

    [Theory]
    [InlineData(108, "1:48")]
    [InlineData(3850, "1:04:10")]
    public void Time_is_shown_as_a_clock(int seconds, string expected)
    {
        var snap = Snap(LastOf(Set(seconds: seconds)));

        Assert.Equal(expected, RowPrefill.For(LogKind.Time, 1, snap, null, Metric).First);
    }

    [Fact]
    public void Distances_use_display_units()
    {
        var snap = Snap(LastOf(Set(km: 1.609344, m: 400)));

        Assert.Equal("1", RowPrefill.For(LogKind.DistanceKm, 1, snap, null, Imperial).First);
        Assert.Equal("400", RowPrefill.For(LogKind.DistanceM, 1, snap, null, Metric).First);
    }

    [Fact]
    public void Prefilled_text_parses_back_to_the_same_set()
    {
        var parser = new InputParser(Metric);
        var snap = Snap(LastOf(Set(kg: 22.5, reps: 8)));

        var row = RowPrefill.For(LogKind.Load, 1, snap, null, Metric);
        var parsed = Assert.IsType<ParseResult.Success>(parser.Parse(RowInput.Join(LogKind.Load, row.First, row.Second), LogKind.Load));

        Assert.Equal(22.5, parsed.Set.WeightKg);
        Assert.Equal(8, parsed.Set.Reps);
    }

    [Fact]
    public void Tick_only_and_text_have_nothing_to_prefill()
    {
        var snap = Snap(LastOf(Set(reps: 5)), Target(reps: 5));

        Assert.Equal(RowPrefillValues.Empty(), RowPrefill.For(LogKind.TickOnly, 1, snap, null, Metric));
        Assert.Equal(RowPrefillValues.Empty(), RowPrefill.For(LogKind.Text, 1, snap, null, Metric));
    }

    [Fact]
    public void Last_set_missing_a_needed_field_falls_through_to_the_target()
    {
        var snap = Snap(LastOf(Set(reps: 8)), Target(22, 8));

        var row = RowPrefill.For(LogKind.Load, 1, snap, null, Metric);

        Assert.Equal(PrefillSource.SuggestedTarget, row.Source);
    }

    [Fact]
    public void FromStored_shows_a_saved_set_in_display_units()
    {
        var stored = Set(kg: 45.3592, reps: 8);

        Assert.Equal(("100", "8"), Pair(RowPrefill.FromStored(LogKind.Load, stored, Imperial)!));
        Assert.Equal(("bw", "8"), Pair(RowPrefill.FromStored(LogKind.BodyweightLoad, Set(reps: 8), Metric)!));
        Assert.Null(RowPrefill.FromStored(LogKind.Load, Set(reps: 8), Metric));
    }

    private static (string, string) Pair(RowPrefillValues v) => (v.First, v.Second);
}
