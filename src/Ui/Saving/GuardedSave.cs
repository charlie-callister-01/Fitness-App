using Core.Storage;

namespace Ui.Saving;

/// <summary>How a guarded save ended.</summary>
public enum SaveStatus
{
    /// <summary>The change was committed.</summary>
    Saved,

    /// <summary>The commit was attempted and failed; the failure notice says why.</summary>
    Failed,

    /// <summary>Another save was already running, so this one did nothing.</summary>
    Busy,

    /// <summary>This window cannot write, so nothing was attempted.</summary>
    ReadOnly,

    /// <summary>The record to change is no longer there; the failure notice says so.</summary>
    Gone,

    /// <summary>Nothing was committed because a rule refused the change or there was nothing to change.</summary>
    NothingToSave,
}

/// <summary>What a guarded save came to. <see cref="Commit"/> is the commit's result when one was attempted.</summary>
public sealed record SaveResult(SaveStatus Status, CommitResult? Commit = null)
{
    public bool IsSaved => Status == SaveStatus.Saved;
}

/// <summary>What the work run under the guard did, so the guard can report it: a commit, a record that has disappeared, or nothing.</summary>
public abstract record SaveWork
{
    /// <summary>The work committed a change; this is the commit's result.</summary>
    public static SaveWork Committed(CommitResult result) => new CommitWork(result);

    /// <summary>The record to change is gone: <paramref name="message"/> goes to the failure notice and nothing is saved.</summary>
    public static SaveWork Gone(string message) => new GoneWork(message);

    /// <summary>No commit was needed or allowed (a rule refused, or there was nothing to change).</summary>
    public static SaveWork None { get; } = new NoWork();

    internal sealed record CommitWork(CommitResult Result) : SaveWork;

    internal sealed record GoneWork(string Message) : SaveWork;

    internal sealed record NoWork : SaveWork;
}

/// <summary>
/// The one way a screen saves. A second request while one is saving does nothing (<see cref="SaveStatus.Busy"/>); a window that cannot
/// write refuses (<see cref="SaveStatus.ReadOnly"/>); the work commits; a record that has disappeared is reported as "no longer there";
/// and the outcome of the commit goes to the <see cref="SaveFailureNotice"/>. Callers read the <see cref="SaveResult"/> and keep only
/// their own screen state.
/// </summary>
public sealed class GuardedSave(AppData data, SaveFailureNotice notice)
{
    /// <summary>A save is running.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>Whether a tap should save: nothing is saving and this window can write.</summary>
    public bool CanAct => !IsBusy && data.Health.CanWrite;

    /// <summary>Raised when a save starts (after <see cref="IsBusy"/> is set, before the work runs), so a screen can show it as saving.</summary>
    public event Action? Started;

    /// <summary>Commits <paramref name="changes"/> under the guard.</summary>
    public Task<SaveResult> CommitAsync(IReadOnlyList<Change> changes, bool requireWrite = true) =>
        RunAsync(async () => SaveWork.Committed(await data.CommitAsync(changes)), requireWrite);

    /// <summary>Commits one change under the guard.</summary>
    public Task<SaveResult> CommitAsync(Change change, bool requireWrite = true) => CommitAsync([change], requireWrite);

    /// <summary>
    /// Runs <paramref name="work"/> under the guard. <paramref name="requireWrite"/> is false only for a live control that has no
    /// disabled state (the Theme choice): the commit's own failure is then what the notice reports.
    /// </summary>
    public async Task<SaveResult> RunAsync(Func<Task<SaveWork>> work, bool requireWrite = true)
    {
        if (IsBusy)
        {
            return new SaveResult(SaveStatus.Busy);
        }

        if (requireWrite && !data.Health.CanWrite)
        {
            return new SaveResult(SaveStatus.ReadOnly);
        }

        IsBusy = true;
        Started?.Invoke();
        try
        {
            switch (await work())
            {
                case SaveWork.CommitWork commit:
                    notice.Report(commit.Result);
                    return new SaveResult(commit.Result is CommitResult.Committed ? SaveStatus.Saved : SaveStatus.Failed, commit.Result);
                case SaveWork.GoneWork gone:
                    notice.ReportMessage(gone.Message);
                    return new SaveResult(SaveStatus.Gone);
                default:
                    return new SaveResult(SaveStatus.NothingToSave);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
