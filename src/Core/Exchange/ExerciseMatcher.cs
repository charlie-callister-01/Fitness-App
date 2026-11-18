using Core.Model;
using Core.Storage;

namespace Core.Exchange;

/// <summary>
/// Step two of an Import: resolve the file's Exercises against the library (same ID, same name, near-match, new),
/// remap every reference in the file to the resolved IDs, and run the deferred Exercise and Session reference checks.
/// Pure: the same snapshot, file and decisions always give the same answer.
/// </summary>
public static class ExerciseMatcher
{
    public static ExerciseMatching Match(DataSnapshot snapshot, Inspected inspected, ImportDecisions decisions)
    {
        var document = inspected.Document;
        var library = snapshot.Exercises;
        var libraryById = library.ToDictionary(e => e.Id);
        var libraryByName = new Dictionary<string, Exercise>();
        foreach (var e in library)
        {
            libraryByName.TryAdd(ExerciseNames.Normalise(e.Name), e);
        }

        var matches = new List<ExerciseMatch>();
        var idMap = new Dictionary<string, string>();
        var resolved = new List<Exercise>();
        var toAdd = new List<Exercise>();
        var toUpdate = new List<Exercise>();
        var findings = new List<MatchFinding>();
        var file = document.Exercises ?? [];

        for (var i = 0; i < file.Count; i++)
        {
            var incoming = file[i];
            var (outcome, target, candidates) = Resolve(incoming, libraryById, libraryByName, library, decisions);
            matches.Add(new ExerciseMatch(incoming.Id, incoming.Name, outcome, candidates, target?.Id));
            if (outcome != ExerciseOutcome.NeedsDecision)
            {
                idMap[incoming.Id] = target?.Id ?? incoming.Id;
            }

            if (target is null)
            {
                resolved.Add(incoming);
                if (outcome == ExerciseOutcome.New)
                {
                    toAdd.Add(incoming);
                }

                continue;
            }

            var reconciled = Reconcile(incoming, target, $"exercises[{i}]", findings);
            resolved.Add(reconciled);
            if (!SameTags(target, reconciled) || target.IsBenchmark != reconciled.IsBenchmark)
            {
                toUpdate.Add(reconciled);
            }
        }

        var distinct = resolved.DistinctBy(e => e.Id).ToList();

        var rewritten = ExerciseReferences.Rewrite(
            document,
            (_, id) => idMap.GetValueOrDefault(id, id),
            metric => idMap.GetValueOrDefault(metric, metric));
        findings.AddRange(UnknownExercises(document, idMap, file.Select(e => e.Id).Concat(libraryById.Keys).ToHashSet()));
        findings.AddRange(UnknownSessions(snapshot, document));

        return new ExerciseMatching(
            matches,
            idMap,
            toAdd,
            toUpdate,
            findings,
            document.Exercises is null ? rewritten : rewritten with { Exercises = distinct });
    }

    /// <summary>Exercise references that name an Exercise in neither the file's list nor the library.</summary>
    private static IEnumerable<MatchFinding> UnknownExercises(Model.ExchangeDocument document, Dictionary<string, string> idMap, HashSet<string> known) =>
        ExerciseReferences.Paths(document)
            .Where(reference => !idMap.ContainsKey(reference.Id) && !known.Contains(reference.Id))
            .Select(reference => new MatchFinding(MatchFindingKind.Blocker, reference.Path, "unknown_exercise",
                $"{reference.Path} \"{reference.Id}\" is not in the Exercise library. Add it to exercises or use an existing ID."));

