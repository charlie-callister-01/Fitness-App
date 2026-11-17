using Core.Model;

namespace Core.Exchange;

/// <summary>How one Exercise in the file was resolved against the library.</summary>
public enum ExerciseOutcome
{
    /// <summary>Same ID as a library Exercise.</summary>
    MatchedById,

    /// <summary>Same name after normalising; the file's ID maps to the library's.</summary>
    MatchedByName,

    /// <summary>A near-match the owner has not answered yet. Blocks the Import.</summary>
    NeedsDecision,

    /// <summary>A near-match the owner resolved by choosing a suggested library Exercise.</summary>
    UsesExisting,

    /// <summary>Nothing matched, or the owner chose "add as a new Exercise". Keeps the file's ID.</summary>
    New,
}

/// <summary>A library Exercise suggested for a near-match. Score is 0 to 1, higher is closer.</summary>
public sealed record ExerciseCandidate(string Id, string Name, double Score);

/// <summary>
/// One Exercise in the file and what it resolved to. <see cref="Chosen"/> is the library Exercise ID it maps to,
/// or null while the outcome is <see cref="ExerciseOutcome.NeedsDecision"/> or the Exercise is <see cref="ExerciseOutcome.New"/>.
/// </summary>
public sealed record ExerciseMatch(
    string IncomingId,
    string IncomingName,
    ExerciseOutcome Outcome,
    IReadOnlyList<ExerciseCandidate> Candidates,
    string? Chosen);

public enum MatchFindingKind
{
    Note,
    Warning,
    Blocker,
}

/// <summary>Something matching found that the preview should show. Path is like <c>workouts[1].entries[0].exerciseId</c>.</summary>
public sealed record MatchFinding(MatchFindingKind Kind, string Path, string Code, string Message);

/// <summary>
/// What matching and remapping produced. <see cref="Document"/> is the file's document with every Exercise reference
/// pointing at its resolved ID and its Exercises list replaced by the resolved Exercises (null if the file had none).
/// <see cref="ToAdd"/> are Exercises new to the library; <see cref="ToUpdate"/> are library Exercises whose tags or benchmark flag the file changes.
/// </summary>
public sealed record ExerciseMatching(
    IReadOnlyList<ExerciseMatch> Matches,
    IReadOnlyDictionary<string, string> IdMap,
    IReadOnlyList<Exercise> ToAdd,
    IReadOnlyList<Exercise> ToUpdate,
    IReadOnlyList<MatchFinding> Findings,
    ExchangeDocument Document)
{
    /// <summary>The near-matches still waiting for the owner. An Import cannot be applied while any remain.</summary>
    public IEnumerable<ExerciseMatch> OpenDecisions => Matches.Where(m => m.Outcome == ExerciseOutcome.NeedsDecision);
}
