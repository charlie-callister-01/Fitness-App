using Core.Model;

namespace Core.Serialisation;

/// <summary>
/// The state shared by every reader of one document read, built once at the top of the reader and carried by each <see cref="FieldReader"/>:
/// where issues go, and what the parse phase could not read (so the validate phase doesn't judge placeholders).
/// </summary>
internal sealed class ReadContext
{
    public IssueSink Sink { get; } = new();

    public ParseFindings Findings { get; } = new();
}

/// <summary>
/// What the parse phase could not read. A field that fails to read leaves a placeholder in the record; the validator is told here
/// which placeholders to leave alone, so a bad field is reported once (by the parse phase) and never again as a cross-record error.
/// </summary>
internal sealed class ParseFindings
{
    private readonly HashSet<Phase> _phasesWithUnreadableWeeks = new(ReferenceEqualityComparer.Instance);

    /// <summary>Nothing was unreadable: for a document built in memory.</summary>
    public static ParseFindings None { get; } = new();

    /// <summary>True when some Plan's <c>isActive</c> failed to read, so "no active Plan" can't be judged.</summary>
    public bool ActiveFlagUnreadable { get; private set; }

    public void MarkActiveFlagUnreadable() => ActiveFlagUnreadable = true;

    public void MarkWeeksUnreadable(Phase phase) => _phasesWithUnreadableWeeks.Add(phase);

    public bool AreWeeksUnreadable(Phase phase) => _phasesWithUnreadableWeeks.Contains(phase);
}
