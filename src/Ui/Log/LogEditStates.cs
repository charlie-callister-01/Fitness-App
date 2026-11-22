using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Ui.Log;

/// <summary>
/// Keeps what the log screen holds that is not stored (the Exercise showing, typed text) only for the round trip to the full Guide,
/// so coming back puts the owner on the same Set. One entry per Workout; taking it out hands it back to the log page that asked.
/// Any other navigation drops what was kept, so reopening a Workout later starts at its first incomplete Exercise. Lives for the session only.
/// </summary>
public sealed class LogEditStates : IDisposable
{
    private readonly Dictionary<string, LogEditState> _kept = [];
    private readonly NavigationManager _navigation;

    public LogEditStates(NavigationManager navigation)
    {
        _navigation = navigation;
        _navigation.LocationChanged += OnLocationChanged;
    }

    /// <summary>The state kept for the Workout (and forgets it), or null when nothing was kept.</summary>
    public LogEditState? Take(string workoutId) => _kept.Remove(workoutId, out var state) ? state : null;

    /// <summary>Keeps the state when the owner is now on an Exercise's full Guide; any other place ends the visit and keeps nothing.</summary>
    public void KeepIfOnGuide(string workoutId, LogEditState state)
    {
        if (IsGuide(_navigation.Uri))
        {
            _kept[workoutId] = state;
        }
    }

    public void Dispose() => _navigation.LocationChanged -= OnLocationChanged;

    // Leaving the Guide for anywhere but a log page (Home, say) abandons the round trip.
    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        if (!IsGuide(e.Location) && !IsLog(e.Location))
        {
            _kept.Clear();
        }
    }

    private bool IsGuide(string uri) => PathOf(uri) is var path
        && path.StartsWith("exercises/", StringComparison.Ordinal)
        && path.EndsWith("/guide", StringComparison.Ordinal);

    private bool IsLog(string uri) => PathOf(uri).StartsWith("log/", StringComparison.Ordinal);

    private string PathOf(string uri) => _navigation.ToBaseRelativePath(uri).Split('?', '#')[0];
}
