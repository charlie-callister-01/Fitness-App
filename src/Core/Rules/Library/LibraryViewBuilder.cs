using Core.Model;

namespace Core.Rules.Library;

/// <summary>One Exercise row in the library: the Exercise and whether it has a Guide.</summary>
public sealed record LibraryRow(Exercise Exercise, bool HasGuide);

/// <summary>A titled group of rows: a Session's name, or "Not in a Session".</summary>
public sealed record LibraryGroup(string Heading, IReadOnlyList<LibraryRow> Rows);

/// <summary>
/// What the Exercise library page shows. <see cref="Tags"/> are the chips (every tag in the library, once). <see cref="NewExerciseOffer"/> is
/// the text of a search that matched nothing, to offer as a new Exercise's name (null otherwise). <see cref="IsLibraryEmpty"/> is true when
/// the library has no Exercises at all (as opposed to none matching).
/// </summary>
public sealed record LibraryView(
    IReadOnlyList<string> Tags, IReadOnlyList<LibraryGroup> Groups, bool ShowSearch, string? NewExerciseOffer, bool IsLibraryEmpty);

/// <summary>Builds the Exercise library page: groups by Session, search, tag filter and Guide status.</summary>
public static class LibraryViewBuilder
{
    public const string NotInASession = "Not in a Session";

    /// <summary>
    /// Every Exercise once: under the first Session of the active <paramref name="plan"/> that holds it (Sessions in Plan order, rows in row
    /// order), then the rest under "Not in a Session" by name. <paramref name="query"/> narrows by name (ignoring case and surrounding
    /// spaces) and <paramref name="tag"/> to Exercises carrying that tag (ignoring case); empty groups are left out.
    /// </summary>
    public static LibraryView Build(
        Plan? plan, IReadOnlyList<Exercise> exercises, IReadOnlyList<Guide> guides, string? query, string? tag)
    {
        var text = query?.Trim() ?? "";
        var guided = guides.Select(g => g.ExerciseId).ToHashSet();
        var byId = new Dictionary<string, Exercise>();
        foreach (var exercise in exercises)
        {
            byId.TryAdd(exercise.Id, exercise);
        }

        var shown = new HashSet<string>();
        var groups = new List<LibraryGroup>();
        foreach (var session in plan?.Sessions ?? [])
        {
            var rows = session.Exercises
                .Select(r => byId.GetValueOrDefault(r.ExerciseId))
                .OfType<Exercise>()
                .Where(e => shown.Add(e.Id))
                .ToList();
            AddGroup(groups, session.Name, rows, text, tag, guided);
        }

        var rest = exercises.Where(e => !shown.Contains(e.Id)).OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase).ToList();
        AddGroup(groups, NotInASession, rest, text, tag, guided);

        var offer = groups.Count == 0 && text.Length > 0 && tag is null ? text : null;
        return new LibraryView(TagsOf(exercises), groups, exercises.Count > ExercisePickerRules.MaxLibraryWithoutSearch, offer, exercises.Count == 0);
    }

    private static void AddGroup(
        List<LibraryGroup> groups, string heading, IEnumerable<Exercise> exercises, string text, string? tag, HashSet<string> guided)
    {
        var rows = exercises
            .Where(e => text.Length == 0 || e.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
            .Where(e => tag is null || TagWords(e).Contains(tag, StringComparer.OrdinalIgnoreCase))
            .Select(e => new LibraryRow(e, guided.Contains(e.Id)))
            .ToList();
        if (rows.Count > 0)
        {
            groups.Add(new LibraryGroup(heading, rows));
        }
    }

    private static IReadOnlyList<string> TagsOf(IEnumerable<Exercise> exercises) =>
        [.. exercises.SelectMany(TagWords).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(t => t, StringComparer.OrdinalIgnoreCase)];

    /// <summary>Every tag of an Exercise, whichever category it is in.</summary>
    public static IEnumerable<string> TagWords(Exercise exercise) =>
        exercise.Tags is { } t ? t.MuscleGroups.Concat(t.Equipment).Concat(t.MovementPatterns) : [];
}
