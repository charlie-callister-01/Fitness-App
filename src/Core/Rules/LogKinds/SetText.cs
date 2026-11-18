using Core.Model;
using Core.Rules.Parsing;
using Core.Rules.Performance;
using Core.Rules.Units;

namespace Core.Rules.LogKinds;

/// <summary>Plain-text results for the Exercise header ("22 kg × 8"), in the owner's display units. The wording per kind is in <see cref="RowSpec"/>.</summary>
public static class SetText
{
    /// <summary>A Set as text for its log kind. Empty when the Set lacks what the kind needs (a text Exercise has no Set to show).</summary>
    public static string Result(LogKind kind, WorkoutSet set, UnitConverter units) =>
        RowSpec.For(kind)?.Result(set, units) ?? "";

    /// <summary>A result without the weight unit, for the "beats 22 × 8" half of the PR toast; every other kind reads as <see cref="Result"/>.</summary>
    public static string Short(LogKind kind, WorkoutSet set, UnitConverter units) =>
        RowSpec.For(kind)?.Short?.Invoke(set, units) ?? Result(kind, set, units);

    public static string Target(LogKind kind, SuggestedTarget target, UnitConverter units) =>
        Result(kind, RowPrefill.AsSet(target), units);
}
