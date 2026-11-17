using Core.Serialisation;

namespace Core.Storage;

/// <summary>How a commit ended. A commit that is not <see cref="Committed"/> changed nothing, in memory or in storage.</summary>
public abstract record CommitResult
{
    private CommitResult() { }

    /// <summary>Saved, published and the change event raised.</summary>
    public sealed record Committed : CommitResult;

    /// <summary>The result would break the exchange format's rules; the issues carry the validator's exact paths and codes.</summary>
    public sealed record Rejected(IReadOnlyList<FormatIssue> Issues) : CommitResult;

    /// <summary>The store could not save it (storage full, another window holds the writer lock, and so on).</summary>
    public sealed record StoreFailed(StoreFailure Failure) : CommitResult;

    /// <summary>Startup could not read the stored data (<see cref="StartupState.CannotRead"/> or <see cref="StartupState.StorageFailed"/>), so writing would risk it.</summary>
    public sealed record Blocked(StartupState State) : CommitResult;
}
