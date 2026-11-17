using Core.Serialisation;

namespace Core.Storage;

/// <summary>What reading the store produced: the startup facts and the dataset to run on (empty unless the data could be read).</summary>
internal sealed record LoadOutcome(StartupResult Startup, DataSnapshot Snapshot);

/// <summary>
/// Reads stored records through the exchange format's own reader, setting aside (quarantining) the records the reader or validator names
/// until what is left reads cleanly. A set-aside record stays in storage untouched and is reported. Setting one aside can leave another
/// dangling (a Workout naming a removed Exercise), so reading repeats. Errors that name no record cannot be set aside: the data as a
/// whole cannot be read.
/// </summary>
internal sealed class StartupReader(StoredData data, int currentVersion)
{
    private readonly List<StoredRecord> _remaining = [.. data.Records];
    private readonly List<LoadIssue> _issues = [];

    public LoadOutcome Read()
    {
        var version = data.Metadata.RecordFormatVersion ?? currentVersion;
        while (true)
        {
            var assembled = StoredRecords.Assemble(_remaining, version);
            SetAside(assembled.Malformed.Select(record => (record, "This record is not valid JSON.")));

            var result = ExchangeFormat.Read(assembled.Json);
            if (result is { IsSuccess: true, Document: { } document })
            {
                return Succeeded(DataSnapshot.From(document));
            }

            var named = NamedErrors(result, assembled);
            if (named.Count == 0)
            {
                _issues.AddRange(result.Errors.Select(error => new LoadIssue(null, null, error.Message)));
                return new LoadOutcome(new StartupResult(StartupState.CannotRead, _issues), DataSnapshot.Empty);
            }

            SetAside(named.Select(pair => (pair.Key, string.Join(" ", pair.Value.Distinct()))));
        }
    }

    /// <summary>The reader's error messages, grouped by the stored record each one points inside. Errors naming no record are left out.</summary>
    private static Dictionary<StoredRecord, List<string>> NamedErrors(ReadResult result, StoredRecords.AssembledDocument assembled)
    {
        var named = new Dictionary<StoredRecord, List<string>>();
        foreach (var error in result.Errors)
        {
            if (assembled.RecordAt(error.Path) is not { } record)
            {
                continue;
            }

            if (!named.TryGetValue(record, out var messages))
            {
                named[record] = messages = [];
            }

            messages.Add(error.Message);
        }

        return named;
    }

    private LoadOutcome Succeeded(DataSnapshot snapshot)
    {
        var firstRun = data.Records.Count == 0 && !data.Metadata.Initialised;
        return new LoadOutcome(new StartupResult(firstRun ? StartupState.FirstRun : StartupState.Ready, _issues), snapshot);
    }

    private void SetAside(IEnumerable<(StoredRecord Record, string Message)> found)
    {
        foreach (var (record, message) in found.ToList())
        {
            _remaining.Remove(record);
            _issues.Add(new LoadIssue(record.Kind, record.Key, message));
        }
    }
}
