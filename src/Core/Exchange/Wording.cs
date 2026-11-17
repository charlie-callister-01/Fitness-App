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

    /// <summary>"1 Plan", "3 Plans".</summary>
    public static string Counted(int count, (string Singular, string Plural) name) => $"{count} {(count == 1 ? name.Singular : name.Plural)}";
}
