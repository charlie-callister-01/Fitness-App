using Core.Model;

namespace Core.Serialisation;

/// <summary>A field-level problem found while reading a document. Path is like <c>exercises[0].name</c>, or <c>$</c> for the whole input.</summary>
public sealed record FormatIssue(string Path, string Code, string Message);

/// <summary>Outcome of reading a document: either a validated document (possibly with warnings) or errors. Never an exception.</summary>
public sealed class ReadResult
{
    private ReadResult(ExchangeDocument? document, IReadOnlyList<FormatIssue> errors, IReadOnlyList<FormatIssue> warnings)
    {
        Document = document;
        Errors = errors;
        Warnings = warnings;
    }

    /// <summary>The validated document; null whenever there are errors.</summary>
    public ExchangeDocument? Document { get; }

    public IReadOnlyList<FormatIssue> Errors { get; }

    public IReadOnlyList<FormatIssue> Warnings { get; }

    public bool IsSuccess => Errors.Count == 0;

    internal static ReadResult Failure(FormatIssue error) => new(null, [error], []);

    internal static ReadResult From(ExchangeDocument document, IssueSink sink) =>
        sink.Errors.Count == 0
            ? new ReadResult(document, [], sink.Warnings)
            : new ReadResult(null, sink.Errors, sink.Warnings);
}

/// <summary>Collects errors and warnings while a document is read.</summary>
internal sealed class IssueSink
{
    public List<FormatIssue> Errors { get; } = [];

    public List<FormatIssue> Warnings { get; } = [];

    public void Error(string path, string code, string message) => Errors.Add(new FormatIssue(path, code, message));

    public void ReportAll(IEnumerable<FormatIssue> issues) => Errors.AddRange(issues);
}
