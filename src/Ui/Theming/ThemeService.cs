using Core.Model;
using Core.Storage;
using Ui.Saving;

namespace Ui.Theming;

/// <summary>
/// Owns the current Theme and Mode. They are read from <see cref="AppData"/>, a choice is committed as a Settings change and applied
/// to the page through <see cref="IThemeInterop"/>. Components read this and never touch the page or storage. It never throws into the
/// UI: if the save fails the choice still applies for this session and the failure goes to the <see cref="SaveFailureNotice"/>.
/// </summary>
/// <remarks>
/// After a failed save the localStorage mirror the page reads at first paint may briefly hold the unsaved choice; Settings
/// win and the next startup reconciles the page and the mirror back to them.
/// </remarks>
public sealed class ThemeService : IDisposable
{
    private readonly AppData _appData;
    private readonly IThemeInterop _interop;
    private readonly SaveFailureNotice _notice;
    private readonly GuardedSave _save;

    // One choice at a time reaches the page and storage, so an older commit can never land after a newer one.
    private readonly SemaphoreSlim _gate = new(1, 1);

    // True while this service's own commit runs, so the change it raises is not mistaken for one made underneath.
    private bool _committing;

    // The Settings values last seen in the snapshot, so a change underneath (a restored Backup) is told from an unrelated commit.
    private (Theme Theme, Mode Mode) _stored;

    public ThemeService(AppData appData, IThemeInterop interop, SaveFailureNotice notice)
    {
        _appData = appData;
        _interop = interop;
        _notice = notice;
        _save = new GuardedSave(appData, notice);
        _stored = StoredChoice();
        (Theme, Mode) = _stored;
        _appData.Changed += OnAppDataChanged;
    }

    public Theme Theme { get; private set; }

    public Mode Mode { get; private set; }

    /// <summary>Raised after the current Theme or Mode changes, whether chosen here or changed underneath.</summary>
    public event Action? Changed;

    /// <summary>Applies the stored choice to the page: reconciles the attributes and the mirror after startup.</summary>
    public Task InitialiseAsync() => ApplyAsync();

    public Task SetThemeAsync(Theme theme) => ChooseAsync(theme, Mode);

    public Task SetModeAsync(Mode mode) => ChooseAsync(Theme, mode);

    public void Dispose() => _appData.Changed -= OnAppDataChanged;

    private async Task ChooseAsync(Theme theme, Mode mode)
    {
        // Held at once, so a second tap while this one is still saving builds on it.
        Theme = theme;
        Mode = mode;

        await _gate.WaitAsync();
        try
        {
            // Applies and saves whatever was chosen last, which may be newer than this call's own choice.
            await ApplyAsync();
            Changed?.Invoke();
            await SaveAsync();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task SaveAsync()
    {
        _committing = true;
        try
        {
            var settings = _appData.Snapshot.Settings with { Theme = Theme, Mode = Mode };

            // The Theme choice has no disabled state, so a read-only window still tries and the commit's own failure is reported.
            var error = await RunAsync(() => _save.CommitAsync(Change.Put(settings), requireWrite: false));
            if (error is not null)
            {
                _notice.Report(new CommitResult.StoreFailed(new StoreFailure(StoreFailureKind.Unexpected, error.Message)));
            }
        }
        finally
        {
            _committing = false;
            _stored = StoredChoice();
        }
    }

    private void OnAppDataChanged()
    {
        if (_committing)
        {
            return;
        }

        var stored = StoredChoice();
        if (stored == _stored)
        {
            return;
        }

        _stored = stored;
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

    // If the look cannot be applied (the script is missing or blocked) the choice is still held, and Settings are unaffected.
    private Task ApplyAsync() => RunAsync(() => _interop.ApplyAsync(Theme, Mode));

    private (Theme Theme, Mode Mode) StoredChoice() => (_appData.Snapshot.Settings.Theme, _appData.Snapshot.Settings.Mode);

    /// <summary>The one error policy: runs the work, never throws into the UI, and hands back what went wrong. Cancellation still escapes.</summary>
    private static async Task<Exception?> RunAsync(Func<Task> work)
    {
        try
        {
            await work();
            return null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return exception;
        }
    }
}
