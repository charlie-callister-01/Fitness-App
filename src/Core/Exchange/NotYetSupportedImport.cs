using Core.Storage;

namespace Core.Exchange;

/// <summary>Stand-in for a kind that is not built yet: one blocker, no changes. Replaced by the real rules for Merge and Restore.</summary>
internal sealed class NotYetSupportedImport(ImportKind kind) : IImportKind
{
    public string Name => $"A {kind} import";

    public IReadOnlySet<string> Reads { get; } = Enum.GetValues<RecordKind>().Select(k => k.WireName()).ToHashSet();

    public KindResult Build(ImportContext context)
    {
        var text = $"{kind} import is not supported yet.";
        return new KindResult([], [new ImportItem(ImportItemAction.Blocker, null, kind.ToString(), text)], [text]);
    }
}
