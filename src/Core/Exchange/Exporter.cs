using Core.Serialisation;
using Core.Storage;

namespace Core.Exchange;

/// <summary>Turns a snapshot and a request into an Export. Pure: no clock, no storage, no file I/O.</summary>
public static class Exporter
{
    /// <param name="snapshot">The data to export from.</param>
    /// <param name="request">What to include.</param>
    /// <param name="exportedFrom">A build string the host passes in; written to the JSON as <c>exportedFrom</c>.</param>
    /// <param name="now">The injected time; written as <c>exportedAt</c> and used for the file name of a Backup.</param>
    public static ExportResult Export(DataSnapshot snapshot, ExportRequest request, string exportedFrom, DateTimeOffset now)
    {
        var document = ExportSlicer.Slice(snapshot, request) with { ExportedAt = now, ExportedFrom = exportedFrom };
        var text = request.Format switch
        {
            ExportFormat.Json => ExchangeFormat.Write(document, indented: true),
            _ => throw new NotSupportedException("Markdown rendering is not built yet (A4 ticket 02)."),
        };

        return new ExportResult(text, ExportSummary.FileName(request, now), ExportSummary.Lines(document, request), request.IsBackup);
    }
}
