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
    private PickerRequest(PickerContext context, IReadOnlyList<Exercise> library, IReadOnlyList<Exercise> similar, IReadOnlyList<Exercise> others)
    {
        Context = context;
        Library = library;
        Similar = similar;
        Others = others;
    }

    public PickerContext Context { get; }

    public IReadOnlyList<Exercise> Library { get; }

    /// <summary>What is offered first as similar to the Exercise in hand (Swap and Add; empty otherwise), with the exclusions already applied.</summary>
    internal IReadOnlyList<Exercise> Similar { get; }

    /// <summary>The rest of what is offered, in the order it is shown, with the exclusions already applied.</summary>
    internal IReadOnlyList<Exercise> Others { get; }

    /// <summary>Swap: the Exercise in hand and the Workout it is in. Those already in the Workout are left out.</summary>
    public static PickerRequest ForSwap(Exercise current, Workout workout, IReadOnlyList<Exercise> library) =>
        Ranked(PickerContext.Swap, current, workout, library);

    /// <summary>Add: as Swap, with the Exercise after which the new one goes.</summary>
    public static PickerRequest ForAdd(Exercise current, Workout workout, IReadOnlyList<Exercise> library) =>
        Ranked(PickerContext.Add, current, workout, library);

    /// <summary>
    /// Custom workout: the Exercises of this week's Missed sessions first, then the rest of the library
    /// (see <see cref="CustomWorkoutOffer.Order"/>). The Exercises already in the draft are left out.
    /// </summary>
    public static PickerRequest ForCustomWorkout(
        Plan? plan, IReadOnlyList<WeekSchedule> schedules, IReadOnlyList<Break> breaks, IReadOnlyList<Workout> workouts,
        IReadOnlyList<Exercise> library, DateOnly today, IReadOnlyCollection<string> draftIds) =>
        new(
            PickerContext.CustomWorkout, library, [],
            Without(plan is null ? library : CustomWorkoutOffer.Order(plan, schedules, breaks, workouts, library, today), draftIds));

    /// <summary>Plan editor: the library in library order, without the Exercises the Session already has.</summary>
    public static PickerRequest ForPlanEditor(IReadOnlyList<Exercise> library, IReadOnlyCollection<string> sessionExerciseIds) =>
        new(PickerContext.PlanEditor, library, [], Without(library, sessionExerciseIds));

    private static PickerRequest Ranked(PickerContext context, Exercise current, Workout workout, IReadOnlyList<Exercise> library)
    {
        var ranked = SimilarExercises.Rank(current, library, workout);
        var similar = ranked.Where(e => SimilarExercises.SharedTagCount(current, e) > 0).ToList();
        return new PickerRequest(context, library, similar, [.. ranked.Except(similar)]);
    }

    private static IReadOnlyList<Exercise> Without(IReadOnlyList<Exercise> offered, IReadOnlyCollection<string> excludedIds) =>
        [.. offered.Where(e => !excludedIds.Contains(e.Id))];
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
        var groups = new List<PickerGroup>();
        AddGroup(groups, SimilarHeading, Match(request.Similar, text));
        AddGroup(groups, AllHeading, Match(request.Others, text));
        return new PickerResult(groups, request.Library.Count > MaxLibraryWithoutSearch, groups.Count == 0 && text.Length > 0 ? text : null);
    }

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
