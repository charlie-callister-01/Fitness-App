using Core.Model;
using Core.Rules.Performance;
using Core.Rules.Units;
using Ui.Components;

namespace Ui.Tests.Components;

public sealed class SetTextTests
{
    private static readonly UnitConverter Metric = new(WeightUnit.Kg, DistanceUnit.Km, 2);
    private static readonly UnitConverter Imperial = new(WeightUnit.Lb, DistanceUnit.Mi, 5);

    private static WorkoutSet Of(
        double? kg = null, int? reps = null, double? km = null, int? seconds = null, double? m = null, double? cm = null) =>
        new("s", kg, reps, km, seconds, m, false, cm);

    [Theory]
    [InlineData(LogKind.Reps, 12, "12 reps")]
    [InlineData(LogKind.Seconds, 45, "45 s")]
    [InlineData(LogKind.Minutes, 750, "12.5 min")]
    [InlineData(LogKind.Minutes, 720, "12 min")]
    [InlineData(LogKind.Time, 108, "1:48")]
    [InlineData(LogKind.Time, 3850, "1:04:10")]
    public void Counts_and_durations_read_in_the_kinds_own_words(LogKind kind, int value, string expected)
    {
        var set = kind == LogKind.Reps ? Of(reps: value) : Of(seconds: value);

        Assert.Equal(expected, SetText.Result(kind, set, Metric, WeightUnit.Kg));
    }

    [Fact]
    public void Distances_read_in_metres_or_the_owners_distance_unit()
    {
        Assert.Equal("500 m", SetText.Result(LogKind.DistanceM, Of(m: 500), Metric, WeightUnit.Kg));
        Assert.Equal("5 km", SetText.Result(LogKind.DistanceKm, Of(km: 5), Metric, WeightUnit.Kg));
        Assert.Equal("3.11 mi", SetText.Result(LogKind.DistanceKm, Of(km: 5), Imperial, WeightUnit.Lb, DistanceUnit.Mi));
    }

    [Fact]
    public void A_carry_reads_weight_by_metres()
    {
        Assert.Equal("24 kg × 40 m", SetText.Result(LogKind.Carry, Of(kg: 24, m: 40), Metric, WeightUnit.Kg));
    }

    [Fact]
    public void Body_measurements_and_ticks_read_plainly()
    {
        Assert.Equal("82.5 kg", SetText.Result(LogKind.BodyMeasurementKg, Of(kg: 82.5), Metric, WeightUnit.Kg));
        Assert.Equal("92 cm", SetText.Result(LogKind.BodyMeasurementCm, Of(cm: 92), Metric, WeightUnit.Kg));
        Assert.Equal("Done", SetText.Result(LogKind.TickOnly, Of(), Metric, WeightUnit.Kg));
    }

    [Fact]
    public void A_suggested_target_reads_like_a_result_for_a_seconds_kind()
    {
        var target = new SuggestedTarget(null, null, null, 50, null, "reason");

        Assert.Equal("50 s", SetText.Target(LogKind.Seconds, target, Metric, WeightUnit.Kg));
    }
}
