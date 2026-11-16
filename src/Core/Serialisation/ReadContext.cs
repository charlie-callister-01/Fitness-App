namespace Core.Serialisation;

/// <summary>
/// The state shared by every reader of one document read: where issues go. Built once at the top of the reader and carried by each <see cref="FieldReader"/>.
/// The parse phase is per record, so it holds nothing else; cross-record state lives in <see cref="DocumentValidator"/>.
/// </summary>
internal sealed class ReadContext
{
    public IssueSink Sink { get; } = new();
}
