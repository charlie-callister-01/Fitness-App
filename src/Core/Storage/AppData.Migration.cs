using Core.Serialisation;

namespace Core.Storage;

public sealed partial class AppData
{
    /// <summary>
    /// Upgrades stored records written in an older format once, at open: the records were just read through the versioned reader, so they
    /// are written back in the current format by one replace-all that also records the new version. Nothing is written when the recorded
    /// version is current (or absent), or when the data could not be read. A record that was set aside at startup would be lost by a
    /// rewrite, so it makes the upgrade fail rather than run. A failed upgrade leaves the stored data as it was and starts with
    /// <see cref="StartupState.CannotRead"/> (which blocks commits, so versions are never mixed).
    /// </summary>
    private static async Task<(LoadOutcome Outcome, StoreMetadata Metadata)> UpgradeIfOlderAsync(
        IDataStore store, StoredData data, LoadOutcome read, int currentVersion)
    {
        if (data.Metadata.RecordFormatVersion is not { } stored || stored >= currentVersion
            || read.Startup.State is not (StartupState.Ready or StartupState.FirstRun))
        {
            return (read, data.Metadata);
        }

        var upgraded = data.Metadata with { RecordFormatVersion = currentVersion };
        if (read.Startup.Issues.Count == 0
            && await store.ReplaceAllAsync(StoredRecords.FromDocument(read.Snapshot.ToDocument()), upgraded) is StoreResult<Unit>.Success)
        {
            return (read, upgraded);
        }

        var issue = new LoadIssue(null, null,
            "Your data was saved by an older version of the app and could not be upgraded. Nothing has been changed.");
        return (new LoadOutcome(new StartupResult(StartupState.CannotRead, [issue]), DataSnapshot.Empty), data.Metadata);
    }
}
