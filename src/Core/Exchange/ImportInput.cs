using System.Text.RegularExpressions;
using Core.Model;
using Core.Serialisation;
using Core.Storage;

namespace Core.Exchange;

/// <summary>A pasted document that was cleaned and read: the document, what to tell the owner, and the Import kind to suggest.</summary>
public sealed record Inspected(
    ExchangeDocument Document,
    ImportKind SuggestedKind,
    IReadOnlyList<string> Notices,
    IReadOnlyList<string> Notes);

/// <summary>Outcome of inspecting a paste: <see cref="Inspected"/> when accepted, otherwise plain-language rejection lines.</summary>
public sealed class InspectResult
{
    private InspectResult(Inspected? inspected, IReadOnlyList<FormatIssue> issues, IReadOnlyList<string> rejections)
    {
        Inspected = inspected;
        Issues = issues;
        Rejections = rejections;
    }

    public Inspected? Inspected { get; }

    /// <summary>Every problem the reader found, uncut.</summary>
    public IReadOnlyList<FormatIssue> Issues { get; }

    /// <summary>What to show: <c>path: message</c> for the first 10 issues, then "and N more".</summary>
    public IReadOnlyList<string> Rejections { get; }

    public bool IsAccepted => Inspected is not null;

    internal static InspectResult Accept(Inspected inspected) => new(inspected, [], []);

    internal static InspectResult Reject(IReadOnlyList<FormatIssue> issues, int maxShown)
    {
        var lines = issues.Take(maxShown).Select(i => $"{i.Path}: {i.Message}").ToList();
        if (issues.Count > maxShown)
        {
            lines.Add($"and {issues.Count - maxShown} more");
        }

        return new InspectResult(null, issues, lines);
    }
}

/// <summary>Step one of an Import: clean what was pasted, read it with the versioned reader, and suggest an Import kind.</summary>
public static partial class ImportInput
{
    private const int MaxIssuesShown = 10;
    private const string AroundTheJsonNotice = "Ignored text around the JSON.";

    [GeneratedRegex(@"```[^\r\n`]*\r?\n(.*?)```", RegexOptions.Singleline)]
    private static partial Regex CodeFence();

    public static InspectResult Inspect(string text)
    {
        var (json, trimmed) = Clean(text);

        var read = ExchangeFormat.Read(json);
        if (!read.IsSuccess)
        {
            return InspectResult.Reject(read.Errors, MaxIssuesShown);
        }

        var document = read.Document!;
        string[] notices = trimmed ? [AroundTheJsonNotice] : [];
        var notes = read.Warnings.Select(w => $"ignored: {w.Path}").ToList();
        return InspectResult.Accept(new Inspected(document, SuggestKind(document), notices, notes));
    }

    /// <summary>Removes a leading BOM, then anything around the JSON. Never guesses beyond that. Reports whether text was cut.</summary>
    private static (string Json, bool Trimmed) Clean(string text)
    {
        var body = text.TrimStart('﻿').Trim();
        if (body.StartsWith('[') || (body.StartsWith('{') && body.EndsWith('}')))
        {
            return (body, false);
        }

        var fence = CodeFence().Match(body);
        if (fence.Success)
        {
            return (fence.Groups[1].Value.Trim(), true);
        }

        var first = body.IndexOf('{');
        var last = body.LastIndexOf('}');
        return first >= 0 && last > first ? (body[first..(last + 1)], true) : (body, false);
    }

    /// <summary>Only Plans and Guides can be a Plan or Guides Import (with Exercises alongside); any other collection makes it a merge.</summary>
    private static ImportKind SuggestKind(ExchangeDocument d)
    {
        var present = DocumentCollections.PresentKinds(d);
        if (DocumentCollections.MissingFromBackup(d).Count == 0)
        {
            return ImportKind.Restore;
        }

        var others = present.Where(k => k is not (RecordKind.Exercise or RecordKind.Plan or RecordKind.Guide));
        if (others.Any())
        {
            return ImportKind.Merge;
        }

        return (present.Contains(RecordKind.Guide), present.Contains(RecordKind.Plan)) switch
        {
            (true, false) => ImportKind.Guides,
            (false, true) => ImportKind.Plan,
            _ => ImportKind.Merge,
        };
    }
}
