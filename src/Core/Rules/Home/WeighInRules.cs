using Core.Ids;
using Core.Model;
using Core.Rules.Parsing;
using Core.Rules.Units;

namespace Core.Rules.Home;

public abstract record WeighInResult
{
    private WeighInResult() { }

    public sealed record Valid(WeighIn WeighIn) : WeighInResult;
    public sealed record Invalid(string Message) : WeighInResult;
}

public static class WeighInRules
{
    private const string UnparsableMessage = "Enter your weight as a number, for example 86.3.";
    private const string OutOfRangeMessage = "That doesn't look like a body weight. Check the number.";

    public static WeighInResult Create(string? text, UnitConverter converter, DateOnly today, IIdGenerator ids)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new WeighInResult.Invalid(UnparsableMessage);
        }

        if (!DecimalText.TryParse(text, out var value) || !double.IsFinite(value))
        {
            return new WeighInResult.Invalid(UnparsableMessage);
        }

        var rawKg = converter.WeightFromDisplay(value);
        if (rawKg < 20.0 || rawKg > 400.0)
        {
            return new WeighInResult.Invalid(OutOfRangeMessage);
        }

        var kg = Math.Round(rawKg, 2, MidpointRounding.AwayFromZero);
        return new WeighInResult.Valid(new WeighIn(ids.NewId(), today, kg));
    }
}
