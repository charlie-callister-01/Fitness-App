namespace Core.Exchange;

/// <summary>The automatic Backup handed to the host, which saves it somewhere (in the browser, a file download).</summary>
public sealed record BackupFile(string Text, string FileName, DateTimeOffset At);
