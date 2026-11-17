namespace Core.Storage;

/// <summary>How the app came up. Later stages add states here (for example "the browser cleared your data").</summary>
public enum StartupState
{
    /// <summary>The stored data was read; any records that could not be are listed in <see cref="StartupResult.Issues"/>.</summary>
    Ready,

    /// <summary>Nothing has ever been stored. The snapshot is empty with default Settings; nothing was seeded.</summary>
    FirstRun,

    /// <summary>A problem with the data as a whole (for example written by a newer app). The snapshot is empty and nothing was written.</summary>
    CannotRead,

    /// <summary>The store itself could not be read; see <see cref="StartupResult.Failure"/>. The snapshot is empty.</summary>
    StorageFailed,
}

/// <summary>
/// Something found while loading. For a record that was left out of the snapshot, <see cref="Kind"/> and <see cref="Key"/> name it
/// (it stays in storage untouched). For a problem with the whole document, both are null. The message is plain language.
/// </summary>
public sealed record LoadIssue(RecordKind? Kind, string? Key, string Message);

/// <summary>What startup found: the state, the snapshot to run on (never null), and what could not be loaded.</summary>
public sealed record StartupResult(
    StartupState State,
    DataSnapshot Snapshot,
    IReadOnlyList<LoadIssue> Issues,
    StoreFailure? Failure = null);
