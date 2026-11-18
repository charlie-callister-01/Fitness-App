using Core.Model;
using Core.Rules.Units;

namespace Core.Rules.LogKinds;

/// <summary>Which of a row's two fields a stepper changes.</summary>
public enum StepField
{
    First,
    Second,
}

/// <summary>A stepper's step in display units, with the unit word used in its accessible name ("Increase by 2 kg").</summary>
public readonly record struct StepSize(StepField Field, double Amount, string Unit);

/// <summary>How far the ConfirmBar steppers move a Set, by log kind (design 8.2). Pure.</summary>
public static class StepRules
{
    private const int SecondsStep = 5;
    private const int RepStep = 1;

    /// <summary>The step for the kind, or null when the kind has no steppers (time, distance and the rest).</summary>
    /// <param name="planIncrementKg">The Plan's weight increment, always kg.</param>
    public static StepSize? For(LogKind kind, UnitConverter units, double planIncrementKg, WeightUnit displayUnit) => kind switch
    {
        LogKind.Load or LogKind.Carry => new(
            StepField.First, units.DisplayWeight(planIncrementKg), displayUnit == WeightUnit.Kg ? "kg" : "lb"),
        LogKind.BodyweightLoad => new(StepField.Second, RepStep, "rep"),
        LogKind.Seconds => new(StepField.First, SecondsStep, "s"),
        _ => null,
    };
}
