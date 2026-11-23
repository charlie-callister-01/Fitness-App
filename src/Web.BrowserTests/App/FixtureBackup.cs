namespace Web.BrowserTests.App;

/// <summary>
/// The small hand-written Backup committed with the browser tests (<c>App/fixture-backup.json</c>): one active Plan with a weekly and a daily
/// Track, Sessions across the week, a few Workouts (the second Goblet squat Workout is a PR), Weigh-ins, an unlocked Badge and a Guide.
/// Its dates are fixed, so "this week" on the app's real clock shows Planned Sessions while the history stays in the past. No private data.
/// </summary>
public static class FixtureBackup
{
    public const string FileName = "fixture-backup.json";

    /// <summary>The fixture's Plan, as Home and Plans show it.</summary>
    public const string PlanName = "Test Plan";

    /// <summary>The fixture's daily Track, which gives Home a Session on every day.</summary>
    public const string DailyTrackName = "Morning mobility";

    public static string Json { get; } = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "App", FileName));
}
