using Core.Storage;

namespace Core.Exchange;

/// <summary>The Exercise side of an Import: the matcher's additions and tag changes as Changes, preview lines and summary sentences.</summary>
internal static class ExerciseChanges
{
    /// <param name="includeTagChanges">False for a kind that reads Exercises only to match Guides against them.</param>
    public static KindResult Build(ExerciseMatching matching, bool includeTagChanges)
    {
        var changes = new List<Change>();
        var items = new List<ImportItem>();
        var summary = new List<string>();

        foreach (var exercise in matching.ToAdd)
        {
            changes.Add(Change.Put(exercise));
            items.Add(new ImportItem(ImportItemAction.Add, RecordKind.Exercise, exercise.Id, $"Adds the Exercise \"{exercise.Name}\"."));
        }

        if (matching.ToAdd.Count > 0)
        {
            summary.Add($"Adds {Wording.Counted(matching.ToAdd.Count, Wording.Names[RecordKind.Exercise])}.");
        }

        if (includeTagChanges)
        {
            foreach (var exercise in matching.ToUpdate)
            {
                changes.Add(Change.Put(exercise));
                items.Add(new ImportItem(ImportItemAction.Change, RecordKind.Exercise, exercise.Id, $"Changes the tags or benchmark flag on \"{exercise.Name}\"."));
            }

            if (matching.ToUpdate.Count > 0)
            {
                summary.Add($"Changes the tags or benchmark flag on {Wording.Counted(matching.ToUpdate.Count, Wording.Names[RecordKind.Exercise])}.");
            }
        }

        return new KindResult(changes, items, summary);
    }
}
