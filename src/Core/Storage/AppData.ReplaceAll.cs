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
    /// It does not touch <see cref="LastBackupAt"/>: the caller records the restore with <see cref="RecordBackupAsync"/>.
    /// </summary>
    public async Task<CommitResult> ReplaceAllAsync(ExchangeDocument document)
    {
        if (ExchangeFormat.Validate(document) is { Count: > 0 } issues)
        {
            return new CommitResult.Rejected(issues);
        }

        if (WriterLockFailure() is { } lockFailure)
        {
            return new CommitResult.StoreFailed(lockFailure);
        }

        await _writer.WaitAsync();
        try
        {
            var metadata = _metadata with { Initialised = true, RecordFormatVersion = _currentVersion };
            if (await Store.ReplaceAllAsync(StoredRecords.FromDocument(document), metadata) is StoreResult<Unit>.Failure failure)
            {
                return new CommitResult.StoreFailed(failure.Error);
            }

            _snapshot = DataSnapshot.From(document);
            _metadata = metadata;
            Startup = new StartupResult(StartupState.Ready, _snapshot, []);
            await WriteMarkerAsync();
            Changed?.Invoke();
            return new CommitResult.Committed();
        }
        finally
        {
            _writer.Release();
        }
    }
}
