using System.Text;

namespace LegacyConverter;

internal static class ReportWriter
{
    public static string Format(ConversionResult result)
    {
        var doc = result.Document;
        var plan = doc.Plans?.FirstOrDefault();
        var mainPhases = plan?.Tracks.FirstOrDefault()?.Phases.Count ?? 0;

        var sb = new StringBuilder();
        sb.AppendLine("# Legacy Conversion Report");
        sb.AppendLine();
        sb.AppendLine("## Counts");
        sb.AppendLine($"- Exercises: {doc.Exercises?.Count ?? 0}");
        sb.AppendLine($"- Plans: {doc.Plans?.Count ?? 0}");
        sb.AppendLine($"- Tracks: {plan?.Tracks.Count ?? 0}");
        sb.AppendLine($"- Phases (main Track): {mainPhases}");
        sb.AppendLine($"- Sessions: {plan?.Sessions.Count ?? 0}");
        sb.AppendLine($"- Guides: {doc.Guides?.Count ?? 0}");
        sb.AppendLine($"- Badges: {doc.Badges?.Count ?? 0}");
        sb.AppendLine($"- Weigh-ins: {doc.WeighIns?.Count ?? 0}");
        sb.AppendLine($"- Workouts: {doc.Workouts?.Count ?? 0}");
        sb.AppendLine();

        AppendSection(sb, "Dropped", result.Notes.Where(n => n.Kind == ConversionNoteKind.Dropped));
        AppendSection(sb, "Guessed", result.Notes.Where(n => n.Kind == ConversionNoteKind.Guessed));
        AppendSection(sb, "Warnings & Unknown Sections", result.Notes.Where(n => n.Kind == ConversionNoteKind.Warning));

        return sb.ToString();
    }

    private static void AppendSection(StringBuilder sb, string heading, IEnumerable<ConversionNote> notes)
    {
        var list = notes.ToList();
        sb.AppendLine($"## {heading} ({list.Count})");
        if (list.Count == 0)
        {
            sb.AppendLine("- None");
        }
        else
        {
            foreach (var note in list)
            {
                sb.AppendLine($"- **{note.Subject}:** {note.Reason}");
            }
        }

        sb.AppendLine();
    }
}
