using System.Globalization;
using Core.Model;

namespace LegacyConverter;

internal static class HistoryBuilder
{
    public static (IReadOnlyList<WeighIn> WeighIns, IReadOnlyList<Workout> Workouts) Build(
        LegacyFolder folder,
        Plan plan,
        IReadOnlyDictionary<string, Exercise> exercisesById,
        List<ConversionNote> notes)
    {
        var weighInsByDate = new SortedDictionary<DateOnly, WeighIn>();
        WeighInParser.AddFromLifeSettings(folder.LifeSettingsMarkdown, weighInsByDate);
        WeighInParser.AddFromWeightsCsv(folder.WeightsCsv, weighInsByDate);

        var workouts = new List<Workout>();
        var sessionsById = plan.Sessions.ToDictionary(s => s.Id, StringComparer.Ordinal);

        foreach (var fileName in folder.Logs.Keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            var workout = TryBuildWorkout(
                fileName,
                folder.Logs[fileName],
                sessionsById,
                exercisesById,
                weighInsByDate,
                notes);

            if (workout is not null)
            {
                workouts.Add(workout);
            }
        }

        return (
            weighInsByDate.Values.ToList(),
            workouts.OrderBy(w => w.Date).ThenBy(w => w.Id, StringComparer.Ordinal).ToList());
    }

    private static Workout? TryBuildWorkout(
        string fileName,
        string markdown,
        IReadOnlyDictionary<string, Session> sessionsById,
        IReadOnlyDictionary<string, Exercise> exercisesById,
        SortedDictionary<DateOnly, WeighIn> weighInsByDate,
        List<ConversionNote> notes)
    {
        var fm = LegacyParsing.ParseFrontMatter(markdown);

        if (!fm.Scalars.TryGetValue("type", out var type)
            || !string.Equals(type, "log", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!fm.Scalars.TryGetValue("session", out var sessionCode)
            || !fm.Scalars.TryGetValue("date", out var dateStr))
        {
            notes.Add(new ConversionNote(
                ConversionNoteKind.Warning,
                $"Log: {fileName}",
                "Missing session or date in front matter; skipped."));
            return null;
        }

        var date = DateOnly.ParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture);

        if (fm.Scalars.TryGetValue("bodyweight", out var bwStr)
            && double.TryParse(bwStr, CultureInfo.InvariantCulture, out var bwKg))
        {
            WeighInParser.AddWeighIn(weighInsByDate, date, bwKg);
        }

        var workoutId = $"w-{date:yyyy-MM-dd}-{sessionCode.Trim().ToLowerInvariant()}";
        var isWeighInSession = string.Equals(sessionCode, "W", StringComparison.OrdinalIgnoreCase);

        string? sessionId = null;
        IReadOnlyList<SessionExercise> planned = [];
        if (!isWeighInSession)
        {
            if (!PlanBuilder.TryGetSessionId(sessionCode, out sessionId))
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

            var sets = WorkoutSetParser.ParseSets(workoutId, exercise, value, fileName, notes);
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

        if (fm.Lists.TryGetValue("done", out var doneItems))
        {
            foreach (var doneKey in doneItems)
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
        }

        if (entries.Count == 0)
        {
            notes.Add(new ConversionNote(
                ConversionNoteKind.Dropped,
                $"Log: {fileName}",
                "Empty log shell (no logged values or ticked items)."));
            return null;
        }

        return new Workout(
            Id: workoutId,
            Date: date,
            SessionId: sessionId,
            LoggedAfterwards: true,
            StartedAt: null,
            EndedAt: null,
            Planned: planned,
            Entries: entries,
            PainCheck: null);
    }
}
