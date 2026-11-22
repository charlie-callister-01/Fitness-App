using Core.Model;

namespace LegacyConverter;

internal static class ExerciseBuilder
{
    private static readonly (string Name, LogKind Kind)[] RetiredExercises =
    [
        ("Chest-supported row", LogKind.Load),
        ("Farmers carry", LogKind.Carry),
        ("Incline DB press", LogKind.Load),
    ];

    public static LogKind MapLogKind(string raw, string exerciseName) => raw.Trim() switch
    {
        "load" => LogKind.Load,
        "carry" => LogKind.Carry,
        "reps" => LogKind.Reps,
        "secs" => LogKind.Seconds,
        "mins" => LogKind.Minutes,
        "km" => LogKind.DistanceKm,
        "m" => LogKind.DistanceM,
        "time" => LogKind.Time,
        "kg" => LogKind.BodyMeasurementKg,
        "cm" => LogKind.BodyMeasurementCm,
        "text" => LogKind.Text,
        "-" => LogKind.TickOnly,
        _ => throw new FormatException($"Unknown log kind '{raw}' for exercise '{exerciseName}'."),
    };

    public static IReadOnlyList<Exercise> Build(
        IReadOnlyList<ConfigExerciseRow> rows,
        List<ConversionNote> notes)
    {
        var byId = new Dictionary<string, Exercise>(StringComparer.Ordinal);
        var ordered = new List<Exercise>();

        foreach (var row in rows)
        {
            if (string.Equals(row.Name, "Bodyweight", StringComparison.OrdinalIgnoreCase))
            {
                if (!notes.Any(n => n.Subject == "Exercise: Bodyweight"))
                {
                    notes.Add(new ConversionNote(
                        ConversionNoteKind.Dropped,
                        "Exercise: Bodyweight",
                        "Bodyweight is a standalone Weigh-in in the app."));
                }

                continue;
            }

            var id = LegacyIds.ToId(row.Name);
            var kind = MapLogKind(row.LogAs, row.Name);
            var isBenchmark = row.SessionCodes.Any(c => string.Equals(c, "T", StringComparison.OrdinalIgnoreCase));

            if (byId.TryGetValue(id, out var existing))
            {
                if (existing.LogKind != kind)
                {
                    throw new FormatException(
                        $"Conflicting log kinds for '{row.Name}': {existing.LogKind} vs {kind}.");
                }

                if (isBenchmark && !existing.IsBenchmark)
                {
                    var updated = existing with { IsBenchmark = true };
                    byId[id] = updated;
                    var idx = ordered.FindIndex(e => e.Id == id);
                    ordered[idx] = updated;
                }

                continue;
            }

            var exercise = new Exercise(id, row.Name, kind, Tags: null, IsBenchmark: isBenchmark);
            byId[id] = exercise;
            ordered.Add(exercise);
        }

        foreach (var (name, kind) in RetiredExercises)
        {
            var id = LegacyIds.ToId(name);
            if (!byId.ContainsKey(id))
            {
                var retired = new Exercise(id, name, kind, Tags: null, IsBenchmark: false);
                byId[id] = retired;
                ordered.Add(retired);
            }
        }

        return ordered;
    }
}
