using System.Globalization;
using Core.Rules.Insight;
using Core.Model;
using Core.Rules.LogKinds;
using Core.Rules.Units;

namespace Core.Rules.Performance;

/// <summary>The progression rules behind <see cref="SuggestedTarget"/>. Assumptions 2 to 4 of the A2 spec live here.</summary>
internal static class TargetRules
{
    private const int CarryCapM = 40;
    private const int CarryResetM = 20;
    private const int CarryStepM = 10;
    private const int RepDrop = 2;
    private const int HoldRpe = 9;
    private const int EasyRpe = 6;

    public static SuggestedTarget? For(LogKind kind, LastResult? last, BestResult? best, Plan plan, SessionExercise? row, UnitConverter units)
    {
        if (last is null) return null;
        var rpe = last.Rpe;
        var hold = rpe >= HoldRpe;
        var steps = rpe <= EasyRpe ? 2 : 1;

        return kind switch
        {
            LogKind.Load or LogKind.BodyweightLoad when last.BestSet is { Reps: { } reps } s
                => Load(kind, s, reps, rpe, hold, steps, plan.WeightIncrementKg, row, units),
            LogKind.Carry when last.BestSet is { DistanceM: { } distance } s
                => Carry(s, distance, rpe, hold, steps, plan.WeightIncrementKg, units),
            LogKind.Time when best?.Set.DurationSeconds is { } bestSeconds
                => Time(bestSeconds, last, rpe, hold, steps),
            _ when LogKindRules.For(kind).Scalar is { } scalar && best is not null
                => Other(scalar, best.Set, last.BestSet, rpe, hold, steps),
            // No target: load, carry or time without a usable Set; text, tick-only and body measurements; undefined kinds.
            _ => null,
        };
    }

    private static SuggestedTarget Load(LogKind kind, WorkoutSet s, int reps, int? rpe, bool hold, int steps, double increment, SessionExercise? row, UnitConverter units)
    {
        var top = row?.RepsMax ?? row?.Reps;
        var weight = s.WeightKg ?? 0;
        var did = top is { } t ? $"Hit {reps} of {t} reps{AtRpe(rpe)}" : $"Did {reps} reps{AtRpe(rpe)}";

        if (hold)
        {
            return new(s.WeightKg, reps, null, null, null, $"{did}: hold the same weight and reps");
        }

        var (w, r) = (weight, reps);
        if (top is { } ceiling && reps >= ceiling)
        {
            // At the top of the range an easy session applies the increment twice, dropping reps once.
            (w, r) = (weight + increment * steps, Math.Max(1, reps - RepDrop));
        }
        else
        {
            for (var i = 0; i < steps; i++)
            {
                (w, r) = top is null || r < top ? (w, r + 1) : (w + increment, Math.Max(1, r - RepDrop));
            }
        }

        var bodyweightOnly = kind == LogKind.BodyweightLoad && w == 0;
        var change = w > weight
            ? $"add {Weight(units, w - weight)}, back to {r} reps"
            : $"add {Plural(r - reps, "rep")}";
        return new(bodyweightOnly ? s.WeightKg : w, r, null, null, null, $"{did}: {Easy(steps)}{change}");
    }

    private static SuggestedTarget Carry(WorkoutSet s, double distance, int? rpe, bool hold, int steps, double increment, UnitConverter units)
    {
        var weight = s.WeightKg ?? 0;
        var did = $"Carried {(weight > 0 ? $"{Weight(units, weight)} for " : "")}{Num(distance)} m{AtRpe(rpe)}";

        if (hold)
        {
            return new(s.WeightKg, null, distance, null, null, $"{did}: hold the same weight and distance");
        }

        var (w, d) = (weight, distance);
        for (var i = 0; i < steps; i++)
        {
            (w, d) = d < CarryCapM ? (w, Math.Min(d + CarryStepM, CarryCapM)) : (w + increment, CarryResetM);
        }

        var change = w > weight
            ? $"add {Weight(units, w - weight)}, back to {Num(d)} m"
            : $"add {Num(d - distance)} m";
        return new(w > 0 ? w : null, null, d, null, null, $"{did}: {Easy(steps)}{change}");
    }

    private static SuggestedTarget? Time(int bestSeconds, LastResult last, int? rpe, bool hold, int steps)
    {
        if (hold)
        {
            return last.BestSet?.DurationSeconds is { } lastSeconds
                ? new(null, null, null, lastSeconds, null, $"Last {Clock(lastSeconds)}{AtRpe(rpe)}: hold the same time")
                : null;
        }

        var step = LogKindRules.SecondStep * steps;
        var reason = steps == 2
            ? $"Best {Clock(bestSeconds)}; RPE {rpe} last time, so beat it by {step} s"
            : $"Best {Clock(bestSeconds)}; beat it by {step} s";
        return new(null, null, null, Math.Max(1, bestSeconds - step), null, reason);
    }

    private static SuggestedTarget? Other(ScalarProgression p, WorkoutSet best, WorkoutSet? lastBest, int? rpe, bool hold, int steps)
    {
        if (hold)
        {
            return lastBest is not null && p.Measure(lastBest) is { } held
                ? p.Target(held, $"Last {Describe(p, held)}{AtRpe(rpe)}: hold the same result")
                : null;
        }

        if (p.Measure(best) is not { } bestValue) return null;

        var lead = steps == 2 ? $"; RPE {rpe} last time, so add " : "; add ";
        return p.Target(bestValue + p.Amount * steps, $"Best {Describe(p, bestValue)}{lead}{StepText(p, steps)}");
    }

    private static string StepText(ScalarProgression p, int steps)
    {
        var shown = p.Amount * steps / p.PerDisplayUnit;
        return p.Pluralise ? Plural(shown, p.Unit) : $"{Num(shown)} {p.Unit}";
    }

    /// <summary>A stored value in the kind's display unit. A pluralising unit always reads plural ("1 reps").</summary>
    private static string Describe(ScalarProgression p, double stored) =>
        $"{Num(stored / p.PerDisplayUnit)} {p.Unit}{(p.Pluralise ? "s" : "")}";

    internal static string Clock(int seconds) =>
        seconds >= 3600
            ? $"{seconds / 3600}:{seconds % 3600 / 60:00}:{seconds % 60:00}"
            : $"{seconds / 60}:{seconds % 60:00}";

    /// <summary>A weight in the display unit with its label. A kg user reads the exact value, as before; a lb user reads it converted and rounded.</summary>
    private static string Weight(UnitConverter units, double kg) =>
        units.WeightLabel == "kg" ? $"{Num(kg)} kg" : $"{Num(units.DisplayWeight(kg))} {units.WeightLabel}";

    private static string AtRpe(int? rpe) => rpe is { } r ? $" at RPE {r}" : "";

    private static string Easy(int steps) => steps == 2 ? "easy, " : "";

    private static string Plural(double n, string word) => $"{Num(n)} {word}{(n == 1 ? "" : "s")}";

    internal static string Num(double n) => InsightFormat.Trimmed(n);
}
