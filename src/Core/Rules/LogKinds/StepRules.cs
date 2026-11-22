using Core.Rules.Formatting;
using Core.Rules.Parsing;

namespace Core.Rules.LogKinds;

/// <summary>Which of a row's two fields a stepper changes.</summary>
public enum StepField
{
    First,
    Second,
}

/// <summary>
/// A stepper's step in display units, with the unit word used in its accessible name ("Increase by 2 kg"). Which kinds have
/// one, and how big it is, is in <see cref="RowSpec"/>.
/// </summary>
public readonly record struct StepSize(StepField Field, double Amount, string Unit)
{
    /// <summary>
    /// The field's text after one press: the typed number (a comma counts as a decimal point; anything unreadable counts as 0)
    /// plus or minus the step, never below zero, as a short number such as "22.5".
    /// </summary>
    /// <param name="direction">-1 for minus, 1 for plus.</param>
    public string Apply(string typed, int direction)
    {
        var start = DecimalText.TryParse(typed, out var number) ? number : 0;
        return DisplayFormat.Trimmed(Math.Max(0, Math.Round(start + direction * Amount, 2)));
    }
}
