using System.Globalization;
using System.Text.RegularExpressions;
using Core.Model;

namespace LegacyConverter;

internal static partial class HistoryBuilder
{
    private static readonly Dictionary<string, string> SessionIdByCode = new(StringComparer.OrdinalIgnoreCase)
    {
        ["A"] = "a-lower",
        ["B"] = "b-upper",
        ["C"] = "c-engine",
        ["D"] = "d-full-body",
        ["E"] = "e-long",
        ["M"] = "m-mobility",
        ["T"] = "benchmark",
    };

    public static (IReadOnlyList<WeighIn> WeighIns, IReadOnlyList<Workout> Workouts) Build(
        LegacyFolder folder,
        Plan plan,
        IReadOnlyDictionary<string, Exercise> exercisesById,
        List<ConversionNote> notes)
    {
        var weighInsByDate = new SortedDictionary<DateOnly, WeighIn>();

        if (!string.IsNullOrWhiteSpace(folder.LifeSettingsMarkdown))
        {
            var lifeFm = ParseFrontMatter(folder.LifeSettingsMarkdown);
            if (lifeFm.Scalars.TryGetValue("start_weight", out var weightStr)
                && lifeFm.Scalars.TryGetValue("start_weight_date", out var dateStr)
                && double.TryParse(weightStr, CultureInfo.InvariantCulture, out var kg)
                && DateOnly.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                weighInsByDate[date] = new WeighIn($"w-{date:yyyy-MM-dd}", date, kg);
            }
        }

        if (!string.IsNullOrWhiteSpace(folder.WeightsCsv))
        {
            ParseWeightsCsv(folder.WeightsCsv, weighInsByDate);
        }

        var workouts = new List<Workout>();
        var sessionsById = plan.Sessions.ToDictionary(s => s.Id, StringComparer.Ordinal);

        foreach (var fileName in folder.Logs.Keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            var markdown = folder.Logs[fileName];
            var fm = ParseFrontMatter(markdown);

            if (!fm.Scalars.TryGetValue("type", out var type)
                || !string.Equals(type, "log", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!fm.Scalars.TryGetValue("session", out var sessionCode)
                || !fm.Scalars.TryGetValue("date", out var dateStr))
            {
                notes.Add(new ConversionNote(
                    ConversionNoteKind.Warning,
                    $"Log: {fileName}",
                    "Missing session or date in front matter; skipped."));
                continue;
            }

            var date = DateOnly.ParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture);

            if (fm.Scalars.TryGetValue("bodyweight", out var bwStr)
                && double.TryParse(bwStr, CultureInfo.InvariantCulture, out var bwKg))
            {
                weighInsByDate[date] = new WeighIn($"w-{date:yyyy-MM-dd}", date, bwKg);
            }

            var workoutId = $"w-{date:yyyy-MM-dd}-{sessionCode.Trim().ToLowerInvariant()}";
            var isWeighInSession = string.Equals(sessionCode, "W", StringComparison.OrdinalIgnoreCase);

            string? sessionId = null;
            IReadOnlyList<SessionExercise> planned = [];
            if (!isWeighInSession)
            {
                if (!SessionIdByCode.TryGetValue(sessionCode, out sessionId))
                {
                    throw new FormatException($"Unknown session code '{sessionCode}' in log '{fileName}'.");
                }

                if (sessionsById.TryGetValue(sessionId, out var session))
                {
                    planned = session.Exercises;
                }
            }

            var entries = new List<WorkoutEntry>();
            var seenExerciseIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (var (key, value) in fm.Scalars)
            {
                if (key is "type" or "session" or "date" or "bodyweight" or "done")
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                var exerciseId = LegacyIds.ToId(key);
                if (!exercisesById.TryGetValue(exerciseId, out var exercise))
                {
                    notes.Add(new ConversionNote(
                        ConversionNoteKind.Warning,
                        $"Log: {fileName} ({key})",
                        $"Exercise '{key}' not found in library; skipped."));
                    continue;
                }

                var sets = ParseSets(workoutId, exercise, value, fileName, notes);
                if (sets.Count == 0 && exercise.LogKind != LogKind.TickOnly)
                {
                    continue;
                }

                seenExerciseIds.Add(exerciseId);
                entries.Add(new WorkoutEntry(
                    ExerciseId: exerciseId,
                    Status: EntryStatus.Done,
                    SkipReason: null,
                    SwappedFromExerciseId: null,
                    Rpe: null,
                    Sets: sets));
            }

            foreach (var doneKey in fm.DoneItems)
            {
                var exerciseId = LegacyIds.ToId(doneKey);
                if (!exercisesById.ContainsKey(exerciseId))
                {
                    notes.Add(new ConversionNote(
                        ConversionNoteKind.Warning,
                        $"Log: {fileName} (done: {doneKey})",
                        $"Done item '{doneKey}' not found in library; skipped."));
                    continue;
                }

                if (seenExerciseIds.Add(exerciseId))
                {
                    entries.Add(new WorkoutEntry(
                        ExerciseId: exerciseId,
                        Status: EntryStatus.Done,
                        SkipReason: null,
                        SwappedFromExerciseId: null,
                        Rpe: null,
                        Sets: []));
                }
            }

            if (entries.Count == 0)
            {
                notes.Add(new ConversionNote(
                    ConversionNoteKind.Dropped,
                    $"Log: {fileName}",
                    "Empty log shell (no logged values or ticked items)."));
                continue;
            }

            workouts.Add(new Workout(
                Id: workoutId,
                Date: date,
                SessionId: sessionId,
                LoggedAfterwards: true,
                StartedAt: null,
                EndedAt: null,
                Planned: planned,
                Entries: entries,
                PainCheck: null));
        }

        return (
            weighInsByDate.Values.ToList(),
            workouts.OrderBy(w => w.Date).ThenBy(w => w.Id, StringComparer.Ordinal).ToList());
    }

