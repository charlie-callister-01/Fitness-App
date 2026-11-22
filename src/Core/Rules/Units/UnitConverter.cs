using Core.Model;

namespace Core.Rules.Units;

/// <summary>A weight shown in both units, each rounded for display.</summary>
public readonly record struct BothWeights(double Kg, double Lb);

/// <summary>A distance shown in both units, each rounded for display.</summary>
public readonly record struct BothDistances(double Km, double Mi);

/// <summary>
/// Converts between canonical units (kg, km) and the owner's display units, and applies display rounding.
/// Pure arithmetic on doubles, so culture never affects results. Stored values are never rounded here:
/// only the Display* methods round, and the *FromDisplay methods return exact canonical values.
/// </summary>
public sealed class UnitConverter
{
    private const double KgPerLb = 0.45359237;
    private const double KmPerMile = 1.609344;
    private const double KgDisplayStep = 0.25;
    private const double LbDisplayStep = 0.5;
    private const int DistanceDecimals = 2;

    private readonly WeightUnit _weightUnit;
    private readonly DistanceUnit _distanceUnit;

    public UnitConverter(WeightUnit weightUnit, DistanceUnit distanceUnit, double weightIncrementKg)
    {
        _weightUnit = weightUnit;
        _distanceUnit = distanceUnit;
        WeightIncrementKg = weightIncrementKg;
    }

    public UnitConverter(Settings settings)
        : this(settings.DisplayWeightUnit, settings.DisplayDistanceUnit, settings.WeightIncrementKg)
    {
    }

    /// <summary>The weight step for targets. Always kg, whatever the display unit.</summary>
    public double WeightIncrementKg { get; }

    /// <summary>The display weight unit as a word: "kg" or "lb".</summary>
    public string WeightLabel => _weightUnit == WeightUnit.Kg ? "kg" : "lb";

    /// <summary>The display distance unit as a word: "km" or "mi".</summary>
    public string DistanceLabel => _distanceUnit == DistanceUnit.Km ? "km" : "mi";

    /// <summary>A canonical weight (kg) in the display unit, rounded for display.</summary>
    public double DisplayWeight(double kg) =>
        _weightUnit == WeightUnit.Kg ? RoundWeightKg(kg) : RoundWeightLb(kg / KgPerLb);

    /// <summary>A canonical distance (km) in the display unit, rounded to two decimals.</summary>
    public double DisplayDistance(double km) =>
        RoundDistance(_distanceUnit == DistanceUnit.Km ? km : km / KmPerMile);

    /// <summary>A canonical weight (kg) in the display unit, not rounded (for chart values, which round only when written).</summary>
    public double ChartWeight(double kg) => _weightUnit == WeightUnit.Kg ? kg : kg / KgPerLb;

    /// <summary>A canonical distance (km) in the display unit, not rounded.</summary>
    public double ChartDistance(double km) => _distanceUnit == DistanceUnit.Km ? km : km / KmPerMile;

    /// <summary>A weight typed in the display unit, as exact canonical kg.</summary>
    public double WeightFromDisplay(double value) =>
        _weightUnit == WeightUnit.Kg ? value : value * KgPerLb;

    /// <summary>A distance typed in the display unit, as exact canonical km.</summary>
    public double DistanceFromDisplay(double value) =>
        _distanceUnit == DistanceUnit.Km ? value : value * KmPerMile;

    public BothWeights BothWeights(double kg) => new(RoundWeightKg(kg), RoundWeightLb(kg / KgPerLb));

    public BothDistances BothDistances(double km) => new(RoundDistance(km), RoundDistance(km / KmPerMile));

    private static double RoundWeightKg(double kg) => RoundToStep(kg, KgDisplayStep);

    private static double RoundWeightLb(double lb) => RoundToStep(lb, LbDisplayStep);

    private static double RoundToStep(double value, double step) =>
        Math.Round(value / step, MidpointRounding.AwayFromZero) * step;

    private static double RoundDistance(double value) =>
        Math.Round(value, DistanceDecimals, MidpointRounding.AwayFromZero);
}