    private static (ExerciseOutcome Outcome, Exercise? Target, IReadOnlyList<ExerciseCandidate> Candidates) Resolve(
        Exercise incoming,
        Dictionary<string, Exercise> libraryById,
        Dictionary<string, Exercise> libraryByName,
        IReadOnlyList<Exercise> library,
        ImportDecisions decisions)
    {
        if (libraryById.TryGetValue(incoming.Id, out var sameId))
        {
            return (ExerciseOutcome.MatchedById, sameId, []);
        }

        if (libraryByName.TryGetValue(ExerciseNames.Normalise(incoming.Name), out var sameName))
        {
            return (ExerciseOutcome.MatchedByName, sameName, []);
        }

        var candidates = ExerciseNames.NearMatches(incoming, library);
        if (candidates.Count == 0)
        {
            return (ExerciseOutcome.New, null, candidates);
        }

        if (!decisions.Exercises.TryGetValue(incoming.Id, out var choice))
        {
            return (ExerciseOutcome.NeedsDecision, null, candidates);
        }

        if (choice.UseExerciseId is null)
        {
            return (ExerciseOutcome.New, null, candidates);
        }

        return candidates.Any(c => c.Id == choice.UseExerciseId)
            ? (ExerciseOutcome.UsesExisting, libraryById[choice.UseExerciseId], candidates)
            : (ExerciseOutcome.NeedsDecision, null, candidates);
    }

    /// <summary>The library Exercise with the file's tags and benchmark flag applied. Name and log kind stay the library's; differences are reported.</summary>
    private static Exercise Reconcile(Exercise incoming, Exercise existing, string path, List<MatchFinding> findings)
    {
        if (ExerciseNames.Normalise(incoming.Name) != ExerciseNames.Normalise(existing.Name))
        {
            findings.Add(new MatchFinding(MatchFindingKind.Note, $"{path}.name", "name_differs",
                $"The file calls \"{existing.Name}\" \"{incoming.Name}\". The name in your library is kept."));
        }

        if (incoming.LogKind != existing.LogKind)
        {
            findings.Add(new MatchFinding(MatchFindingKind.Warning, $"{path}.logKind", "log_kind_ignored",
                $"The file logs \"{existing.Name}\" as {incoming.LogKind}, but yours is {existing.LogKind}. Your log kind is kept so history is not reinterpreted."));
        }

        return existing with { Tags = incoming.Tags ?? existing.Tags, IsBenchmark = incoming.IsBenchmark };
    }

    private static bool SameTags(Exercise a, Exercise b) => (a.Tags, b.Tags) switch
    {
        (null, null) => true,
        (null, _) or (_, null) => false,
        var (x, y) => x!.MuscleGroups.SequenceEqual(y!.MuscleGroups) && x.Equipment.SequenceEqual(y.Equipment) && x.MovementPatterns.SequenceEqual(y.MovementPatterns),
    };

    /// <summary>Workout and week-schedule Sessions that are in neither the file's Plans nor the library's.</summary>
    private static IEnumerable<MatchFinding> UnknownSessions(DataSnapshot snapshot, Model.ExchangeDocument document)
    {
        var known = snapshot.Plans.Concat(document.Plans ?? []).SelectMany(p => p.Sessions).Select(s => s.Id).ToHashSet();

        foreach (var (w, i) in (document.Workouts ?? []).Select((w, i) => (w, i)))
        {
            if (w.SessionId is { } id && !known.Contains(id))
            {
                var path = $"workouts[{i}].sessionId";
                yield return new MatchFinding(MatchFindingKind.Blocker, path, "unknown_session", $"{path} \"{id}\" is not a Session in any Plan in this document or your library.");
            }
        }

        foreach (var (week, i) in (document.WeekSchedules ?? []).Select((w, i) => (w, i)))
        {
            foreach (var (move, m) in week.Moves.Select((mv, m) => (mv, m)))
            {
                if (!known.Contains(move.SessionId))
                {
                    var path = $"weekSchedules[{i}].moves[{m}].sessionId";
                    yield return new MatchFinding(MatchFindingKind.Blocker, path, "unknown_session", $"{path} \"{move.SessionId}\" is not a Session in any Plan in this document or your library.");
                }
            }
        }
    }
}
