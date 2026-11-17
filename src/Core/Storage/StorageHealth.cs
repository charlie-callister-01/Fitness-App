namespace Core.Storage;

/// <summary>
/// The storage facts the UI words: whether the browser promised to keep the data, how full storage is, whether the database is
/// there at all, and whether this window may write. Wording is the UI's job.
/// </summary>
/// <param name="Persistence">Granted, not granted (show the install-and-back-up advice) or unsupported.</param>
/// <param name="Estimate">Usage and quota, or null when the browser cannot say.</param>
/// <param name="DatabaseAvailable">False when the app is running in memory only: nothing will be kept.</param>
/// <param name="CanWrite">False for a read-only window and when startup could not read the data.</param>
/// <param name="AnotherWindowOpen">True when another window holds the writer lock, so this one is read-only.</param>
public sealed record StorageHealth(
    PersistenceStatus Persistence,
    StorageEstimate? Estimate,
    bool DatabaseAvailable,
    bool CanWrite,
    bool AnotherWindowOpen);