    private static IReadOnlyList<WorkoutSet> ParseSets(
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
                var parts = value.Split(':');
                if (parts.Length != 2
                    || !int.TryParse(parts[0], CultureInfo.InvariantCulture, out var mins)
                    || !int.TryParse(parts[1], CultureInfo.InvariantCulture, out var secs)
                    || secs is < 0 or >= 60)
                {
                    throw new FormatException($"Unreadable Time value '{rawValue}' for '{exercise.Name}' in '{fileName}'.");
                }

                return [MakeSet(workoutId, exercise.Id, 1, durationSeconds: (mins * 60) + secs)];
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

    private static void ParseWeightsCsv(string csv, SortedDictionary<DateOnly, WeighIn> weighInsByDate)
    {
        foreach (var rawLine in csv.Replace("\r\n", "\n").Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("date,", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var parts = line.Split(',');
            if (parts.Length < 2)
            {
                throw new FormatException($"Invalid weights.csv row: '{line}'.");
            }

            var date = DateOnly.ParseExact(parts[0].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var kg = double.Parse(parts[1].Trim(), CultureInfo.InvariantCulture);
            weighInsByDate[date] = new WeighIn($"w-{date:yyyy-MM-dd}", date, kg);
        }
    }

    private sealed record FrontMatter(
        Dictionary<string, string> Scalars,
        List<string> DoneItems);

    private static FrontMatter ParseFrontMatter(string markdown)
    {
        var scalars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var doneItems = new List<string>();
        var lines = markdown.Replace("\r\n", "\n").Split('\n');

        if (lines.Length == 0 || lines[0].Trim() != "---")
        {
            return new FrontMatter(scalars, doneItems);
        }

        var inDone = false;
        for (var i = 1; i < lines.Length; i++)
        {
            var raw = lines[i];
            var trimmed = raw.Trim();
            if (trimmed == "---")
            {
                break;
            }

            if (inDone && trimmed.StartsWith("- ", StringComparison.Ordinal))
            {
                var item = trimmed[2..].Trim().Trim('"', '\'');
                if (item.Length > 0)
                {
                    doneItems.Add(item);
                }

                continue;
            }

            inDone = false;

            var colon = trimmed.IndexOf(':');
            if (colon <= 0)
            {
                continue;
            }

            var key = trimmed[..colon].Trim();
            var value = trimmed[(colon + 1)..].Trim();

            if (string.Equals(key, "done", StringComparison.OrdinalIgnoreCase))
            {
                if (value.Length == 0)
                {
                    inDone = true;
                }

                continue;
            }

            scalars[key] = value.Trim('"', '\'');
        }

        return new FrontMatter(scalars, doneItems);
    }

    [GeneratedRegex(@"^bw\s*[x×]\s*(?<reps>\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex BodyweightRepsRegex();

    [GeneratedRegex(@"^(?<w>\d+(?:\.\d+)?)\s*[x×]\s*(?<v>\d+(?:\.\d+)?)$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex WeightTimesValueRegex();
}
