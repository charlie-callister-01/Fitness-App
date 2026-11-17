using Core.Storage;

namespace Core.Exchange;

/// <summary>Shared words for preview sentences, so every kind counts and names things the same way.</summary>
internal static class Wording
{
    public static readonly IReadOnlyDictionary<RecordKind, (string Singular, string Plural)> Names = new Dictionary<RecordKind, (string, string)>
    {
        [RecordKind.Exercise] = ("Exercise", "Exercises"),
        [RecordKind.Settings] = ("Settings", "Settings"),
        [RecordKind.Plan] = ("Plan", "Plans"),
        [RecordKind.WeighIn] = ("Weigh-in", "Weigh-ins"),
        [RecordKind.Break] = ("Break", "Breaks"),
        [RecordKind.Badge] = ("Badge", "Badges"),
        [RecordKind.Guide] = ("Guide", "Guides"),
        [RecordKind.Workout] = ("Workout", "Workouts"),
        [RecordKind.WeekSchedule] = ("Week schedule", "Week schedules"),
    };

    /// <summary>A Pain check lives inside a Workout, so it is not a record kind of its own.</summary>
    public static readonly (string Singular, string Plural) PainCheck = ("Pain check", "Pain checks");

    /// <summary>"1 Plan", "3 Plans".</summary>
    public static string Counted(int count, (string Singular, string Plural) name) => $"{count} {(count == 1 ? name.Singular : name.Plural)}";

    /// <summary>
    /// The summary lines the by-key kinds share: "Adds 2 Guides.", "Replaces 1 Guide." (<paramref name="replaceVerb"/> says which word), and
    /// "Keeps 1 existing Guide. Choose to replace it to use the file's version." A line for a zero count is left out.
    /// </summary>
    public static List<string> AddedReplacedKept(RecordKind kind, string replaceVerb, int added, int replaced, int kept)
    {
        var name = Names[kind];
        var summary = new List<string>();
        if (added > 0)
        {
            summary.Add($"Adds {Counted(added, name)}.");
        }

        if (replaced > 0)
        {
            summary.Add($"{replaceVerb} {Counted(replaced, name)}.");
        }

        if (kept > 0)
        {
            summary.Add($"Keeps {kept} existing {(kept == 1 ? name.Singular : name.Plural)}. Choose to replace {(kept == 1 ? "it" : "them")} to use the file's version.");
        }

        return summary;
    }
}
