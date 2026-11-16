using Core.Model;

namespace Core.Serialisation;

/// <summary>
/// Reads planned Exercise rows (<see cref="SessionExercise"/>) and Exercise ID references. Shared by Plans (Session rows, Phase overrides)
/// and Workouts (the planned snapshot), so a row means the same thing everywhere.
/// </summary>
internal static class SessionRowReader
{
    /// <param name="exerciseIds">The document's Exercise IDs; null (no library in the document) switches reference checking off.</param>
    public static List<SessionExercise> ReadRows(FieldReader parent, string arrayName, IssueSink sink, HashSet<string>? exerciseIds)
    {
        var rows = new List<SessionExercise>();
        foreach (var (reader, _) in parent.OptionalObjectArray(arrayName) ?? [])
        {
            var errorsBefore = sink.Errors.Count;
            var exerciseId = ReadExerciseReference(reader, "exerciseId", sink, exerciseIds);
            var sets = reader.RequiredInt("sets", min: 1);
            var repsErrorsBefore = sink.Errors.Count;
            var reps = reader.OptionalInt("reps", min: 1);
            var repsAreValid = sink.Errors.Count == repsErrorsBefore;
            var repsMax = ReadRepsMax(reader, sink, reps, repsAreValid);
            var cue = reader.OptionalString("cue");
            reader.WarnAboutUnknownFields();

            if (sink.Errors.Count == errorsBefore)
            {
                rows.Add(new SessionExercise(exerciseId, sets, reps, repsMax, cue));
            }
        }

        return rows;
    }

    /// <summary>Reads a required Exercise ID field and, when there is a library to check against, reports an unknown one.</summary>
    public static string ReadExerciseReference(FieldReader reader, string field, IssueSink sink, HashSet<string>? exerciseIds)
    {
        var id = reader.RequiredId(field);
        CheckExerciseKnown(reader, field, id, sink, exerciseIds);
        return id;
    }

    public static void CheckExerciseKnown(FieldReader reader, string field, string id, IssueSink sink, HashSet<string>? exerciseIds)
    {
        if (id.Length > 0 && exerciseIds is not null && !exerciseIds.Contains(id))
        {
            var path = reader.ChildPath(field);
            sink.Error(path, "unknown_exercise", $"{path} \"{id}\" is not in the Exercise library. Add it to exercises or use an existing ID.");
        }
    }

    private static int? ReadRepsMax(FieldReader row, IssueSink sink, int? reps, bool repsAreValid)
    {
        var repsMax = row.OptionalInt("repsMax", min: reps ?? 1);
        if (repsMax is not null && reps is null && repsAreValid)
        {
            sink.Error(row.ChildPath("repsMax"), "invalid_value",
                $"{row.ChildPath("repsMax")} is the top of a rep range, so {row.ChildPath("reps")} (the bottom of the range) is required too.");
        }

        return repsMax;
    }
}
