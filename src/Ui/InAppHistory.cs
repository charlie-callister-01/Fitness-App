using Microsoft.AspNetCore.Components;

namespace Ui;

/// <summary>
/// Whether this session has navigated inside the app, i.e. whether the browser's Back would stay in the app. After a deep link
/// (or a refresh on a pushed page) it would not, so the pushed pages' back arrow goes to the app root instead. Create it at startup
/// (the host resolves it once) so it sees every navigation.
/// </summary>
public sealed class InAppHistory : IDisposable
{
    private readonly NavigationManager _navigation;

    public InAppHistory(NavigationManager navigation)
    {
        _navigation = navigation;
        _navigation.LocationChanged += OnLocationChanged;
    }

    /// <summary>True once any in-app navigation has happened since the app started.</summary>
    public bool HasEarlierPage { get; private set; }

    public void Dispose() => _navigation.LocationChanged -= OnLocationChanged;

    private void OnLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e) => HasEarlierPage = true;
}
