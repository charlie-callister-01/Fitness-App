using Core.Model;

namespace Core.Exchange;

/// <summary>Name normalising and near-match scoring for Exercise matching.</summary>
internal static class ExerciseNames
{
    public const double NearMatchThreshold = 0.75;
    public const int MaxCandidates = 3;

    /// <summary>Trim, collapse runs of whitespace to one space, ignore case.</summary>
    public static string Normalise(string name) =>
        string.Join(' ', name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();

    /// <summary>Up to three library Exercises that look like <paramref name="incoming"/>, best first; same log kind wins ties.</summary>
    public static IReadOnlyList<ExerciseCandidate> NearMatches(Exercise incoming, IEnumerable<Exercise> library)
    {
        var name = Normalise(incoming.Name);
        return library
            .Select(e => (Exercise: e, Score: Similarity(name, Normalise(e.Name))))
            .Where(x => x.Score >= NearMatchThreshold)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Exercise.LogKind == incoming.LogKind)
            .ThenBy(x => x.Exercise.Name, StringComparer.Ordinal)
            .Take(MaxCandidates)
            .Select(x => new ExerciseCandidate(x.Exercise.Id, x.Exercise.Name, x.Score))
            .ToList();
    }

    /// <summary>1.0 when the word sets are equal (ignoring a trailing "s" on each word), otherwise the character-bigram Dice coefficient.</summary>
    private static double Similarity(string a, string b) => SameWordSet(a, b) ? 1.0 : Dice(a, b);

    private static bool SameWordSet(string a, string b) => WordSet(a).SetEquals(WordSet(b));

    private static HashSet<string> WordSet(string name) =>
        name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(w => w.Length > 1 && w.EndsWith('s') ? w[..^1] : w).ToHashSet();

    private static double Dice(string a, string b)
    {
        var left = Bigrams(a);
        var right = Bigrams(b);
        if (left.Count == 0 || right.Count == 0)
        {
            return 0;
        }

        var shared = 0;
        foreach (var gram in right)
        {
            if (left.Remove(gram))
            {
                shared++;
            }
        }

        return 2.0 * shared / (Bigrams(a).Count + right.Count);
    }

    private static List<string> Bigrams(string s) =>
        s.Length < 2 ? [] : Enumerable.Range(0, s.Length - 1).Select(i => s.Substring(i, 2)).ToList();
}
