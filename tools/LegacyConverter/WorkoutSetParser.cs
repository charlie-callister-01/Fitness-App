using System.Globalization;
using System.Text.RegularExpressions;
using Core.Model;

namespace LegacyConverter;

internal static partial class WorkoutSetParser
{
    public static IReadOnlyList<WorkoutSet> ParseSets(
        string workoutId,
        Exercise exercise,
        string rawValue,
        string fileName,
        List<ConversionNote> notes)
    {
        var value = rawValue.Trim();

        // Named special case: Side plank "2.3" = 2 sets of 30 s
        if (value == "2.3")
        {
            if (exercise.Id == "side-plank")
            {
                return
                [
                    MakeSet(workoutId, exercise.Id, 1, durationSeconds: 30),
                    MakeSet(workoutId, exercise.Id, 2, durationSeconds: 30),
                ];
            }

            notes.Add(new ConversionNote(
                ConversionNoteKind.Warning,
                $"Log: {fileName} ({exercise.Name})",
                $"Unexpected '2.3' shorthand on '{exercise.Name}'; skipped."));
            return [];
        }

        switch (exercise.LogKind)
        {
            case LogKind.Load:
            {
                var bwMatch = BodyweightRepsRegex().Match(value);
                if (bwMatch.Success)
                {
                    var reps = int.Parse(bwMatch.Groups["reps"].Value, CultureInfo.InvariantCulture);
                    return [MakeSet(workoutId, exercise.Id, 1, weightKg: 0, reps: reps)];
                }

                var loadMatch = WeightTimesValueRegex().Match(value);
                if (loadMatch.Success)
                {
                    var kg = double.Parse(loadMatch.Groups["w"].Value, CultureInfo.InvariantCulture);
                    var reps = int.Parse(loadMatch.Groups["v"].Value, CultureInfo.InvariantCulture);
                    return [MakeSet(workoutId, exercise.Id, 1, weightKg: kg, reps: reps)];
                }

                throw new FormatException($"Unreadable Load value '{rawValue}' for '{exercise.Name}' in '{fileName}'.");
            }

            case LogKind.Carry:
            {
                var carryMatch = WeightTimesValueRegex().Match(value);
                if (carryMatch.Success)
                {
                    var kg = double.Parse(carryMatch.Groups["w"].Value, CultureInfo.InvariantCulture);
                    var metres = double.Parse(carryMatch.Groups["v"].Value, CultureInfo.InvariantCulture);
                    return [MakeSet(workoutId, exercise.Id, 1, weightKg: kg, distanceM: metres)];
                }

                throw new FormatException($"Unreadable Carry value '{rawValue}' for '{exercise.Name}' in '{fileName}'.");
            }

            case LogKind.Reps:
            {
                var reps = int.Parse(value, CultureInfo.InvariantCulture);
                return [MakeSet(workoutId, exercise.Id, 1, reps: reps)];
            }

            case LogKind.Seconds:
            {
                var secs = int.Parse(value, CultureInfo.InvariantCulture);
                return [MakeSet(workoutId, exercise.Id, 1, durationSeconds: secs)];
            }

            case LogKind.Minutes:
            {
                var mins = double.Parse(value, CultureInfo.InvariantCulture);
                return [MakeSet(workoutId, exercise.Id, 1, durationSeconds: (int)Math.Round(mins * 60))];
            }

            case LogKind.DistanceKm:
            {
                var km = double.Parse(value, CultureInfo.InvariantCulture);
                return [MakeSet(workoutId, exercise.Id, 1, distanceKm: km)];
            }

            case LogKind.DistanceM:
            {
                var m = double.Parse(value, CultureInfo.InvariantCulture);
                return [MakeSet(workoutId, exercise.Id, 1, distanceM: m)];
            }

            case LogKind.Time:
            {
                if (!LegacyParsing.TryParseMinutesSeconds(value, out var totalSeconds))
                {
                    throw new FormatException($"Unreadable Time value '{rawValue}' for '{exercise.Name}' in '{fileName}'.");
                }

                return [MakeSet(workoutId, exercise.Id, 1, durationSeconds: totalSeconds)];
            }

            case LogKind.BodyMeasurementKg:
            {
                var kg = double.Parse(value, CultureInfo.InvariantCulture);
                return [MakeSet(workoutId, exercise.Id, 1, weightKg: kg)];
            }

            case LogKind.BodyMeasurementCm:
            {
                var cm = double.Parse(value, CultureInfo.InvariantCulture);
                return [MakeSet(workoutId, exercise.Id, 1, measurementCm: cm)];
            }

            case LogKind.TickOnly:
                return [];

            default:
                throw new FormatException($"Cannot parse value '{rawValue}' for LogKind.{exercise.LogKind} in '{fileName}'.");
        }
    }

    private static WorkoutSet MakeSet(
        string workoutId,
        string exerciseId,
        int index,
        double? weightKg = null,
        int? reps = null,
        double? distanceKm = null,
        int? durationSeconds = null,
        double? distanceM = null,
        double? measurementCm = null) =>
        new(
            Id: $"{workoutId}-{exerciseId}-s{index}",
            WeightKg: weightKg,
            Reps: reps,
            DistanceKm: distanceKm,
            DurationSeconds: durationSeconds,
            DistanceM: distanceM,
            IsMax: false,
            MeasurementCm: measurementCm);

    [GeneratedRegex(@"^bw\s*[x×]\s*(?<reps>\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex BodyweightRepsRegex();

    [GeneratedRegex(@"^(?<w>\d+(?:\.\d+)?)\s*[x×]\s*(?<v>\d+(?:\.\d+)?)$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex WeightTimesValueRegex();
}
