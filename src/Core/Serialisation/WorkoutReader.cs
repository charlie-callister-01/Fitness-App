using Core.Model;

namespace Core.Serialisation;

/// <summary>
/// Reads and validates the <c>workouts</c> collection of a version 1 document: Workouts, their planned snapshot, entries, Sets and Pain check.
/// Exercise references are checked when the document has an Exercise library, and Session references when it has Plans; a Workouts-only
/// document can't be checked here, so those references are resolved when it is imported.
/// </summary>
internal static class WorkoutReader
{
    public static List<Workout>? Read(FieldReader root, IReadOnlyList<Exercise>? exercises, IReadOnlyList<Plan>? plans, IssueSink sink)
    {
        var items = root.OptionalObjectArray("workouts");
        if (items is null)
        {
            return null;
        }

        var context = new Context(
            sink,
            exercises?.Select(e => e.Id).ToHashSet(),
            plans?.SelectMany(p => p.Sessions).Select(s => s.Id).ToHashSet());
        var workouts = new List<Workout>();
        foreach (var (reader, _) in items)
        {
            var errorsBefore = sink.Errors.Count;
            var workout = ReadWorkout(reader, context);
            reader.WarnAboutUnknownFields();

            context.CheckUnique(context.WorkoutIds, reader, "Workout", workout.Id);
            if (sink.Errors.Count == errorsBefore)
            {
                workouts.Add(workout);
            }
        }

        return workouts;
    }

    private static Workout ReadWorkout(FieldReader reader, Context context)
    {
        var id = reader.RequiredId("id");
        var date = reader.RequiredDate("date");
        var sessionId = ReadSessionReference(reader, context);
        var loggedAfterwards = reader.OptionalBool("loggedAfterwards", defaultValue: false);
        var startedAt = reader.OptionalTimestamp("startedAt");
        var endedAt = reader.OptionalTimestamp("endedAt");
        if (startedAt is { } start && endedAt is { } end && end < start)
        {
            var path = reader.ChildPath("endedAt");
            context.Sink.Error(path, "end_before_start", $"{path} ({end:O}) can't be before {reader.ChildPath("startedAt")} ({start:O}).");
        }

        var planned = SessionRowReader.ReadRows(reader, "planned", context.Sink, context.ExerciseIds);
        var entries = ReadEntries(reader, context);
        var painCheck = ReadPainCheck(reader, context);

        return new Workout(id, date, sessionId, loggedAfterwards, startedAt, endedAt, planned, entries, painCheck);
    }

    private static string? ReadSessionReference(FieldReader reader, Context context)
    {
        var sessionId = reader.OptionalString("sessionId");
        if (sessionId is null)
        {
            return null;
        }

        var path = reader.ChildPath("sessionId");
        if (sessionId.Length == 0 || !IsValidId(sessionId))
        {
            context.Sink.Error(path, "invalid_id", $"{path} \"{sessionId}\" is not a valid ID. Use only letters, digits, hyphens and underscores.");
            return null;
        }

        if (context.SessionIds is { } known && !known.Contains(sessionId))
        {
            context.Sink.Error(path, "unknown_session", $"{path} \"{sessionId}\" is not a Session in any Plan in this document.");
        }

        return sessionId;
    }

    private static bool IsValidId(string id) => id.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');

    private static List<WorkoutEntry> ReadEntries(FieldReader workout, Context context)
    {
        var entries = new List<WorkoutEntry>();
        foreach (var (reader, _) in workout.OptionalObjectArray("entries") ?? [])
        {
            var errorsBefore = context.Sink.Errors.Count;
            var exerciseId = SessionRowReader.ReadExerciseReference(reader, "exerciseId", context.Sink, context.ExerciseIds);
            var status = reader.RequiredEnum<EntryStatus>("status");
            var skipReason = reader.OptionalString("skipReason");
            var swappedFrom = ReadSwappedFrom(reader, context, status);
            var rpe = reader.OptionalIntInRange("rpe", 1, 10);
            var sets = ReadSets(reader, context);
            reader.WarnAboutUnknownFields();

            if (skipReason is not null && status != EntryStatus.Skipped)
            {
                context.Sink.Error(reader.ChildPath("skipReason"), "invalid_value", $"{reader.ChildPath("skipReason")} only applies to a skipped entry.");
            }

            if (status == EntryStatus.Skipped && sets.Count > 0)
            {
                context.Sink.Error(reader.ChildPath("sets"), "invalid_value", $"{reader.ChildPath("sets")} must be empty: a skipped Exercise has no Sets.");
            }

            if (context.Sink.Errors.Count == errorsBefore)
            {
                entries.Add(new WorkoutEntry(exerciseId, status, skipReason, swappedFrom, rpe, sets));
            }
        }

        return entries;
    }

