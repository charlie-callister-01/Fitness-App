using Core.Model;
using Core.Serialisation;
using Core.Storage;
using Core.Tests.Serialisation;

namespace Core.Tests.Exchange;

/// <summary>A whole dataset to export from: the A1 "everything" document plus a Week schedule and Workouts on both sides of any range.</summary>
internal static class ExchangeTestData
{
    public static readonly DateTimeOffset Now = new(2026, 11, 17, 9, 0, 0, TimeSpan.Zero);

    public static DateOnly Today => new(2026, 11, 17);

    public static DataSnapshot Snapshot()
    {
        var document = WholeDatasetTests.Everything();
        return DataSnapshot.From(document with
        {
            Workouts = [.. document.Workouts!, OldWorkout()],
            WeekSchedules =
            [
                new WeekSchedule(new DateOnly(2026, 11, 16), [new SessionMove("a-lower", new DateOnly(2026, 11, 18))]),
                new WeekSchedule(new DateOnly(2025, 1, 6), []),
            ],
        });
    }

    /// <summary>A Workout far outside the default range, with a Pain check and an Exercise nothing else in range uses.</summary>
    public static Workout OldWorkout() => new(
        "w-old", new DateOnly(2025, 1, 6), null, true, null, null, [],
        [new WorkoutEntry("rdl", EntryStatus.Done, null, null, null, [new WorkoutSet("s-old", 60, 5, null, null, null, false)])],
        new PainCheck("p-old", PainStatus.None, [], null, null, null));

    public static ExchangeDocument ReadBack(string json)
    {
        var result = ExchangeFormat.Read(json);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors.Select(e => $"{e.Path}: {e.Message}")));
        return result.Document!;
    }
}
