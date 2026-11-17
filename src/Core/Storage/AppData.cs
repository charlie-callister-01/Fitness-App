using Core.Serialisation;

namespace Core.Storage;

/// <summary>
/// The in-memory model of the whole dataset. <see cref="OpenAsync"/> loads every stored record through the same reader and validator
/// that read a Backup and builds an immutable <see cref="DataSnapshot"/>. A record that cannot be read is left out of the snapshot and
/// reported, never deleted. "Restart" is calling <see cref="OpenAsync"/> again over the same store.
/// </summary>
public sealed partial class AppData
{
    /// <summary>Commits run one at a time, in the order they asked (the semaphore queues waiters first in, first out).</summary>
    private readonly SemaphoreSlim _writer = new(1, 1);

    private DataSnapshot _snapshot;
    private StoreMetadata _metadata;

    private AppData(IDataStore store, StartupResult startup, StoreMetadata metadata)
    {
        Store = store;
        Startup = startup;
        _snapshot = startup.Snapshot;
        _metadata = metadata;
    }

    /// <summary>The store this model reads and writes.</summary>
    internal IDataStore Store { get; }

    /// <summary>What startup found. Never changes after open.</summary>
    public StartupResult Startup { get; }

    /// <summary>
    /// The current dataset. Always present, whatever the startup state. A reader that keeps this value keeps a dataset that never changes:
    /// a commit publishes a new snapshot rather than editing this one.
    /// </summary>
    public DataSnapshot Snapshot => _snapshot;

    /// <summary>
    /// Raised once after each successful commit (and after a recorded Backup), once the new snapshot is current, so a handler reading
    /// <see cref="Snapshot"/> sees the change. Never raised for a rejected or failed commit. Handlers run inside the write queue, so a
    /// handler that starts another commit does not deadlock; that commit runs after this one.
    /// </summary>
    public event Action? Changed;

    /// <summary>When the last Backup was made or restored on this device; null if never. Device bookkeeping, not part of a Backup.</summary>
    public DateTimeOffset? LastBackupAt => _metadata.LastBackupAt;

    /// <summary>Opens over <paramref name="store"/> with a default environment (writable, persistence granted, no marker). Tests and tools; the app passes the real one.</summary>
    public static Task<AppData> OpenAsync(IDataStore store) => OpenAsync(store, new InMemoryStorageEnvironment());

    /// <summary>A commit of one change; see <see cref="CommitAsync(IReadOnlyList{Change})"/>.</summary>
    public Task<CommitResult> CommitAsync(Change change) => CommitAsync([change]);

    /// <summary>
    /// Saves the changes as one atomic batch, then publishes the new snapshot and raises <see cref="Changed"/>. If the store fails, the
    /// old snapshot stays, nothing is raised and the failure kind comes back. The first successful commit also marks the store initialised,
    /// in the same unit as the save.
    /// </summary>
    public async Task<CommitResult> CommitAsync(IReadOnlyList<Change> changes)
    {
        if (Startup.State is StartupState.CannotRead or StartupState.StorageFailed)
        {
            return new CommitResult.Blocked(Startup.State);
        }

        if (WriterLockFailure() is { } lockFailure)
        {
            return new CommitResult.StoreFailed(lockFailure);
        }

        await _writer.WaitAsync();
        try
        {
            var candidate = changes.Aggregate(_snapshot, (snapshot, change) => change.ApplyTo(snapshot));
            if (ExchangeFormat.Validate(candidate.ToDocument()) is { Count: > 0 } issues)
            {
                return new CommitResult.Rejected(issues);
            }

            var firstSave = !_metadata.Initialised;
            var metadata = _metadata.Initialised ? null : _metadata with { Initialised = true };
            if (await Store.ApplyAsync([.. changes.Select(change => change.ToStoreChange())], metadata) is StoreResult<Unit>.Failure failure)
            {
                return new CommitResult.StoreFailed(failure.Error);
            }

            _snapshot = candidate;
            _metadata = metadata ?? _metadata;
            if (firstSave)
            {
                await WriteMarkerAsync();
            }

            Changed?.Invoke();
            return new CommitResult.Committed();
        }
        finally
        {
            _writer.Release();
        }
    }

    /// <summary>
    /// Remembers when a Backup was made or restored on this device (the caller supplies the time, from the clock), for the Backup Nudge.
    /// It is device bookkeeping: it is never in the exchange format or a Backup. Other metadata is kept. On failure nothing is remembered.
    /// </summary>
    public async Task<StoreResult<Unit>> RecordBackupAsync(DateTimeOffset at)
    {
        if (WriterLockFailure() is { } lockFailure)
        {
            return new StoreResult<Unit>.Failure(lockFailure);
        }

        await _writer.WaitAsync();
        try
        {
            var metadata = _metadata with { LastBackupAt = at };
            var result = await Store.WriteMetadataAsync(metadata);
            if (result is StoreResult<Unit>.Success)
            {
                _metadata = metadata;
                Changed?.Invoke();
            }

            return result;
        }
        finally
        {
            _writer.Release();
        }
    }

    /// <summary>
    /// Reads the records, setting aside the ones the reader or validator names, until what is left reads cleanly. Setting one aside can
    /// leave another dangling (a Workout naming a removed Exercise), so this repeats. Errors that name no record cannot be set aside:
    /// the data as a whole cannot be read.
    /// </summary>
    private static StartupResult Read(StoredData data)
    {
        var remaining = data.Records.ToList();
        var issues = new List<LoadIssue>();
        var version = data.Metadata.RecordFormatVersion ?? ExchangeFormat.CurrentVersion;

        while (true)
        {
            var assembled = StoredRecords.Assemble(remaining, version);
            SetAside(remaining, issues, assembled.Malformed.Select(record => (record, "This record is not valid JSON.")));

            var result = ExchangeFormat.Read(assembled.Json);
            if (result.IsSuccess)
            {
                return Succeeded(data, result.Document!, issues);
            }

            var named = result.Errors
                .Select(error => (Record: assembled.RecordAt(error.Path), error.Message))
                .Where(error => error.Record is not null)
                .GroupBy(error => error.Record!)
                .Select(group => (group.Key, string.Join(" ", group.Select(error => error.Message).Distinct())))
                .ToList();
            if (named.Count == 0)
            {
                issues.AddRange(result.Errors.Select(error => new LoadIssue(null, null, error.Message)));
                return new StartupResult(StartupState.CannotRead, DataSnapshot.Empty, issues);
            }

            SetAside(remaining, issues, named);
        }
    }

    private static StartupResult Succeeded(StoredData data, Model.ExchangeDocument document, List<LoadIssue> issues)
    {
        var firstRun = data.Records.Count == 0 && !data.Metadata.Initialised;
        return new StartupResult(firstRun ? StartupState.FirstRun : StartupState.Ready, DataSnapshot.From(document), issues);
    }

    private static void SetAside(List<StoredRecord> remaining, List<LoadIssue> issues, IEnumerable<(StoredRecord Record, string Message)> found)
    {
        foreach (var (record, message) in found.ToList())
        {
            remaining.Remove(record);
            issues.Add(new LoadIssue(record.Kind, record.Key, message));
        }
    }
}
