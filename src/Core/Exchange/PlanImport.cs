using Core.Model;
using Core.Storage;

namespace Core.Exchange;

/// <summary>
/// Plan: each Plan in the file is added as a new Plan, inactive unless the library has none. Plan, Track, Phase and Session IDs are unique across
/// the whole library, so one that is taken gets the first free <c>id-2</c>, <c>id-3</c>... and the Plan's own Sessions, Track Phase overrides,
/// and any qualified Badge metrics in the file (<c>streak:&lt;id&gt;</c>, <c>days:&lt;id&gt;</c>, <c>bestweek:&lt;id&gt;</c>) follow.
/// Fresh IDs are derived, not random, so the same file always previews the same way, and a Plan this file already added
/// (found by its derived ID, compared as stored JSON apart from <c>isActive</c>) is reported as nothing to change.
/// </summary>
internal sealed class PlanImport : IImportKind
{
    private static readonly string[] TrackMetricPrefixes = ["streak:", "days:", "bestweek:"];

    public string Name => "A Plan import";

    public IReadOnlySet<RecordKind> Reads { get; } = new HashSet<RecordKind> { RecordKind.Plan, RecordKind.Exercise, RecordKind.Badge };

    public KindResult Build(ImportContext context)
    {
        var exercises = ExerciseChanges.Build(context.Matching, includeTagChanges: true);
        var changes = new List<Change>();
        var items = new List<ImportItem>();
        var summary = new List<string>();
        var taken = context.Snapshot.Plans.SelectMany(IdsOf).ToHashSet();
        var trackIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var needsActive = context.Snapshot.Plans.Count == 0;
        foreach (var plan in context.Matching.Document.Plans ?? [])
        {
            var placement = Place(plan, context.Snapshot.Plans, taken);
            foreach (var (oldId, newId) in placement.TrackIds)
            {
                trackIds[oldId] = newId;
            }

            switch (placement)
            {
                case { Existing: { } existing }:
                    items.Add(new ImportItem(ImportItemAction.Keep, RecordKind.Plan, existing.Id, $"Plan \"{existing.Name}\" is already in your library, unchanged."));
                    break;
                case { Added: { } added }:
                    var tracksPhrase = added.Tracks.Count > 1 ? $" with {added.Tracks.Count} Tracks" : "";
                    var text = needsActive
                        ? $"Creates a new Plan called \"{plan.Name}\"{tracksPhrase} and makes it active, because you have no Plan yet."
                        : $"Creates a new Plan called \"{plan.Name}\"{tracksPhrase}, inactive.";
                    taken.UnionWith(IdsOf(added));
                    changes.Add(Change.Put(added with { IsActive = needsActive }));
                    items.Add(new ImportItem(ImportItemAction.Add, RecordKind.Plan, added.Id, text));
                    summary.Add(text);
                    needsActive = false;
                    break;
            }
        }

        var badges = PlaceBadges(context.Matching.Document.Badges, context.Snapshot.Badges, trackIds);
        return exercises.Then(new KindResult(changes, items, summary)).Then(badges).Closed();
    }

    private static KindResult PlaceBadges(
        IReadOnlyList<Badge>? incoming,
        IReadOnlyList<Badge> library,
        Dictionary<string, string> trackIds)
    {
        if (incoming is null or { Count: 0 })
        {
            return new KindResult([], [], []);
        }

        var taken = library.Select(b => b.Id).ToHashSet();
        var changes = new List<Change>();
        var items = new List<ImportItem>();
        var added = 0;
        foreach (var badge in incoming)
        {
            var remapped = badge with { Metric = RemapMetric(badge.Metric, trackIds) };
            for (var n = 1; ; n++)
            {
                var id = n == 1 ? remapped.Id : $"{remapped.Id}-{n}";
                var candidate = remapped with { Id = id };
                if (library.FirstOrDefault(b => b.Id == id) is { } existing)
                {
                    if (StoredJson.Same(candidate, existing))
                    {
                        break;
                    }
                }
                else if (taken.Add(id))
                {
                    changes.Add(Change.Put(candidate));
                    items.Add(new ImportItem(ImportItemAction.Add, RecordKind.Badge, id, $"Adds Badge: {candidate.Name}."));
                    added++;
                    break;
                }
            }
        }

        return new KindResult(changes, items, Wording.AddedReplacedKept(RecordKind.Badge, "Changes", added, 0, 0));
    }

    private static string RemapMetric(string metric, Dictionary<string, string> trackIds)
    {
        var trimmed = metric.Trim();
        foreach (var prefix in TrackMetricPrefixes)
        {
            if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var trackId = trimmed[prefix.Length..];
                if (trackIds.TryGetValue(trackId, out var newTrackId) && !string.Equals(newTrackId, trackId, StringComparison.Ordinal))
                {
                    return $"{trimmed[..prefix.Length]}{newTrackId}";
                }

                return metric;
            }
        }

        return metric;
    }

    private static IEnumerable<string> IdsOf(Plan plan) =>
        plan.Sessions.Select(s => s.Id)
            .Concat(plan.Tracks.Select(t => t.Id))
            .Concat(plan.Tracks.SelectMany(t => t.Phases).Select(p => p.Id))
            .Prepend(plan.Id);

    private sealed record Placement(Plan? Existing, Plan? Added, IReadOnlyDictionary<string, string> TrackIds);

    /// <summary>Walks <c>id</c>, <c>id-2</c>... until the Plan is found in the library as it would be added, or an ID is free for it.</summary>
    private static Placement Place(Plan plan, IReadOnlyList<Plan> library, HashSet<string> taken)
    {
        for (var n = 1; ; n++)
        {
            var id = n == 1 ? plan.Id : $"{plan.Id}-{n}";
            if (library.FirstOrDefault(p => p.Id == id) is { } existing)
            {
                var (rebuilt, trackIds) = Rebuild(plan, id, taken.Except(IdsOf(existing)));
                if (StoredJson.Same(rebuilt with { IsActive = existing.IsActive }, existing))
                {
                    return new Placement(existing, null, trackIds);
                }
            }
            else if (!taken.Contains(id))
            {
                var (rebuilt, trackIds) = Rebuild(plan, id, taken);
                return new Placement(null, rebuilt, trackIds);
            }
        }
    }

    /// <summary>The Plan under <paramref name="id"/>, with its Track, Session and Phase IDs freshened and references following.</summary>
    private static (Plan Plan, Dictionary<string, string> TrackIds) Rebuild(Plan plan, string id, IEnumerable<string> taken)
    {
        var used = new HashSet<string>(taken) { id };
        var trackIds = new Dictionary<string, string>();
        foreach (var track in plan.Tracks)
        {
            trackIds[track.Id] = Fresh(track.Id, used);
        }

        var sessionIds = new Dictionary<string, string>();
        var sessions = plan.Sessions
            .Select(s => s with { Id = sessionIds[s.Id] = Fresh(s.Id, used), TrackId = trackIds.GetValueOrDefault(s.TrackId, s.TrackId) })
            .ToList();
        var tracks = plan.Tracks
            .Select(t => t with
            {
                Id = trackIds[t.Id],
                Phases =
                [
                    .. t.Phases.Select(p => p with
                    {
                        Id = Fresh(p.Id, used),
                        SessionOverrides = [.. p.SessionOverrides.Select(o => o with { SessionId = sessionIds.GetValueOrDefault(o.SessionId, o.SessionId) })],
                    }),
                ],
            })
            .ToList();
        return (plan with { Id = id, Tracks = tracks, Sessions = sessions }, trackIds);
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
