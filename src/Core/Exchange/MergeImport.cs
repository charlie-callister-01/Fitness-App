using Core.Model;
using Core.Storage;

namespace Core.Exchange;

/// <summary>
/// Merge: Exercises through the matcher, then Plans, Weigh-ins, Workouts, Breaks, Badges and Week schedules by key. A record the library
/// does not have is added; one that is identical (as stored JSON) is not listed; one that differs replaces the existing one. Never deletes.
/// </summary>
internal sealed class MergeImport : IImportKind
{
    public string Name => "A merge";

    public IReadOnlySet<RecordKind> Reads { get; } = new HashSet<RecordKind>
    {
        RecordKind.Exercise, RecordKind.Plan, RecordKind.WeighIn, RecordKind.Workout, RecordKind.Break, RecordKind.Badge, RecordKind.WeekSchedule,
    };

    public KindResult Build(ImportContext context)
    {
        var document = context.Matching.Document;
        var snapshot = context.Snapshot;
        var result = ExerciseChanges.Build(context.Matching, includeTagChanges: true);
        return result
            .Then(Merge(RecordKind.Plan, document.Plans, snapshot.Plans, p => p.Name, settle: (file, current) => file with { IsActive = current?.IsActive ?? false }))
            .Then(Merge(RecordKind.WeighIn, document.WeighIns, snapshot.WeighIns, w => $"{w.Date:yyyy-MM-dd}"))
            .Then(Merge(RecordKind.Workout, document.Workouts, snapshot.Workouts, w => $"{w.Date:yyyy-MM-dd}", context.Decisions.WorkoutChoice))
            .Then(new KindResult([], SameSessionWarnings(document.Workouts ?? [], snapshot.Workouts), []))
            .Then(Merge(RecordKind.Break, document.Breaks, snapshot.Breaks, b => $"{b.StartDate:yyyy-MM-dd} to {b.EndDate:yyyy-MM-dd}"))
            .Then(Merge(RecordKind.Badge, document.Badges, snapshot.Badges, b => b.Name))
            .Then(Merge(RecordKind.WeekSchedule, document.WeekSchedules, snapshot.WeekSchedules, w => $"week of {w.WeekStart:yyyy-MM-dd}"))
            .Closed();
    }

    /// <summary>A warning for each Workout the merge adds that is on the same date and Session as a Workout the library has under another ID.</summary>
    private static IReadOnlyList<ImportItem> SameSessionWarnings(IReadOnlyList<Workout> incoming, IReadOnlyList<Workout> library)
    {
        var ids = library.Select(w => w.Id).ToHashSet();
        return
        [
            .. from added in incoming.DistinctBy(w => w.Id).Where(w => w.SessionId is not null && !ids.Contains(w.Id))
               from twin in library.Where(w => w.Date == added.Date && w.SessionId == added.SessionId).Take(1)
               select new ImportItem(
                   ImportItemAction.Warning,
                   RecordKind.Workout,
                   added.Id,
                   $"The file's Workout {added.Id} is on {added.Date:yyyy-MM-dd} for the same Session as your Workout {twin.Id}. Both will be kept."),
        ];
    }

    /// <param name="choice">What to do, by key, with a record that differs from the library's. Only Workouts are ever kept.</param>
    /// <param name="settle">The file's record as it would be stored given the library's (null when it is new). Plans use it to keep <c>isActive</c>.</param>
    private static KindResult Merge<T>(
        RecordKind kind,
        IReadOnlyList<T>? incoming,
        IReadOnlyList<T> library,
        Func<T, string> label,
        Func<string, KeepOrReplace>? choice = null,
        Func<T, T?, T>? settle = null)
        where T : class
    {
        choice ??= _ => KeepOrReplace.Replace;
        settle ??= (record, _) => record;
        var existing = library.ToDictionary(RecordKeys.For);
        var changes = new List<Change>();
        var items = new List<ImportItem>();
        var (added, replaced, kept) = (0, 0, 0);
        var name = Wording.Names[kind].Singular;
        foreach (var fileRecord in (incoming ?? []).DistinctBy(RecordKeys.For))
        {
            var key = RecordKeys.For(fileRecord);
            var record = settle(fileRecord, existing.GetValueOrDefault(key));
            if (!existing.TryGetValue(key, out var current))
            {
                changes.Add(Change.Put(record));
                items.Add(new ImportItem(ImportItemAction.Add, kind, key, $"Adds {name}: {label(record)}."));
                added++;
            }
            else if (StoredJson.Same(record, current))
            {
                continue;
            }
            else if (choice(key) == KeepOrReplace.Replace)
            {
                changes.Add(Change.Put(record));
                items.Add(new ImportItem(ImportItemAction.Change, kind, key, $"Replaces {name}: {label(record)}."));
                replaced++;
            }
            else
            {
                items.Add(new ImportItem(ImportItemAction.Keep, kind, key, $"Keeps your {name}: {label(record)}; the file has a different one.", OffersReplace: true));
                kept++;
            }
        }

        return new KindResult(changes, items, Wording.AddedReplacedKept(kind, "Changes", added, replaced, kept));
    }
}
