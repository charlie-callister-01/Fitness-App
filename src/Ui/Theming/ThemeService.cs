using Core.Model;
using Core.Storage;

namespace Ui.Theming;

/// <summary>
/// Owns the current Theme and Mode. They are read from <see cref="AppData"/>, a choice is committed as a Settings change and applied
/// to the page through <see cref="IThemeInterop"/>. Components read this and never touch the page or storage. It never throws into the
/// UI: if the save fails the choice still applies for this session and <see cref="LastSaveFailure"/> says why.
/// </summary>
public sealed class ThemeService : IDisposable
{
    private readonly AppData _appData;
    private readonly IThemeInterop _interop;

    // The Settings values last seen in the snapshot, so a change underneath (a restored Backup) is told from an unrelated commit.
    private (Theme Theme, Mode Mode) _stored;

    public ThemeService(AppData appData, IThemeInterop interop)
    {
        _appData = appData;
        _interop = interop;
        _stored = (appData.Snapshot.Settings.Theme, appData.Snapshot.Settings.Mode);
        Theme = _stored.Theme;
        Mode = _stored.Mode;
        _appData.Changed += OnAppDataChanged;
    }

    public Theme Theme { get; private set; }

    public Mode Mode { get; private set; }

    /// <summary>Why the last choice was not saved, or null when it was (or nothing has been chosen). The choice is applied either way.</summary>
    public CommitResult? LastSaveFailure { get; private set; }

    /// <summary>Raised after the current Theme or Mode changes, whether chosen here or changed underneath.</summary>
    public event Action? Changed;

    /// <summary>Applies the stored choice to the page: reconciles the attributes and the mirror after startup.</summary>
    public Task InitialiseAsync() => ApplyAsync();

    public Task SetThemeAsync(Theme theme) => ChooseAsync(theme, Mode);

    public Task SetModeAsync(Mode mode) => ChooseAsync(Theme, mode);

    public void Dispose() => _appData.Changed -= OnAppDataChanged;

    private async Task ChooseAsync(Theme theme, Mode mode)
    {
        Theme = theme;
        Mode = mode;
        await ApplyAsync();
        Changed?.Invoke();

        try
        {
            var result = await _appData.CommitAsync(Change.Put(_appData.Snapshot.Settings with { Theme = theme, Mode = mode }));
            LastSaveFailure = result is CommitResult.Committed ? null : result;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LastSaveFailure = new CommitResult.StoreFailed(new StoreFailure(StoreFailureKind.Unexpected, exception.Message));
        }
    }

    private void OnAppDataChanged()
    {
        var settings = _appData.Snapshot.Settings;
        if ((settings.Theme, settings.Mode) == _stored)
        {
            return;
        }

        _stored = (settings.Theme, settings.Mode);
        if ((Theme, Mode) == _stored)
        {
            return;
        }

        (Theme, Mode) = _stored;
        _ = ApplyAndRaiseAsync();
    }

    private async Task ApplyAndRaiseAsync()
    {
        await ApplyAsync();
        Changed?.Invoke();
    }

    private async Task ApplyAsync()
    {
        try
        {
            await _interop.ApplyAsync(Theme, Mode);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The look could not be applied (the script is missing or blocked). The choice is still held, and Settings are unaffected.
        }
    }
}
