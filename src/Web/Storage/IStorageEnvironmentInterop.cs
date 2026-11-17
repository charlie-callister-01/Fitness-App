namespace Web.Storage;

/// <summary>The slice of <c>wwwroot/js/storage-environment.js</c> the environment uses; exists so the mapping can be tested with a fake.</summary>
public interface IStorageEnvironmentInterop
{
    /// <summary>"granted", "notGranted" or "unsupported".</summary>
    Task<string> RequestPersistenceAsync();

    /// <summary>JSON <c>{"usage":n,"quota":n}</c>, or empty when the browser cannot say.</summary>
    Task<string> EstimateAsync();

    Task<bool> TryTakeWriterLockAsync();

    Task<bool> ReadClearedDataMarkerAsync();

    Task WriteClearedDataMarkerAsync();
}
