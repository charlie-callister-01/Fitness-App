using Core.Model;
using Core.Rules.Guides;

namespace Ui.Pages;

/// <summary>
/// The Guide editor's text boxes as typed: one string per section, with lists as one item per line. Two drafts are equal when every box
/// reads the same, which is how the page knows there are unsaved edits.
/// </summary>
public sealed record GuideDraft(
    string Works,
    string WhyInPlan,
    string Setup,
    string HowTo,
    string Cues,
    string CommonMistakes,
    string Easier,
    string Harder,
    string WatchOuts,
    string VideoUrl)
{
    public static GuideDraft From(Guide? guide)
    {
        var s = GuideEdits.SectionsOf(guide);
        return new(
            s.Works ?? "", s.WhyInPlan ?? "", s.Setup ?? "", s.HowTo ?? "",
            string.Join('\n', s.Cues), string.Join('\n', s.CommonMistakes),
            s.Easier ?? "", s.Harder ?? "", string.Join('\n', s.WatchOuts), s.VideoUrl ?? "");
    }

    public GuideSections ToSections() => new(
        Works, WhyInPlan, Setup, HowTo, Lines(Cues), Lines(CommonMistakes), Easier, Harder, Lines(WatchOuts), VideoUrl);

    private static string[] Lines(string text) => text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
}
