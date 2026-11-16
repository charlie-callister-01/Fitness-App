using Core.Model;

namespace Core.Serialisation;

/// <summary>
/// What a reference is checked against: either nothing (<see cref="Unchecked"/>, for a document that lacks the referenced
/// collection, whose IDs are resolved on import) or a known set of IDs.
/// </summary>
internal sealed class ReferenceScope
{
    /// <summary>Reference checking is off.</summary>
    public static readonly ReferenceScope Unchecked = new(null, "", null!);

    private readonly HashSet<string>? _known;
    private readonly string _code;
    private readonly Func<string, string, string> _message;

    private ReferenceScope(HashSet<string>? known, string code, Func<string, string, string> message)
    {
        _known = known;
        _code = code;
        _message = message;
    }

    /// <summary>Exercise references; unchecked when the document has no Exercise library.</summary>
    public static ReferenceScope ForExercises(IEnumerable<Exercise>? exercises) =>
        exercises is null
            ? Unchecked
            : new ReferenceScope(exercises.Select(e => e.Id).ToHashSet(), "unknown_exercise",
                (path, id) => $"{path} \"{id}\" is not in the Exercise library. Add it to exercises or use an existing ID.");

    /// <summary>Session references from a Workout; unchecked when the document has no Plans.</summary>
    public static ReferenceScope ForSessions(IEnumerable<Plan>? plans) =>
        plans is null
            ? Unchecked
            : new ReferenceScope(plans.SelectMany(p => p.Sessions).Select(s => s.Id).ToHashSet(), "unknown_session",
                (path, id) => $"{path} \"{id}\" is not a Session in any Plan in this document.");

    /// <summary>Session references from a Phase override: they must name a Session of the same Plan.</summary>
    public static ReferenceScope ForSessionsInPlan(HashSet<string> sessionIds) =>
        new(sessionIds, "unknown_session", (path, id) => $"{path} \"{id}\" is not a Session in this Plan.");

    /// <summary>Reports the unknown-reference error when this scope is checked and does not contain <paramref name="id"/>.</summary>
    public void Check(IssueSink sink, string path, string id)
    {
        if (_known is not null && !_known.Contains(id))
        {
            sink.Error(path, _code, _message(path, id));
        }
    }
}

/// <summary>
/// The state shared by every reader of one document read: where issues go, what references are checked against,
/// and the IDs seen so far per record kind. Built once at the top of the reader and carried by each <see cref="FieldReader"/>.
/// </summary>
internal sealed class ReadContext
{
    private readonly Dictionary<string, HashSet<string>> _seenIds = [];

    public IssueSink Sink { get; } = new();

    /// <summary>Set once the document's Exercises have been read; unchecked until then and when the document has none.</summary>
    public ReferenceScope Exercises { get; set; } = ReferenceScope.Unchecked;

    /// <summary>Set once the document's Plans have been read; unchecked until then and when the document has none.</summary>
    public ReferenceScope Sessions { get; set; } = ReferenceScope.Unchecked;

    /// <summary>The IDs already used by records of one kind (Exercise, Plan, Set and so on); IDs are unique per kind.</summary>
    public HashSet<string> SeenIds(string kind)
    {
        if (!_seenIds.TryGetValue(kind, out var seen))
        {
            seen = [];
            _seenIds[kind] = seen;
        }

        return seen;
    }
}
