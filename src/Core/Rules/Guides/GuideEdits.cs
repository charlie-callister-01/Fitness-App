using Core.Model;

namespace Core.Rules.Guides;

/// <summary>The text of a Guide's sections as typed. Lists hold one item per entry; blank text and blank items mean "nothing here".</summary>
public sealed record GuideSections(
    string? Works,
    string? WhyInPlan,
    string? Setup,
    string? HowTo,
    IReadOnlyList<string> Cues,
    IReadOnlyList<string> CommonMistakes,
    string? Easier,
    string? Harder,
    IReadOnlyList<string> WatchOuts,
    string? VideoUrl);

/// <summary>What saving a Guide's sections came to: the Guide to save, or the plain reason it was refused.</summary>
public abstract record GuideEditResult
{
    public sealed record Valid(Guide Guide) : GuideEditResult;

    public sealed record Invalid(string Message) : GuideEditResult;
}

/// <summary>Changes the Guide screen makes to a Guide. Sections and personal notes save independently of each other.</summary>
public static class GuideEdits
{
    /// <summary>A Guide with nothing in it, for an Exercise that has none yet.</summary>
    public static Guide Empty(string exerciseId) => new(exerciseId, null, null, null, null, [], [], null, null, [], null, null);

    /// <summary>The sections of <paramref name="guide"/> as the editor starts from them (all empty when there is no Guide).</summary>
    public static GuideSections SectionsOf(Guide? guide) =>
        guide is null
            ? new(null, null, null, null, [], [], null, null, [], null)
            : new(guide.Works, guide.WhyInPlan, guide.Setup, guide.HowTo, guide.Cues, guide.CommonMistakes, guide.Easier, guide.Harder, guide.WatchOuts, guide.VideoUrl);

    /// <summary>
    /// The Guide with its sections replaced by <paramref name="sections"/>, created when <paramref name="existing"/> is null.
    /// Text is trimmed and an empty section is stored as absent; blank list items are dropped. The personal notes are kept as they are.
    /// The video link must be an http or https address, or empty; anything else is refused and nothing is saved.
    /// </summary>
    public static GuideEditResult WithSections(Guide? existing, string exerciseId, GuideSections sections)
    {
        var video = Clean(sections.VideoUrl);
        if (video is not null && !IsWebAddress(video))
        {
            return new GuideEditResult.Invalid("That video link is not a web address. Use one that starts with http:// or https://.");
        }

        var baseGuide = existing ?? Empty(exerciseId);
        return new GuideEditResult.Valid(baseGuide with
        {
            Works = Clean(sections.Works),
            WhyInPlan = Clean(sections.WhyInPlan),
            Setup = Clean(sections.Setup),
            HowTo = Clean(sections.HowTo),
            Cues = Items(sections.Cues),
            CommonMistakes = Items(sections.CommonMistakes),
            Easier = Clean(sections.Easier),
            Harder = Clean(sections.Harder),
            WatchOuts = Items(sections.WatchOuts),
            VideoUrl = video,
        });
    }

    /// <summary>The Guide with only its personal notes replaced (empty means absent), created when <paramref name="existing"/> is null.</summary>
    public static Guide WithNotes(Guide? existing, string exerciseId, string? notes) =>
        (existing ?? Empty(exerciseId)) with { PersonalNotes = Clean(notes) };

    /// <summary>Whether the text is an absolute http or https address.</summary>
    public static bool IsWebAddress(string? text) =>
        Uri.TryCreate(text?.Trim(), UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" && uri.Host.Length > 0;

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private static IReadOnlyList<string> Items(IReadOnlyList<string> items) =>
        [.. items.Select(i => i.Trim()).Where(i => i.Length > 0)];
}
