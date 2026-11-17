using Core.Model;
using Core.Storage;

namespace Core.Exchange;

/// <summary>
/// Guides: keyed by the matched Exercise ID. No existing Guide: added. An existing Guide with no content: replaced. An existing Guide
/// with content: kept unless the owner chose to replace it. Replacing swaps all content and never touches the existing personal notes;
/// the file's personal notes are used only when the library has none. A Guide that would not change (compared as stored JSON) is not listed.
/// </summary>
internal sealed class GuidesImport : IImportKind
{
    public string Name => "A Guides import";

    public IReadOnlySet<string> Reads { get; } = new HashSet<string> { "guides", "exercises" };

    public KindResult Build(ImportContext context)
    {
        var exercises = ExerciseChanges.Build(context.Matching, includeTagChanges: false);
        var names = context.Matching.Document.Exercises?.ToDictionary(e => e.Id, e => e.Name) ?? [];
        var existing = context.Snapshot.Guides.ToDictionary(g => g.ExerciseId);
        var changes = new List<Change>();
        var items = new List<ImportItem>();
        var (added, replaced, kept) = (0, 0, 0);
        foreach (var guide in (context.Matching.Document.Guides ?? []).DistinctBy(g => g.ExerciseId))
        {
            var name = names.GetValueOrDefault(guide.ExerciseId, guide.ExerciseId);
            if (!existing.TryGetValue(guide.ExerciseId, out var current))
            {
                changes.Add(Change.Put(guide));
                items.Add(new ImportItem(ImportItemAction.Add, RecordKind.Guide, guide.ExerciseId, $"Adds a Guide for \"{name}\"."));
                added++;
                continue;
            }

            var proposed = guide with { PersonalNotes = HasText(current.PersonalNotes) ? current.PersonalNotes : guide.PersonalNotes };
            if (StoredJson.Same(proposed, current))
            {
                continue;
            }

            if (!HasContent(current) || context.Decisions.GuideChoice(guide.ExerciseId) == KeepOrReplace.Replace)
            {
                changes.Add(Change.Put(proposed));
                items.Add(new ImportItem(ImportItemAction.Change, RecordKind.Guide, guide.ExerciseId, $"Replaces your Guide for \"{name}\". Your personal notes are kept."));
                replaced++;
            }
            else
            {
                items.Add(new ImportItem(ImportItemAction.Keep, RecordKind.Guide, guide.ExerciseId, $"Keeps your Guide for \"{name}\"; the file has a different one.", OffersReplace: true));
                kept++;
            }
        }

        var summary = Wording.AddedReplacedKept(RecordKind.Guide, "Replaces", added, replaced, kept);
        return exercises.Then(new KindResult(changes, items, summary)).ClosedForKindsThatLeaveWorkoutsAlone();
    }

    private static bool HasText(string? text) => !string.IsNullOrWhiteSpace(text);

    /// <summary>True when the Guide has anything but personal notes.</summary>
    internal static bool HasContent(Guide guide) =>
        new[] { guide.Works, guide.WhyInPlan, guide.Setup, guide.HowTo, guide.Easier, guide.Harder, guide.VideoUrl }.Any(HasText)
        || guide.Cues.Count > 0 || guide.CommonMistakes.Count > 0 || guide.WatchOuts.Count > 0;
}
