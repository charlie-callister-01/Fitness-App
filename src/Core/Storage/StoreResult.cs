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

    /// <summary>A storage upgrade is waiting on another open window; the UI says "close other windows and reopen".</summary>
    UpgradeBlocked,

    /// <summary>Anything else; <see cref="StoreFailure.Detail"/> carries the diagnostics.</summary>
    Unexpected,
}

public sealed record StoreFailure(StoreFailureKind Kind, string? Detail = null)
{
    /// <summary>True when the store could not open because another window must be closed first.</summary>
    public bool IsBlockedByAnotherWindow => Kind == StoreFailureKind.UpgradeBlocked;
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
