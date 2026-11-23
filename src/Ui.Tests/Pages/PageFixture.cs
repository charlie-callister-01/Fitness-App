using Bunit;
using Core.Ids;
using Core.Model;
using Core.Rules.Starting;
using Core.Storage;
using Core.Time;
using Microsoft.Extensions.DependencyInjection;
using Ui;
using Ui.Browser;
using Ui.Log;
using Ui.Preferences;
using Ui.Saving;
using Ui.Theming;

namespace Ui.Tests.Pages;

/// <summary>Shared seed data and wiring for the page tests that run over a real <see cref="AppData"/> on the in-memory store.</summary>
internal static class PageFixture
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

    /// <summary>A clock a test moves by hand.</summary>
    public sealed class TestClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; private set; } = now;

        public void Advance(TimeSpan by) => UtcNow += by;
    }

    /// <summary>The rest readout's pacing, fired by hand: <see cref="Tick"/> is "a second passed".</summary>
    public sealed class FakeTicker : ISecondTicker
    {
        private TaskCompletionSource _next = new();

        /// <summary>How many waits are open, so a test can see that a disposed page left none running.</summary>
        public int Waiting { get; private set; }

        public async Task WaitNextAsync(CancellationToken cancellation)
        {
            Waiting++;
            try
            {
                await _next.Task.WaitAsync(cancellation);
            }
            finally
            {
                Waiting--;
            }
        }

        public void Tick()
        {
            var current = _next;
            _next = new TaskCompletionSource();
            current.SetResult();
        }
    }

    public sealed class RecordingWakeLock : IWakeLock
    {
        public List<string> Calls { get; } = [];

        public Task<bool> IsSupportedAsync() => Task.FromResult(true);

        public Task AcquireAsync()
        {
            Calls.Add("acquire");
            return Task.CompletedTask;
        }

        public Task ReleaseAsync()
        {
            Calls.Add("release");
            return Task.CompletedTask;
        }
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

    public const string MainTrack = "plan-main";

    public static Session Session(
        string id,
        string name,
        DayOfWeek day,
        SessionType type = SessionType.Strength,
        int? benchmarkIntervalWeeks = null,
        IReadOnlyList<SessionExercise>? exercises = null,
        string trackId = MainTrack) =>
        new(id, name, trackId, day.ToDefaultDay(), type, benchmarkIntervalWeeks, exercises ?? []);

    public static Plan WithMainPhases(Plan plan, IReadOnlyList<Phase> phases) =>
        plan with { Tracks = [plan.Tracks[0] with { Phases = phases }, .. plan.Tracks.Skip(1)] };

    public static TrackRhythm.Weekly MainWeekly(Plan plan) =>
        Assert.IsType<TrackRhythm.Weekly>(plan.Tracks[0].Rhythm);

    /// <summary>The active Plan: "Lower" on Mondays with Goblet squat 3 x 8 and Walking lunge 2 x 10.</summary>
    public static Plan Plan(IReadOnlyList<Session>? sessions = null, bool active = true) =>
        new("plan", "Plan", active, Today, null, null, 2,
            [new Track(MainTrack, "Main", new TrackRhythm.Weekly(3, 2, false), [])],
            sessions ??
            [
                Session("lower", "Lower", DayOfWeek.Monday, SessionType.Strength, null,
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

    public sealed class FakeThemeInterop : IThemeInterop
    {
        public Task ApplyAsync(Theme theme, Mode mode) => Task.CompletedTask;
    }

    /// <summary>Per-device settings held in memory, so a "reload" is a new component over the same instance.</summary>
    public sealed class FakeDevicePreferences : IDevicePreferences
    {
        public Dictionary<string, string> Values { get; } = [];

        public Task<string?> GetAsync(string key) => Task.FromResult(Values.GetValueOrDefault(key));

        public Task SetAsync(string key, string value)
        {
            Values[key] = value;
            return Task.CompletedTask;
        }
    }

    public sealed class FixedBuildInfo(string identifier = "test-build") : IBuildInfo
    {
        public string Identifier => identifier;
    }

    public sealed record DownloadedFile(string FileName, string Content, string ContentType);

    public sealed record SharedPayload(string Title, string Text, string? FileName, string ContentType);

    /// <summary>Recording fake for <see cref="IBrowserCapabilities"/>.</summary>
    public sealed class FakeBrowserCapabilities : IBrowserCapabilities
    {
        public List<string> CopiedTexts { get; } = [];
        public List<DownloadedFile> Downloads { get; } = [];
        public List<SharedPayload> Shares { get; } = [];
        public int InstallPrompts { get; private set; }

        public bool CopyResult { get; set; } = true;
        public bool DownloadResult { get; set; } = true;
        public bool ShareSupported { get; set; } = true;
        public bool ShareResult { get; set; } = true;
        public bool IsStandalone { get; set; }
        public bool CanInstall { get; set; }
        public bool PromptInstallResult { get; set; } = true;

        public event Action? InstallAvailabilityChanged;

        public Task<bool> CopyTextAsync(string text)
        {
            if (CopyResult)
            {
                CopiedTexts.Add(text);
            }

            return Task.FromResult(CopyResult);
        }

        public Task<bool> DownloadFileAsync(string fileName, string content, string contentType = "application/json")
        {
            if (DownloadResult)
            {
                Downloads.Add(new DownloadedFile(fileName, content, contentType));
            }

            return Task.FromResult(DownloadResult);
        }

        public Task<bool> CanShareAsync() => Task.FromResult(ShareSupported);

        public Task<bool> ShareAsync(string title, string text, string? fileName = null, string contentType = "text/plain")
        {
            if (ShareResult)
            {
                Shares.Add(new SharedPayload(title, text, fileName, contentType));
            }

            return Task.FromResult(ShareResult);
        }

        public Task<bool> IsStandaloneAsync() => Task.FromResult(IsStandalone);

        public Task<bool> CanInstallAsync() => Task.FromResult(CanInstall);

        public Task<bool> PromptInstallAsync()
        {
            InstallPrompts++;
            if (PromptInstallResult)
            {
                CanInstall = false;
                IsStandalone = true;
                InstallAvailabilityChanged?.Invoke();
            }

            return Task.FromResult(PromptInstallResult);
        }

        public void SetCanInstall(bool canInstall)
        {
            CanInstall = canInstall;
            InstallAvailabilityChanged?.Invoke();
        }
    }

    public static void Wire(
        BunitContext context,
        AppData data,
        SaveFailureNotice? notice = null,
        FakeDevicePreferences? preferences = null,
        FakeBrowserCapabilities? browser = null)
    {
        notice ??= new SaveFailureNotice();
        preferences ??= new FakeDevicePreferences();
        browser ??= new FakeBrowserCapabilities();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddSingleton(data);
        var clock = new TestClock(Now);
        context.Services.AddSingleton(clock);
        context.Services.AddSingleton<IClock>(clock);
        context.Services.AddSingleton<FakeTicker>();
        context.Services.AddSingleton<ISecondTicker>(sp => sp.GetRequiredService<FakeTicker>());
        context.Services.AddSingleton<RecordingWakeLock>();
        context.Services.AddSingleton<IWakeLock>(sp => sp.GetRequiredService<RecordingWakeLock>());
        context.Services.AddSingleton<IIdGenerator>(new CountingIds());
        context.Services.AddSingleton<IBuildInfo>(new FixedBuildInfo());
        context.Services.AddSingleton(browser);
        context.Services.AddSingleton<IBrowserCapabilities>(browser);
        context.Services.AddSingleton(notice);
        context.Services.AddSingleton<Announcer>();
        context.Services.AddSingleton<InAppHistory>();
        context.Services.AddSingleton<LogEditStates>();
        context.Services.AddSingleton(new ThemeService(data, new FakeThemeInterop(), notice));
        context.Services.AddSingleton<IDevicePreferences>(preferences);
        context.Services.AddSingleton<RestTimerSetting>();
    }
}
