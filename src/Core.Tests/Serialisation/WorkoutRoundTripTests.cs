using Core.Model;
using Core.Serialisation;

namespace Core.Tests.Serialisation;

public class WorkoutRoundTripTests
{
    private static IReadOnlyList<Workout> ReadBack(ExchangeDocument document)
    {
        var result = ExchangeFormat.Read(ExchangeFormat.Write(document, indented: true));
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors.Select(e => $"{e.Path}: {e.Message}")));
        Assert.Empty(result.Warnings);
        return result.Document!.Workouts!;
    }

    [Fact]
    public void Workout_headers_and_planned_snapshots_survive_write_then_read()
    {
        var original = WorkoutTestData.Rich();

        var workouts = ReadBack(original);

        Assert.Equal(3, workouts.Count);
        for (var i = 0; i < 3; i++)
        {
            var (before, after) = (original.Workouts![i], workouts[i]);
            Assert.Equal((before.Id, before.Date, before.SessionId, before.LoggedAfterwards), (after.Id, after.Date, after.SessionId, after.LoggedAfterwards));
            Assert.Equal((before.StartedAt, before.EndedAt), (after.StartedAt, after.EndedAt));
            Assert.Equal(before.Planned, after.Planned);
        }
    }

    [Fact]
    public void Entry_statuses_ratings_and_sets_survive()
    {
        var workouts = ReadBack(WorkoutTestData.Rich());
        var entries = workouts[0].Entries;

        Assert.Equal((EntryStatus.Done, 8), (entries[0].Status, entries[0].Rpe));
        Assert.Equal((24.5, 10, false), (entries[0].Sets[0].WeightKg, entries[0].Sets[0].Reps, entries[0].Sets[0].IsMax));
        Assert.True(entries[0].Sets[1].IsMax);
        Assert.Equal((EntryStatus.Skipped, "Grip was gone"), (entries[1].Status, entries[1].SkipReason));
        Assert.Equal((EntryStatus.Swapped, "rdl"), (entries[2].Status, entries[2].SwappedFromExerciseId));
        Assert.Equal((5.0, 1530), (entries[2].Sets[0].DistanceKm, entries[2].Sets[0].DurationSeconds));
        Assert.Equal((40.0, 60), (workouts[1].Entries[0].Sets[0].DistanceM, workouts[1].Entries[0].Sets[0].DurationSeconds));
    }

    [Fact]
    public void All_three_pain_check_states_survive()
    {
        var workouts = ReadBack(WorkoutTestData.Rich());

        var reported = workouts[0].PainCheck!;
        Assert.Equal([BodyArea.Knee, BodyArea.LowerBack], reported.Areas);
        Assert.Equal((PainStatus.Reported, PainSeverity.Moderate, "goblet-squat"), (reported.Status, reported.Severity, reported.ExerciseId));
        Assert.Equal(PainStatus.None, workouts[1].PainCheck!.Status);
        Assert.Null(workouts[2].PainCheck);
    }

    [Fact]
    public void Back_dated_and_custom_workouts_keep_their_flags()
    {
        var back = ReadBack(WorkoutTestData.Rich())[1];

        Assert.True(back.LoggedAfterwards);
        Assert.Null(back.SessionId);
        Assert.Null(back.StartedAt);
    }

    [Fact]
    public void Planned_snapshot_is_stored_independently_of_later_plan_edits()
    {
        var json = WorkoutTestData.Broken(r => r["plans"]![0]!["sessions"]![0]!["exercises"]![0]!["sets"] = 9);

        var result = ExchangeFormat.Read(json);

        Assert.Equal(9, result.Document!.Plans![0].Sessions[0].Exercises[0].Sets);
        Assert.Equal(3, result.Document.Workouts![0].Planned[0].Sets);
    }

    [Fact]
    public void Workouts_only_document_is_accepted_without_checking_references()
    {
        var result = ExchangeFormat.Read("""
            {"formatVersion":1,"workouts":[{"id":"w","date":"2026-11-16","sessionId":"x","entries":[{"exerciseId":"e","status":"done","sets":[{"id":"s","reps":5}]}]}]}
            """);

        Assert.True(result.IsSuccess, string.Join("; ", result.Errors.Select(e => $"{e.Path}: {e.Message}")));
    }
}
