using Core.Serialisation;
using Core.Storage;

namespace Core.Exchange;

/// <summary>How an Import ended. Anything but <see cref="Applied"/> left the data as it was.</summary>
public abstract record ApplyOutcome
{
    private ApplyOutcome() { }

    /// <summary>The Import was committed. <paramref name="Summary"/> is the preview's plain-language list of what changed.</summary>
    public sealed record Applied(IReadOnlyList<string> Summary) : ApplyOutcome;

    /// <summary>The rules refused the commit; <paramref name="Issues"/> name the fields. The data is unchanged (the Backup date stays stamped).</summary>
    public sealed record Rejected(IReadOnlyList<FormatIssue> Issues) : ApplyOutcome;

    /// <summary>The store failed during the commit. The data is unchanged (the Backup date stays stamped).</summary>
    public sealed record StoreFailed(StoreFailure Failure) : ApplyOutcome;

    /// <summary>Startup could not read the data, so only a Restore can write. Nothing was changed.</summary>
    public sealed record Blocked(StartupState State) : ApplyOutcome;

    /// <summary>The data changed since the preview was built; look at a fresh preview.</summary>
    public sealed record PreviewOutdated : ApplyOutcome;

    /// <summary>The Backup could not be saved, so the Import was stopped.</summary>
    public sealed record BackupNotSaved : ApplyOutcome;

    /// <summary>The preview still has a blocker or an open choice.</summary>
    public sealed record NotReady : ApplyOutcome;
}
