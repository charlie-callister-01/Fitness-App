using System.Globalization;
using Core.Model;
using Core.Rules.Units;

namespace Core.Rules.Editing;

/// <summary>Result of validating and applying a single Settings field edit.</summary>
public sealed record SettingsUpdateResult(Settings? Settings, string? Error)
{
    public bool IsValid => Settings is not null;

    public static SettingsUpdateResult Valid(Settings settings) => new(settings, null);

    public static SettingsUpdateResult Invalid(string error) => new(null, error);
}

/// <summary>Pure validation and unit-conversion rules for editing <see cref="Settings"/>.</summary>
public static class SettingsRules
{
    public static SettingsUpdateResult TrySetWeightIncrement(Settings current, string? input)
    {
        if (!TryParsePositiveDouble(input, out var value))
        {
            return SettingsUpdateResult.Invalid("Enter a positive weight increment (in kg).");
        }

        return SettingsUpdateResult.Valid(current with { WeightIncrementKg = value });
    }

    public static SettingsUpdateResult TrySetRestTimer(Settings current, string? input)
    {
        if (!int.TryParse(input?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds) || seconds < 0)
        {
            return SettingsUpdateResult.Invalid("Enter 0 or more whole seconds.");
        }

        return SettingsUpdateResult.Valid(current with { RestTimerSeconds = seconds });
    }

    public static SettingsUpdateResult TrySetGoalWeight(Settings current, string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return SettingsUpdateResult.Valid(current with { GoalWeightKg = null });
        }

        var converter = new UnitConverter(current);
        if (!TryParsePositiveDouble(input, out var displayWeight))
        {
            return SettingsUpdateResult.Invalid($"Enter a positive goal weight in {converter.WeightLabel}, or leave blank.");
        }

        return SettingsUpdateResult.Valid(current with { GoalWeightKg = converter.WeightFromDisplay(displayWeight) });
    }

    public static string FormatGoalWeight(Settings settings)
    {
        if (settings.GoalWeightKg is not { } kg)
        {
            return "";
        }

        var displayed = new UnitConverter(settings).DisplayWeight(kg);
        return displayed.ToString("0.##", CultureInfo.InvariantCulture);
    }

    public static SettingsUpdateResult TrySetStreakMinWorkouts(Settings current, string? input)
    {
        if (!int.TryParse(input?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var min) || min < 1)
        {
            return SettingsUpdateResult.Invalid("Enter at least 1 Workout per week.");
        }

        return SettingsUpdateResult.Valid(current with { StreakMinWorkoutsPerWeek = min });
    }

    private static bool TryParsePositiveDouble(string? input, out double value)
    {
        value = 0;
        return double.TryParse(input?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value)
            && double.IsFinite(value)
            && value > 0;
    }
}
