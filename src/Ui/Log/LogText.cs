using System.Globalization;

namespace Ui.Log;

/// <summary>Display wording for the Workout log that has no state of its own.</summary>
public static class LogText
{
    /// <summary>"Logged afterwards · Sun 15 Nov. No timers."</summary>
    public static string LoggedAfterwardsBanner(DateOnly date) =>
        $"Logged afterwards · {date.ToString("ddd d MMM", CultureInfo.InvariantCulture)}. No timers.";
}
