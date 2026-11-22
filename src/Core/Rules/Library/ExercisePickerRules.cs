using Core.Model;
using Core.Rules.CustomWorkout;
using Core.Rules.Editing;

namespace Core.Rules.Library;

/// <summary>Where the picker is opened from. The context decides which Exercises are offered and in what order.</summary>
public enum PickerContext
{
    /// <summary>Swapping the Exercise in hand for another during a Workout.</summary>
    Swap,

    /// <summary>Adding an Exercise to the Workout after the one in hand.</summary>
    Add,

    /// <summary>Building a Custom workout.</summary>
    CustomWorkout,

    /// <summary>Adding a row to a Session in the Plan editor.</summary>
    PlanEditor,
}

/// <summary>
/// What a picker is being asked for: the <see cref="Context"/>, the library, and what that context needs to order and exclude.
/// Make one with the factory methods.
/// </summary>
public sealed record PickerRequest
{
    private PickerRequest(PickerContext context, IReadOnlyList<Exercise> library)
    {
        Context = context;
        Library = library;
    }

    public PickerContext Context { get; }

    public IReadOnlyList<Exercise> Library { get; }

    internal Exercise? Current { get; private init; }

    internal Workout? Workout { get; private init; }

    internal IReadOnlyCollection<string> ExcludedIds { get; private init; } = [];

    internal IReadOnlyList<Exercise>? Offered { get; private init; }

    /// <summary>Swap: the Exercise in hand and the Workout it is in. Those already in the Workout are left out.</summary>
    public static PickerRequest ForSwap(Exercise current, Workout workout, IReadOnlyList<Exercise> library) =>
        new(PickerContext.Swap, library) { Current = current, Workout = workout };

    /// <summary>Add: as Swap, with the Exercise after which the new one goes.</summary>
    public static PickerRequest ForAdd(Exercise current, Workout workout, IReadOnlyList<Exercise> library) =>
        new(PickerContext.Add, library) { Current = current, Workout = workout };

    /// <summary>
    /// Custom workout: the Exercises of this week's Missed sessions first, then the rest of the library
    /// (see <see cref="CustomWorkoutOffer.Order"/>). The Exercises already in the draft are left out.
    /// </summary>
    public static PickerRequest ForCustomWorkout(
        Plan? plan, IReadOnlyList<WeekSchedule> schedules, IReadOnlyList<Break> breaks, IReadOnlyList<Workout> workouts,
        IReadOnlyList<Exercise> library, DateOnly today, IReadOnlyCollection<string> draftIds) =>
        new(PickerContext.CustomWorkout, library)
        {
            ExcludedIds = draftIds,
            Offered = plan is null ? library : CustomWorkoutOffer.Order(plan, schedules, breaks, workouts, library, today),
        };

    /// <summary>Plan editor: the library in library order, without the Exercises the Session already has.</summary>
    public static PickerRequest ForPlanEditor(IReadOnlyList<Exercise> library, IReadOnlyCollection<string> sessionExerciseIds) =>
        new(PickerContext.PlanEditor, library) { ExcludedIds = sessionExerciseIds, Offered = library };
}

/// <summary>One titled group of Exercises in the picker.</summary>
public sealed record PickerGroup(string Heading, IReadOnlyList<Exercise> Items);

/// <summary>
/// What the picker shows. <see cref="Groups"/> holds only groups that have something in them. <see cref="ShowSearch"/> is true when the
/// library is big enough to need a search box. <see cref="NewExerciseOffer"/> is the text to offer as a new Exercise's name when a
/// search matched nothing (null otherwise): the picker words it as "Add the text as a new Exercise".
/// </summary>
public sealed record PickerResult(IReadOnlyList<PickerGroup> Groups, bool ShowSearch, string? NewExerciseOffer)
{
    public bool IsEmpty => Groups.Count == 0;
}

/// <summary>The one place the picker's matching, ordering, exclusion and search threshold are decided.</summary>
public static class ExercisePickerRules
{
    /// <summary>The library is searchable once it has more Exercises than this.</summary>
    public const int MaxLibraryWithoutSearch = 20;

    public const string SimilarHeading = "Similar";

    public const string AllHeading = "All Exercises";

    /// <summary>
    /// The groups for <paramref name="request"/> narrowed to names containing <paramref name="query"/> (ignoring case and surrounding
    /// spaces). Swap and Add offer a Similar group (sharing a tag with the Exercise in hand) before All Exercises; the other
    /// contexts offer All Exercises only.
    /// </summary>
    public static PickerResult Choose(PickerRequest request, string? query)
    {
        var text = query?.Trim() ?? "";
        var (similar, others) = request.Current is { } current && request.Workout is { } workout
            ? Ranked(current, request.Library, workout)
            : ([], Remaining(request));

        var groups = new List<PickerGroup>();
        AddGroup(groups, SimilarHeading, Match(similar, text));
        AddGroup(groups, AllHeading, Match(others, text));
        return new PickerResult(groups, request.Library.Count > MaxLibraryWithoutSearch, groups.Count == 0 && text.Length > 0 ? text : null);
    }

    private static (IReadOnlyList<Exercise> Similar, IReadOnlyList<Exercise> Others) Ranked(
        Exercise current, IReadOnlyList<Exercise> library, Workout workout)
    {
        var ranked = SimilarExercises.Rank(current, library, workout);
        var similar = ranked.Where(e => SimilarExercises.SharedTagCount(current, e) > 0).ToList();
        return (similar, [.. ranked.Except(similar)]);
    }

    private static IReadOnlyList<Exercise> Remaining(PickerRequest request) =>
        [.. (request.Offered ?? request.Library).Where(e => !request.ExcludedIds.Contains(e.Id))];

    private static IReadOnlyList<Exercise> Match(IReadOnlyList<Exercise> items, string text) =>
        text.Length == 0 ? items : [.. items.Where(e => e.Name.Contains(text, StringComparison.OrdinalIgnoreCase))];

    private static void AddGroup(List<PickerGroup> groups, string heading, IReadOnlyList<Exercise> items)
    {
        if (items.Count > 0)
        {
            groups.Add(new PickerGroup(heading, items));
        }
    }
}