    /// <summary>A swapped entry records the planned Exercise it replaced; no other status may have one.</summary>
    private static string? ReadSwappedFrom(FieldReader entry, Context context, EntryStatus status)
    {
        const string field = "swappedFromExerciseId";
        if (status == EntryStatus.Swapped)
        {
            return SessionRowReader.ReadExerciseReference(entry, field, context.Sink, context.ExerciseIds);
        }

        var value = entry.OptionalString(field);
        if (value is not null)
        {
            context.Sink.Error(entry.ChildPath(field), "invalid_value", $"{entry.ChildPath(field)} only applies to a swapped entry.");
        }

        return null;
    }

    private static List<WorkoutSet> ReadSets(FieldReader entry, Context context)
    {
        var sets = new List<WorkoutSet>();
        foreach (var (reader, _) in entry.OptionalObjectArray("sets") ?? [])
        {
            var errorsBefore = context.Sink.Errors.Count;
            var id = reader.RequiredId("id");
            var weightKg = reader.OptionalNonNegativeDouble("weightKg");
            var reps = reader.OptionalInt("reps", min: 1);
            var distanceKm = reader.OptionalPositiveDouble("distanceKm");
            var duration = reader.OptionalInt("durationSeconds", min: 1);
            var distanceM = reader.OptionalPositiveDouble("distanceM");
            var isMax = reader.OptionalBool("isMax", defaultValue: false);
            reader.WarnAboutUnknownFields();

            context.CheckUnique(context.SetIds, reader, "Set", id);
            if (distanceKm is not null && distanceM is not null)
            {
                context.Sink.Error(reader.ChildPath("distanceM"), "invalid_value",
                    $"{reader.ChildPath("distanceM")} can't be used together with {reader.ChildPath("distanceKm")}. Use km for cardio or metres for a carry, not both.");
            }

            if (context.Sink.Errors.Count == errorsBefore)
            {
                sets.Add(new WorkoutSet(id, weightKg, reps, distanceKm, duration, distanceM, isMax));
            }
        }

        return sets;
    }

    private static PainCheck? ReadPainCheck(FieldReader workout, Context context)
    {
        var reader = workout.OptionalObject("painCheck");
        if (reader is null)
        {
            return null;
        }

        var errorsBefore = context.Sink.Errors.Count;
        var id = reader.RequiredId("id");
        var status = reader.RequiredEnum<PainStatus>("status");
        var areas = reader.OptionalEnumList<BodyArea>("areas");
        var note = reader.OptionalString("note");
        var exerciseId = reader.OptionalString("exerciseId");
        if (exerciseId is not null)
        {
            SessionRowReader.CheckExerciseKnown(reader, "exerciseId", exerciseId, context.Sink, context.ExerciseIds);
        }

        PainSeverity? severity;
        if (status == PainStatus.Reported)
        {
            severity = reader.RequiredEnum<PainSeverity>("severity");
            if (areas.Count == 0 && context.Sink.Errors.Count == errorsBefore)
            {
                context.Sink.Error(reader.ChildPath("areas"), "missing_field", $"{reader.ChildPath("areas")} must name at least one body area when pain is reported.");
            }
        }
        else
        {
            severity = reader.OptionalEnum<PainSeverity>("severity");
            if (areas.Count > 0 || severity is not null)
            {
                var path = reader.ChildPath(areas.Count > 0 ? "areas" : "severity");
                context.Sink.Error(path, "invalid_value", $"{path} can't be given when status is \"none\". Use status \"reported\" to record pain.");
            }
        }

        reader.WarnAboutUnknownFields();
        context.CheckUnique(context.PainCheckIds, reader, "Pain check", id);
        return context.Sink.Errors.Count == errorsBefore ? new PainCheck(id, status, areas, severity, note, exerciseId) : null;
    }

    /// <summary>Shared state for one read: where errors go, what references are checked against, and IDs seen so far.</summary>
    private sealed class Context(IssueSink sink, HashSet<string>? exerciseIds, HashSet<string>? sessionIds)
    {
        public IssueSink Sink { get; } = sink;

        /// <summary>Null when the document has no Exercise library, which switches Exercise checking off.</summary>
        public HashSet<string>? ExerciseIds { get; } = exerciseIds;

        /// <summary>Null when the document has no Plans, which switches Session checking off.</summary>
        public HashSet<string>? SessionIds { get; } = sessionIds;

        public HashSet<string> WorkoutIds { get; } = [];

        public HashSet<string> SetIds { get; } = [];

        public HashSet<string> PainCheckIds { get; } = [];

        public void CheckUnique(HashSet<string> seen, FieldReader reader, string kind, string id)
        {
            if (id.Length > 0 && !seen.Add(id))
            {
                var path = reader.ChildPath("id");
                Sink.Error(path, "duplicate_id", $"{path} \"{id}\" is used by more than one {kind}. Every {kind} needs its own ID.");
            }
        }
    }
}
