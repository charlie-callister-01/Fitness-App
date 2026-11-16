using Core.Model;

namespace Core.Serialisation;

/// <summary>
/// Reads planned Exercise rows (<see cref="SessionExercise"/>). Shared by Plans (Session rows, Phase overrides)
/// and Workouts (the planned snapshot), so a row means the same thing everywhere.
/// </summary>
internal static class SessionRowReader
{
    public static List<SessionExercise> ReadRows(FieldReader parent, string arrayName) =>
        (parent.OptionalObjectArray(arrayName) ?? [])
            .Select(item => item.Reader.ReadRecord(ReadRow))
            .OfType<SessionExercise>()
            .ToList();

    private static SessionExercise ReadRow(FieldReader reader)
    {
        var exerciseId = reader.RequiredId("exerciseId");
        var sets = reader.RequiredInt("sets", min: 1);
        var reps = reader.OptionalInt("reps", min: 1);
        var repsMax = reader.OptionalInt("repsMax", min: reps ?? 1);
        if (repsMax is not null && reps is null && !reader.Has("reps"))
        {
            reader.Report("repsMax", "invalid_value",
                $"{reader.ChildPath("repsMax")} is the top of a rep range, so {reader.ChildPath("reps")} (the bottom of the range) is required too.");
        }

        return new SessionExercise(exerciseId, sets, reps, repsMax, reader.OptionalString("cue"));
    }
}
