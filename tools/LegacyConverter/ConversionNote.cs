using Core.Model;

namespace LegacyConverter;

public enum ConversionNoteKind
{
    Dropped,
    Guessed,
    Warning,
}

/// <summary>An explicit record of anything dropped, guessed, or flagged during conversion.</summary>
public sealed record ConversionNote(
    ConversionNoteKind Kind,
    string Subject,
    string Reason);

/// <summary>The deterministic output of converting a <see cref="LegacyFolder"/>.</summary>
public sealed record ConversionResult(
    ExchangeDocument Document,
    IReadOnlyList<ConversionNote> Notes);
