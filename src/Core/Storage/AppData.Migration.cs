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
    private static async Task<(StartupResult Startup, StoreMetadata Metadata)> UpgradeIfOlderAsync(
        IDataStore store, StoredData data, StartupResult startup, int currentVersion)
    {
        if (data.Metadata.RecordFormatVersion is not { } stored || stored >= currentVersion
            || startup.State is not (StartupState.Ready or StartupState.FirstRun))
        {
            return (startup, data.Metadata);
        }

        var upgraded = data.Metadata with { RecordFormatVersion = currentVersion };
        if (startup.Issues.Count == 0
            && await store.ReplaceAllAsync(StoredRecords.FromDocument(startup.Snapshot.ToDocument()), upgraded) is StoreResult<Unit>.Success)
        {
            return (startup, upgraded);
        }

        var issue = new LoadIssue(null, null,
            "Your data was saved by an older version of the app and could not be upgraded. Nothing has been changed.");
        return (new StartupResult(StartupState.CannotRead, DataSnapshot.Empty, [issue]), data.Metadata);
    }
}
