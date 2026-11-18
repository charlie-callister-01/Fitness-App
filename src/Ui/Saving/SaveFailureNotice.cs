using Core.Model;
using Core.Storage;

namespace Ui.Saving;

/// <summary>
/// The one place a failed save is reported. Any screen hands it the <see cref="CommitResult"/> of a commit; the shared banner in the
/// layouts shows <see cref="Message"/> in plain language, and a successful commit clears it. Failed saves are not reported through
/// <see cref="AppData.Health"/> (that describes the storage, not the last write), so they are kept here.
/// </summary>
public sealed class SaveFailureNotice
{
    /// <summary>What to tell the owner about the last failed save, or null when the last save worked (or nothing was saved).</summary>
    public string? Message { get; private set; }

    /// <summary>Raised after <see cref="Message"/> changes.</summary>
    public event Action? Changed;

    /// <summary>Records the outcome of a commit: a failure sets the message, a success clears it.</summary>
    public void Report(CommitResult result) => ReportMessage(MessageFor(result));

    /// <summary>Records a failure that never reached storage, in plain words; null clears the notice.</summary>
    public void ReportMessage(string? message)
    {
        if (message == Message)
        {
            return;
        }

        Message = message;
        Changed?.Invoke();
    }

    private static string? MessageFor(CommitResult result) => result switch
    {
        CommitResult.Committed => null,
        CommitResult.Rejected rejected => rejected.Issues.Count > 0
            ? rejected.Issues[0].Message
            : "That could not be saved because something in it is not valid.",
        CommitResult.Blocked { State: StartupState.CannotRead } =>
            "Your saved data could not be read, so nothing can be saved. Your data is untouched.",
        CommitResult.Blocked => "Storage could not be opened, so nothing can be saved.",
        CommitResult.StoreFailed { Failure.Kind: StoreFailureKind.WriterLockHeld } =>
            "Another window of the app is open and is the one that saves. Close it and reopen this one to save.",
        CommitResult.StoreFailed { Failure.Kind: StoreFailureKind.StorageFull } =>
            "This device is out of storage space, so that was not saved.",
        CommitResult.StoreFailed { Failure.Kind: StoreFailureKind.StorageUnavailable } =>
            "Storage is not available (private browsing can cause this), so that was not saved.",
        CommitResult.StoreFailed { Failure.Kind: StoreFailureKind.UpgradeBlocked } =>
            "Another window is blocking an update. Close other windows and reopen the app.",
        _ => "That could not be saved. Try again.",
    };
}
