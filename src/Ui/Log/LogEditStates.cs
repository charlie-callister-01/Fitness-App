namespace Ui.Log;

/// <summary>
/// Keeps what the log screen holds that is not stored (the Exercise showing, typed text) while the owner is on another page, such as the
/// full Guide, so coming back puts them on the same Set. One entry per Workout; taking it out hands it back to the log page that asked.
/// Lives for the session only.
/// </summary>
public sealed class LogEditStates
{
    private readonly Dictionary<string, LogEditState> _kept = [];

    /// <summary>The state kept for the Workout (and forgets it), or null when nothing was kept.</summary>
    public LogEditState? Take(string workoutId) => _kept.Remove(workoutId, out var state) ? state : null;

    public void Keep(string workoutId, LogEditState state) => _kept[workoutId] = state;
}
