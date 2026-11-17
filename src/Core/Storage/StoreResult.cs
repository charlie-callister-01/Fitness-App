namespace Core.Storage;

/// <summary>The closed set of expected storage failures. Anything else is <see cref="Unexpected"/>.</summary>
public enum StoreFailureKind
{
    /// <summary>The device or browser quota is used up.</summary>
    StorageFull,

    /// <summary>Storage cannot be opened at all (for example private browsing).</summary>
    StorageUnavailable,

    /// <summary>Another tab or window holds the exclusive writer lock.</summary>
    WriterLockHeld,

    /// <summary>Anything else; <see cref="StoreFailure.Detail"/> carries the diagnostics.</summary>
    Unexpected,
}

public sealed record StoreFailure(StoreFailureKind Kind, string? Detail = null)
{
    /// <summary>How a store words "a schema upgrade is waiting on another open window" in <see cref="Detail"/>, as it has no closed kind of its own.</summary>
    public const string BlockedDetailPrefix = "blocked:";

    /// <summary>True when the store could not open because another window must be closed first; the UI says "close other windows and reopen".</summary>
    public bool IsBlockedByAnotherWindow => Detail?.StartsWith(BlockedDetailPrefix, StringComparison.Ordinal) == true;
}

/// <summary>The value returned by operations that have nothing to return.</summary>
public readonly record struct Unit;

/// <summary>An operation's outcome. Expected failures are returned as values, never thrown.</summary>
public abstract record StoreResult<T>
{
    private StoreResult() { }

    public sealed record Success(T Value) : StoreResult<T>;

    public sealed record Failure(StoreFailure Error) : StoreResult<T>;
}
