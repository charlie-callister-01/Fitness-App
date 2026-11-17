using Core.Model;
using Core.Storage;

namespace Core.Exchange;

/// <summary>
/// Plan: each Plan in the file is added as a new Plan, inactive unless the library has none. Plan, Phase and Session IDs are unique across
/// the whole library, so one that is taken gets the first free <c>id-2</c>, <c>id-3</c>... and the Plan's own overrides follow.
/// Fresh IDs are derived, not random, so the same file always previews the same way, and a Plan this file already added
/// (found by its derived ID, compared as stored JSON apart from <c>isActive</c>) is reported as nothing to change.
/// </summary>
internal sealed class PlanImport : IImportKind
{
    public string Name => "A Plan import";

    public IReadOnlySet<string> Reads { get; } = new HashSet<string> { "plans", "exercises" };

    public KindResult Build(ImportContext context)
    {
        var exercises = ExerciseChanges.Build(context.Matching, includeTagChanges: true);
        var changes = new List<Change>();
        var items = new List<ImportItem>();
        var summary = new List<string>();
        var taken = context.Snapshot.Plans.SelectMany(IdsOf).ToHashSet();
        var needsActive = context.Snapshot.Plans.Count == 0;
        foreach (var plan in context.Matching.Document.Plans ?? [])
        {
            switch (Place(plan, context.Snapshot.Plans, taken))
            {
                case { Existing: { } existing }:
                    items.Add(new ImportItem(ImportItemAction.Keep, RecordKind.Plan, existing.Id, $"Plan \"{existing.Name}\" is already in your library, unchanged."));
                    break;
                case { Added: { } added }:
                    var text = needsActive
                        ? $"Creates a new Plan called \"{plan.Name}\" and makes it active, because you have no Plan yet."
                        : $"Creates a new Plan called \"{plan.Name}\", inactive.";
                    taken.UnionWith(IdsOf(added));
                    changes.Add(Change.Put(added with { IsActive = needsActive }));
                    items.Add(new ImportItem(ImportItemAction.Add, RecordKind.Plan, added.Id, text));
                    summary.Add(text);
                    needsActive = false;
                    break;
            }
        }

        return exercises.Then(new KindResult(changes, items, summary)).ClosedForKindsThatLeaveWorkoutsAlone();
    }

    private static IEnumerable<string> IdsOf(Plan plan) =>
        plan.Sessions.Select(s => s.Id).Concat(plan.Phases.Select(p => p.Id)).Prepend(plan.Id);

    private sealed record Placement(Plan? Existing, Plan? Added);

    /// <summary>Walks <c>id</c>, <c>id-2</c>... until the Plan is found in the library as it would be added, or an ID is free for it.</summary>
    private static Placement Place(Plan plan, IReadOnlyList<Plan> library, HashSet<string> taken)
    {
        for (var n = 1; ; n++)
        {
            var id = n == 1 ? plan.Id : $"{plan.Id}-{n}";
            if (library.FirstOrDefault(p => p.Id == id) is { } existing)
            {
                var rebuilt = Rebuild(plan, id, taken.Except(IdsOf(existing)));
                if (StoredJson.Same(rebuilt with { IsActive = existing.IsActive }, existing))
                {
                    return new Placement(existing, null);
                }
            }
            else if (!taken.Contains(id))
            {
                return new Placement(null, Rebuild(plan, id, taken));
            }
        }
    }

    /// <summary>The Plan under <paramref name="id"/>, with its Session and Phase IDs freshened and its overrides following the Sessions.</summary>
    private static Plan Rebuild(Plan plan, string id, IEnumerable<string> taken)
    {
        var used = new HashSet<string>(taken) { id };
        var sessionIds = new Dictionary<string, string>();
        var sessions = plan.Sessions.Select(s => s with { Id = sessionIds[s.Id] = Fresh(s.Id, used) }).ToList();
        var phases = plan.Phases
            .Select(p => p with
            {
                Id = Fresh(p.Id, used),
                SessionOverrides = [.. p.SessionOverrides.Select(o => o with { SessionId = sessionIds.GetValueOrDefault(o.SessionId, o.SessionId) })],
            })
            .ToList();
        return plan with { Id = id, Sessions = sessions, Phases = phases };
    }

    /// <summary>The ID itself when free, otherwise the first free <c>id-2</c>, <c>id-3</c>...; the result is reserved in <paramref name="used"/>.</summary>
    private static string Fresh(string id, HashSet<string> used)
    {
        var candidate = id;
        for (var n = 2; used.Contains(candidate); n++)
        {
            candidate = $"{id}-{n}";
        }

        used.Add(candidate);
        return candidate;
    }
}
