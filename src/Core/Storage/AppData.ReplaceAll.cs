using Core.Model;
using Core.Serialisation;

namespace Core.Storage;

public sealed partial class AppData
{
    /// <summary>
    /// Replaces the whole dataset with <paramref name="document"/> (for restoring a Backup), all or nothing: everything stored that is not
    /// in the document is removed. The document is validated first and rejected without any change if it breaks the format's rules.
    /// The new snapshot is published only after the store has saved it; on failure the old data and snapshot stay. It works in any
    /// startup state, so a Backup can rescue data that could not be read; on success <see cref="Startup"/> becomes <see cref="StartupState.Ready"/>.
    /// When <paramref name="lastBackupAt"/> is given it becomes <see cref="LastBackupAt"/> in the same atomic store call (so a restore
    /// cannot be saved with a stale Backup date); when null the Backup date is left as it was.
    /// </summary>
    public async Task<CommitResult> ReplaceAllAsync(ExchangeDocument document, DateTimeOffset? lastBackupAt = null)
    {
        if (ExchangeFormat.Validate(document) is { Count: > 0 } issues)
        {
            return new CommitResult.Rejected(issues);
        }

        return await WithWriterAsync<CommitResult>(failure => new CommitResult.StoreFailed(failure), async () =>
        {
            var metadata = _state.Metadata with { Initialised = true, RecordFormatVersion = _currentVersion, LastBackupAt = lastBackupAt ?? _state.Metadata.LastBackupAt };
            if (await Store.ReplaceAllAsync(StoredRecords.FromDocument(document), metadata) is StoreResult<Unit>.Failure failure)
            {
                return new CommitResult.StoreFailed(failure.Error);
            }

            _state = new State(DataSnapshot.From(document), metadata, new StartupResult(StartupState.Ready, []));
            await WriteMarkerAsync();
            RaiseChanged();
            return new CommitResult.Committed();
        });
    }
}
