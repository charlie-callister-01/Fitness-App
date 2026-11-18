using Bunit;
using Core.Ids;
using Core.Model;
using Core.Rules.Starting;
using Core.Storage;
using Core.Time;
using Microsoft.Extensions.DependencyInjection;
using Ui.Saving;

namespace Ui.Tests.Pages;

/// <summary>Shared seed data and wiring for the page tests that run over a real <see cref="AppData"/> on the in-memory store.</summary>
internal static class LogFixture
{
    /// <summary>A Monday, so the Plan's Monday Session is due today.</summary>
    public static readonly DateTimeOffset Now = new(2026, 11, 16, 9, 0, 0, TimeSpan.Zero);

    public static readonly DateOnly Today = new(2026, 11, 16);

    public const string Squat = "squat";
    public const string Lunge = "lunge";

    public sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    public sealed class CountingIds : IIdGenerator
    {
        private int _next;

        public string NewId() => $"id-{++_next}";
    }

    public static IReadOnlyList<Exercise> Exercises { get; } =
    [
        new(Squat, "Goblet squat", LogKind.Load, null, false),
        new(Lunge, "Walking lunge", LogKind.Load, null, false),
        new("plank", "Plank", LogKind.Seconds, null, false),
        new("journal", "Journal", LogKind.Text, null, false),
        new("pullup", "Pull-up", LogKind.BodyweightLoad, null, false),
    ];

    /// <summary>The active Plan: "Lower" on Mondays with Goblet squat 3 x 8 and Walking lunge 2 x 10.</summary>
    public static Plan Plan(IReadOnlyList<Session>? sessions = null, bool active = true) =>
        new("plan", "Plan", active, Today, null, null, 3, 2, false, 2, [],
            sessions ??
            [
                new("lower", "Lower", DayOfWeek.Monday, SessionType.Strength, null,
                [
                    new SessionExercise(Squat, 3, 8, null, "Chest up"),
                    new SessionExercise(Lunge, 2, 10, null, null),
                ]),
            ]);

    public static async Task<AppData> OpenSeededAsync(
        IDataStore? store = null, Plan? plan = null, params Workout[] workouts)
    {
        store ??= new InMemoryDataStore();
        var data = await AppData.OpenAsync(store);
        var changes = Exercises.Select(Change.Put).ToList();
        if (plan is not null)
        {
            changes.Add(Change.Put(plan));
        }

        changes.AddRange(workouts.Select(Change.Put));
        var result = await data.CommitAsync(changes);
        Assert.IsType<CommitResult.Committed>(result);
        return data;
    }

    /// <summary>The Workout Start would make for the Plan's "Lower" Session today.</summary>
    public static Workout InProgress(string id = "w-now", params WorkoutEntry[] entries)
    {
        var plan = Plan();
        return WorkoutStart.Create(plan, plan.Sessions[0], Today, new CountingIds(), new FixedClock(Now)) with
        {
            Id = id,
            Entries = entries,
        };
    }

    /// <summary>A finished Workout from earlier, with the given sets of Goblet squat.</summary>
    public static Workout Finished(string id, DateOnly date, params WorkoutSet[] squatSets) =>
        new(id, date, "lower", false, Now.AddDays(-7), Now.AddDays(-7).AddHours(1), [],
            [new WorkoutEntry(Squat, EntryStatus.Done, null, null, null, squatSets)], null);

    public static WorkoutSet Set(string id, double kg, int reps) => new(id, kg, reps, null, null, null, false);

    public static void Wire(BunitContext context, AppData data, SaveFailureNotice? notice = null)
    {
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddSingleton(data);
        context.Services.AddSingleton<IClock>(new FixedClock(Now));
        context.Services.AddSingleton<IIdGenerator>(new CountingIds());
        context.Services.AddSingleton(notice ?? new SaveFailureNotice());
        context.Services.AddSingleton<InAppHistory>();
    }
}
