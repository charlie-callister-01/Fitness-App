using Core.Serialisation;

namespace Core.Storage;

/// <summary>
/// The in-memory model of the whole dataset. <see cref="OpenAsync(IDataStore, IStorageEnvironment?)"/> loads every stored record
/// through the same reader and validator that read a Backup and builds an immutable <see cref="DataSnapshot"/>. A record that cannot be
/// read is left out of the snapshot and reported, never deleted. "Restart" is calling <c>OpenAsync</c> again over the same store.
/// </summary>
public sealed partial class AppData
{
    /// <summary>What changes together: the dataset, the device metadata and what startup found. Swapped in one assignment.</summary>
    private sealed record State(DataSnapshot Snapshot, StoreMetadata Metadata, StartupResult Startup);

    /// <summary>Commits run one at a time, in the order they asked (the semaphore queues waiters first in, first out).</summary>
    private readonly SemaphoreSlim _writer = new(1, 1);

    private State _state;
    private readonly int _currentVersion;

    private AppData(IDataStore store, State state, EnvironmentFacts environment, int currentVersion)
    {
        _currentVersion = currentVersion;
        Store = store;
        _state = state;
        _environment = environment;
    }

    /// <summary>The store this model reads and writes.</summary>
    internal IDataStore Store { get; }

    /// <summary>What startup found. Changes only when a replace-all succeeds (see <see cref="ReplaceAllAsync"/>).</summary>
    public StartupResult Startup => _state.Startup;

    /// <summary>
    /// The current dataset. Always present, whatever the startup state. A reader that keeps this value keeps a dataset that never changes:
    /// a commit publishes a new snapshot rather than editing this one.
    /// </summary>
    public DataSnapshot Snapshot => _state.Snapshot;

    /// <summary>
    /// Raised once after each successful commit (and after a recorded Backup), once the new snapshot is current, so a handler reading
    /// <see cref="Snapshot"/> sees the change. Never raised for a rejected or failed commit. Handlers run inside the write queue, so a
    /// handler that starts another commit does not deadlock; that commit runs after this one. A handler that throws cannot make the
    /// commit look failed (it has already been saved and published): the exception is traced and the remaining handlers still run.
    /// </summary>
    public event Action? Changed;

    /// <summary>When the last Backup was made or restored on this device; null if never. Device bookkeeping, not part of a Backup.</summary>
    public DateTimeOffset? LastBackupAt => _state.Metadata.LastBackupAt;

    /// <summary>A commit of one change; see <see cref="CommitAsync(IReadOnlyList{Change})"/>.</summary>
    public Task<CommitResult> CommitAsync(Change change) => CommitAsync([change]);

    /// <summary>
    /// Saves the changes as one atomic batch, then publishes the new snapshot and raises <see cref="Changed"/>. If the store fails, the
    /// old snapshot stays, nothing is raised and the failure kind comes back. The first successful commit also marks the store initialised,
    /// in the same unit as the save, and records the record format version (always the current one).
    /// </summary>
    public Task<CommitResult> CommitAsync(IReadOnlyList<Change> changes) =>
        WithWriterAsync<CommitResult>(failure => new CommitResult.StoreFailed(failure), () => CommitWhileWritingAsync(changes));

    private async Task<CommitResult> CommitWhileWritingAsync(IReadOnlyList<Change> changes)
    {
        var state = _state;
        if (state.Startup.State is StartupState.CannotRead or StartupState.StorageFailed)
        {
            return new CommitResult.Blocked(state.Startup.State);
        }

        if (SetAsideRejection(state.Startup, changes) is { } setAside)
        {
            return setAside;
        }

        var candidate = changes.Aggregate(state.Snapshot, (snapshot, change) => change.ApplyTo(snapshot));
        if (ExchangeFormat.Validate(candidate.ToDocument()) is { Count: > 0 } issues)
        {
            return new CommitResult.Rejected(issues);
        }

        var firstSave = !state.Metadata.Initialised;
        var metadata = state.Metadata is { Initialised: true } && state.Metadata.RecordFormatVersion == _currentVersion
            ? null
            : state.Metadata with { Initialised = true, RecordFormatVersion = _currentVersion };
        if (await Store.ApplyAsync([.. changes.Select(change => change.ToStoreChange())], metadata) is StoreResult<Unit>.Failure failure)
        {
            return new CommitResult.StoreFailed(failure.Error);
        }

        _state = state with { Snapshot = candidate, Metadata = metadata ?? state.Metadata };
        if (firstSave)
        {
            await WriteMarkerAsync();
        }

        RaiseChanged();
        return new CommitResult.Committed();
    }

    /// <summary>
    /// Remembers when a Backup was made or restored on this device (the caller supplies the time, from the clock), for the Backup Nudge.
    /// It is device bookkeeping: it is never in the exchange format or a Backup. Other metadata is kept. On failure nothing is remembered.
    /// </summary>
    public Task<StoreResult<Unit>> RecordBackupAsync(DateTimeOffset at) =>
        WithWriterAsync<StoreResult<Unit>>(failure => new StoreResult<Unit>.Failure(failure), async () =>
        {
            var state = _state;
            // The stored metadata is unknown, so writing it could wipe real values.
            if (state.Startup is { State: StartupState.StorageFailed, Failure: { } startupFailure })
            {
                return new StoreResult<Unit>.Failure(startupFailure);
            }

            var metadata = state.Metadata with { LastBackupAt = at };
            var result = await Store.WriteMetadataAsync(metadata);
            if (result is StoreResult<Unit>.Success)
            {
                _state = state with { Metadata = metadata };
                RaiseChanged();
            }

            return result;
        });

    /// <summary>
    /// Runs <paramref name="work"/> as the one writer: refused with <paramref name="lockHeld"/> when another window holds the writer lock,
    /// otherwise queued behind any write already running. Everything <paramref name="work"/> reads from the current state is read only
    /// once it holds the queue, so it never decides on state that a write ahead of it has since changed.
    /// </summary>
    private async Task<TResult> WithWriterAsync<TResult>(Func<StoreFailure, TResult> lockHeld, Func<Task<TResult>> work)
    {
        if (!_environment.HasWriterLock)
        {
            return lockHeld(new StoreFailure(StoreFailureKind.WriterLockHeld));
        }

        await _writer.WaitAsync();
        try
        {
            return await work();
        }
        finally
        {
            _writer.Release();
        }
    }

    /// <summary>
    /// A record set aside at startup stays in storage untouched, so a put or delete of the same kind and key is rejected until a
    /// replace-all (restore) replaces everything.
    /// </summary>
    private static CommitResult.Rejected? SetAsideRejection(StartupResult startup, IReadOnlyList<Change> changes)
    {
        var issues = changes
            .Where(change => startup.Issues.Any(issue => issue.Kind == change.Kind && issue.Key == change.Key))
            .Select(change => new FormatIssue(
                $"{change.Kind}/{change.Key}",
                "record_set_aside",
                "The stored copy of this record could not be read, so it has been left untouched. Restore a Backup or contact the owner to fix it."))
            .ToList();
        return issues.Count == 0 ? null : new CommitResult.Rejected(issues);
    }

    private void RaiseChanged()
    {
        foreach (var handler in Changed?.GetInvocationList() ?? [])
        {
            try
            {
                ((Action)handler)();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"A Changed handler threw after a successful save: {ex}");
            }
        }
    }
}
