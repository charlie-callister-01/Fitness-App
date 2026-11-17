namespace Core.Exchange;

/// <summary>An Export ready to hand to the owner: the text, a suggested file name, plain summary lines and whether it is a Backup.</summary>
public sealed record ExportResult(string Text, string FileName, IReadOnlyList<string> Summary, bool CountsAsBackup);
