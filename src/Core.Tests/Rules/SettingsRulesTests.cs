using Core.Model;
using Core.Rules.Editing;

namespace Core.Tests.Rules;

public sealed class SettingsRulesTests
{
    [Fact]
    public void TrySetWeightIncrement_accepts_positive_number_and_rejects_zero_or_invalid()
    {
        var settings = Settings.Default;

        var ok = SettingsRules.TrySetWeightIncrement(settings, "1.25");
        Assert.True(ok.IsValid);
        Assert.Equal(1.25, ok.Settings!.WeightIncrementKg);

        Assert.False(SettingsRules.TrySetWeightIncrement(settings, "0").IsValid);
        Assert.False(SettingsRules.TrySetWeightIncrement(settings, "-2.5").IsValid);
        Assert.False(SettingsRules.TrySetWeightIncrement(settings, "abc").IsValid);
        Assert.False(SettingsRules.TrySetWeightIncrement(settings, "").IsValid);
    }

    [Fact]
    public void TrySetRestTimer_accepts_non_negative_whole_seconds_and_rejects_negative_or_non_integer()
    {
        var settings = Settings.Default;

        var zero = SettingsRules.TrySetRestTimer(settings, "0");
        Assert.True(zero.IsValid);
        Assert.Equal(0, zero.Settings!.RestTimerSeconds);

        var ok = SettingsRules.TrySetRestTimer(settings, "120");
        Assert.True(ok.IsValid);
        Assert.Equal(120, ok.Settings!.RestTimerSeconds);

        Assert.False(SettingsRules.TrySetRestTimer(settings, "-1").IsValid);
        Assert.False(SettingsRules.TrySetRestTimer(settings, "1.5").IsValid);
        Assert.False(SettingsRules.TrySetRestTimer(settings, "").IsValid);
    }

    [Fact]
    public void TrySetGoalWeight_converts_from_active_display_unit_and_allows_blank_to_clear()
    {
        var lbSettings = Settings.Default with { DisplayWeightUnit = WeightUnit.Lb };

        var ok = SettingsRules.TrySetGoalWeight(lbSettings, "165");
        Assert.True(ok.IsValid);
        Assert.NotNull(ok.Settings!.GoalWeightKg);
        Assert.Equal(165 * 0.45359237, ok.Settings.GoalWeightKg!.Value, precision: 6);

        var cleared = SettingsRules.TrySetGoalWeight(ok.Settings, "   ");
        Assert.True(cleared.IsValid);
        Assert.Null(cleared.Settings!.GoalWeightKg);

        Assert.False(SettingsRules.TrySetGoalWeight(lbSettings, "0").IsValid);
        Assert.False(SettingsRules.TrySetGoalWeight(lbSettings, "-10").IsValid);
        Assert.False(SettingsRules.TrySetGoalWeight(lbSettings, "nope").IsValid);
    }

    [Fact]
    public void FormatGoalWeight_formats_in_active_display_unit()
    {
        var kg = Settings.Default with { DisplayWeightUnit = WeightUnit.Kg, GoalWeightKg = 75.0 };
        Assert.Equal("75", SettingsRules.FormatGoalWeight(kg));

        var lb = kg with { DisplayWeightUnit = WeightUnit.Lb };
        // 75 / 0.45359237 = 165.346..., rounded to 0.5 lb step = 165.5
        Assert.Equal("165.5", SettingsRules.FormatGoalWeight(lb));

        Assert.Equal("", SettingsRules.FormatGoalWeight(Settings.Default));
    }

    [Fact]
    public void TrySetStreakMinWorkouts_requires_at_least_one()
    {
        var settings = Settings.Default;

        var ok = SettingsRules.TrySetStreakMinWorkouts(settings, "4");
        Assert.True(ok.IsValid);
        Assert.Equal(4, ok.Settings!.StreakMinWorkoutsPerWeek);

        Assert.False(SettingsRules.TrySetStreakMinWorkouts(settings, "0").IsValid);
        Assert.False(SettingsRules.TrySetStreakMinWorkouts(settings, "-2").IsValid);
        Assert.False(SettingsRules.TrySetStreakMinWorkouts(settings, "2.5").IsValid);
        Assert.False(SettingsRules.TrySetStreakMinWorkouts(settings, "").IsValid);
    }
}
