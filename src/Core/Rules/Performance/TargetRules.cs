using System.Globalization;
using Core.Model;

namespace Core.Rules.Performance;

/// <summary>The progression rules behind <see cref="SuggestedTarget"/>. Assumptions 2 to 4 of the A2 spec live here.</summary>
internal static class TargetRules
{
    private const int CarryCapM = 40;
    private const int CarryResetM = 20;
    private const int CarryStepM = 10;
    private const int RepDrop = 2;
    private const int TimeStepSeconds = 5;
    private const int HoldRpe = 9;
    private const int EasyRpe = 6;

    public static SuggestedTarget? For(LogKind kind, LastResult? last, BestResult? best, Plan plan, SessionExercise? row)
    {
        if (last is null) return null;
        var rpe = last.Rpe;
        var hold = rpe >= HoldRpe;
        var steps = rpe <= EasyRpe ? 2 : 1;

        return kind switch
        {
            LogKind.Load or LogKind.BodyweightLoad when last.BestSet is { Reps: not null } s
                => Load(kind, s, rpe, hold, steps, plan.WeightIncrementKg, row),
            LogKind.Carry when last.BestSet is { DistanceM: not null } s
                => Carry(s, rpe, hold, steps, plan.WeightIncrementKg),
            LogKind.Time when best?.Set.DurationSeconds is { } bestSeconds
                => Time(bestSeconds, last, rpe, hold, steps),
            LogKind.Reps or LogKind.Seconds or LogKind.Minutes or LogKind.DistanceKm or LogKind.DistanceM when best is not null
                => Other(kind, best.Set, last.BestSet, rpe, hold, steps),
            _ => null,
        };
    }

    private static SuggestedTarget Load(LogKind kind, WorkoutSet s, int? rpe, bool hold, int steps, double increment, SessionExercise? row)
    {
        var top = row?.RepsMax ?? row?.Reps;
        var weight = s.WeightKg ?? 0;
        var reps = s.Reps!.Value;
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
            ? $"add {Num(w - weight)} kg, back to {r} reps"
            : $"add {Plural(r - reps, "rep")}";
        return new(bodyweightOnly ? s.WeightKg : w, r, null, null, null, $"{did}: {Easy(steps)}{change}");
    }

    private static SuggestedTarget Carry(WorkoutSet s, int? rpe, bool hold, int steps, double increment)
    {
        var weight = s.WeightKg ?? 0;
        var distance = s.DistanceM!.Value;
        var did = $"Carried {(weight > 0 ? $"{Num(weight)} kg for " : "")}{Num(distance)} m{AtRpe(rpe)}";

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
            ? $"add {Num(w - weight)} kg, back to {Num(d)} m"
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

        var step = TimeStepSeconds * steps;
        var reason = steps == 2
            ? $"Best {Clock(bestSeconds)}; RPE {rpe} last time, so beat it by {step} s"
            : $"Best {Clock(bestSeconds)}; beat it by {step} s";
        return new(null, null, null, Math.Max(1, bestSeconds - step), null, reason);
    }

    private static SuggestedTarget? Other(LogKind kind, WorkoutSet best, WorkoutSet? lastBest, int? rpe, bool hold, int steps)
    {
        if (hold)
        {
            return lastBest is null
                ? null
                : Stepped(kind, lastBest, 0, $"Last {Describe(kind, lastBest)}{AtRpe(rpe)}: hold the same result");
        }

        var lead = steps == 2 ? $"; RPE {rpe} last time, so add " : "; add ";
        return Stepped(kind, best, steps, $"Best {Describe(kind, best)}{lead}{StepText(kind, steps)}");
    }

    private static SuggestedTarget Stepped(LogKind kind, WorkoutSet from, int steps, string reason) => kind switch
    {
        LogKind.Reps => new(null, from.Reps!.Value + steps, null, null, null, reason),
        LogKind.Seconds => new(null, null, null, from.DurationSeconds!.Value + 5 * steps, null, reason),
        LogKind.Minutes => new(null, null, null, from.DurationSeconds!.Value + 60 * steps, null, reason),
        LogKind.DistanceKm => new(null, null, null, null, from.DistanceKm!.Value + 0.5 * steps, reason),
        _ => new(null, null, from.DistanceM!.Value + 10 * steps, null, null, reason),
    };

    private static string StepText(LogKind kind, int steps) => kind switch
    {
        LogKind.Reps => Plural(steps, "rep"),
        LogKind.Seconds => $"{5 * steps} s",
        LogKind.Minutes => $"{steps} min",
        LogKind.DistanceKm => $"{Num(0.5 * steps)} km",
        _ => $"{10 * steps} m",
    };

    private static string Describe(LogKind kind, WorkoutSet s) => kind switch
    {
        LogKind.Reps => $"{s.Reps} reps",
        LogKind.Seconds => $"{s.DurationSeconds} s",
        LogKind.Minutes => $"{Num(s.DurationSeconds!.Value / 60.0)} min",
        LogKind.DistanceKm => $"{Num(s.DistanceKm!.Value)} km",
        _ => $"{Num(s.DistanceM!.Value)} m",
    };

    private static string Clock(int seconds) =>
        seconds >= 3600
            ? $"{seconds / 3600}:{seconds % 3600 / 60:00}:{seconds % 60:00}"
            : $"{seconds / 60}:{seconds % 60:00}";

    private static string AtRpe(int? rpe) => rpe is { } r ? $" at RPE {r}" : "";

    private static string Easy(int steps) => steps == 2 ? "easy, " : "";

    private static string Plural(double n, string word) => $"{Num(n)} {word}{(n == 1 ? "" : "s")}";

    private static string Num(double n) => n.ToString("0.##", CultureInfo.InvariantCulture);
}
