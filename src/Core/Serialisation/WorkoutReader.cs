using Core.Model;

namespace Core.Serialisation;

/// <summary>
/// Reads and validates the <c>workouts</c> collection of a version 1 document: Workouts, their planned snapshot, entries, Sets and Pain check.
/// Structure only: references to Exercises and Sessions, and unique IDs, are checked by <see cref="DocumentValidator"/>.
/// </summary>
internal static class WorkoutReader
{
    public static List<Workout>? Read(FieldReader root) => root.ReadRecords("workouts", ReadWorkout);

    private static Workout ReadWorkout(FieldReader reader)
    {
        var id = reader.RequiredId("id");
        var date = reader.RequiredDate("date");
        var sessionId = reader.OptionalId("sessionId");
        var loggedAfterwards = reader.OptionalBool("loggedAfterwards", defaultValue: false);
        var startedAt = reader.OptionalTimestamp("startedAt");
        var endedAt = reader.OptionalTimestamp("endedAt");
        reader.CheckTimestampOrder("startedAt", "endedAt", startedAt, endedAt);

        var planned = SessionRowReader.ReadRows(reader, "planned");
        var entries = ReadEntries(reader);
        var painCheck = ReadPainCheck(reader);

        return new Workout(id, date, sessionId, loggedAfterwards, startedAt, endedAt, planned, entries, painCheck);
    }

    private static List<WorkoutEntry> ReadEntries(FieldReader workout) => workout.ReadRecords("entries", ReadEntry) ?? [];

    private static WorkoutEntry ReadEntry(FieldReader reader)
    {
        var exerciseId = reader.RequiredId("exerciseId");
        var status = reader.RequiredEnum<EntryStatus>("status");
        var skipReason = reader.OptionalString("skipReason");
        var swappedFrom = ReadSwappedFrom(reader, status);
        var rpe = reader.OptionalIntInRange("rpe", 1, 10);
        var sets = ReadSets(reader);

        if (skipReason is not null && status != EntryStatus.Skipped)
        {
            reader.Report("skipReason", "invalid_value", $"{reader.ChildPath("skipReason")} only applies to a skipped entry.");
        }

        if (status == EntryStatus.Skipped && sets.Count > 0)
        {
            reader.Report("sets", "invalid_value", $"{reader.ChildPath("sets")} must be empty: a skipped Exercise has no Sets.");
        }

        return new WorkoutEntry(exerciseId, status, skipReason, swappedFrom, rpe, sets);
    }

    /// <summary>A swapped entry records the planned Exercise it replaced; no other status may have one.</summary>
    private static string? ReadSwappedFrom(FieldReader entry, EntryStatus status)
    {
        const string field = "swappedFromExerciseId";
        if (status == EntryStatus.Swapped)
        {
            return entry.RequiredId(field);
        }

        if (entry.OptionalString(field) is not null)
        {
            entry.Report(field, "invalid_value", $"{entry.ChildPath(field)} only applies to a swapped entry.");
        }

        return null;
    }

    private static List<WorkoutSet> ReadSets(FieldReader entry) => entry.ReadRecords("sets", ReadSet) ?? [];

    private static WorkoutSet ReadSet(FieldReader reader)
    {
        var id = reader.RequiredId("id");
        var weightKg = reader.OptionalNonNegativeDouble("weightKg");
        var reps = reader.OptionalInt("reps", min: 1);
        var distanceKm = reader.OptionalPositiveDouble("distanceKm");
        var duration = reader.OptionalInt("durationSeconds", min: 1);
        var distanceM = reader.OptionalPositiveDouble("distanceM");
        var isMax = reader.OptionalBool("isMax", defaultValue: false);

        if (distanceKm is not null && distanceM is not null)
        {
            reader.Report("distanceM", "invalid_value",
                $"{reader.ChildPath("distanceM")} can't be used together with {reader.ChildPath("distanceKm")}. Use km for cardio or metres for a carry, not both.");
        }

        return new WorkoutSet(id, weightKg, reps, distanceKm, duration, distanceM, isMax);
    }

    private static PainCheck? ReadPainCheck(FieldReader workout) =>
        workout.OptionalObject("painCheck")?.ReadRecord(reader =>
        {
            var fields = reader.Attempt(() =>
            {
                var id = reader.RequiredId("id");
                var status = reader.RequiredEnum<PainStatus>("status");
                var areas = reader.OptionalEnumList<BodyArea>("areas");
                var note = reader.OptionalString("note");
                var exerciseId = reader.OptionalId("exerciseId");
                var severity = status == PainStatus.Reported
                    ? reader.RequiredEnum<PainSeverity>("severity")
                    : reader.OptionalEnum<PainSeverity>("severity");
                return new PainCheck(id, status, areas, severity, note, exerciseId);
            });

            if (fields.IsValid)
            {
                PainCheckRules.Check(reader, fields.Value);
            }

            return fields.Value;
        });
}
