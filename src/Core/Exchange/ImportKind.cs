namespace Core.Exchange;

/// <summary>The four ways an Import can treat a document (see the A4 spec, "Import: the four kinds").</summary>
public enum ImportKind
{
    Restore,
    Plan,
    Merge,
    Guides,
}
