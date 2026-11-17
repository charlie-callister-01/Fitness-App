using Core.Serialisation;

namespace Core.Storage;

/// <summary>
/// The in-memory model of the whole dataset. <see cref="OpenAsync"/> loads every stored record through the same reader and validator
/// that read a Backup and builds an immutable <see cref="DataSnapshot"/>. A record that cannot be read is left out of the snapshot and
/// reported, never deleted. "Restart" is calling <see cref="OpenAsync"/> again over the same store.
/// </summary>
public sealed class AppData
{
    private AppData(IDataStore store, StartupResult startup)
    {
        Store = store;
        Startup = startup;
    }

    /// <summary>The store this model reads and (from the commit ticket on) writes.</summary>
    internal IDataStore Store { get; }

    /// <summary>What startup found. Never changes after open.</summary>
    public StartupResult Startup { get; }

    /// <summary>The current dataset. Always present, whatever the startup state.</summary>
    public DataSnapshot Snapshot => Startup.Snapshot;

    public static async Task<AppData> OpenAsync(IDataStore store) => new(store, await LoadAsync(store));

    private static async Task<StartupResult> LoadAsync(IDataStore store)
    {
        return await store.LoadAllAsync() switch
        {
            StoreResult<StoredData>.Success success => Read(success.Value),
            StoreResult<StoredData>.Failure failure => new StartupResult(StartupState.StorageFailed, DataSnapshot.Empty, [], failure.Error),
            _ => throw new InvalidOperationException("Unreachable: StoreResult has two cases."),
        };
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
