namespace Web.BrowserTests.App;

/// <summary>
/// The small hand-written Backup committed with the browser tests (<c>App/fixture-backup.json</c>): one active Plan with a weekly and a daily
/// Track, Sessions across the week, a few Workouts (the second Goblet squat Workout is a PR, one Workout has a Swapped Exercise), Weigh-ins,
/// an unlocked Badge and a Guide. Its dates are fixed, so "this week" on the app's real clock shows Planned Sessions while the history stays
/// in the past. No private data.
/// </summary>
public static class FixtureBackup
{
    public const string FileName = "fixture-backup.json";

    /// <summary>The fixture's Plan, as Home and Plans show it.</summary>
    public const string PlanName = "Test Plan";

    /// <summary>The fixture's Plan id, for its editor route.</summary>
    public const string PlanId = "test-plan";

    /// <summary>The fixture's daily Track, which gives Home a Session on every day.</summary>
    public const string DailyTrackName = "Morning mobility";

    /// <summary>A Session of the Plan, for the Make up route.</summary>
    public const string SessionId = "a-lower";

    /// <summary>The Exercise with a Guide.</summary>
    public const string GuideExerciseId = "goblet-squat";

    /// <summary>The first finished Goblet squat Workout.</summary>
    public const string EarlierWorkoutId = "w-2026-11-09";

    /// <summary>A finished Workout with its Push-up Swapped for a Plank.</summary>
    public const string SwappedWorkoutId = "w-2026-11-13";

    /// <summary>The finished Workout with the PR, a Skipped Exercise and a reported pain.</summary>
    public const string FinishedWorkoutId = "w-2026-11-16";

    /// <summary>The Morning mobility Workout left open, every Exercise done.</summary>
    public const string OpenWorkoutId = "w-2026-11-17";

    public static string Json { get; } = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "App", FileName));
}
