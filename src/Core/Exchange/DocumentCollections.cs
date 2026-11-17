using Core.Model;
using Core.Storage;

namespace Core.Exchange;

/// <summary>Which collections a document carries. One definition shared by the kind suggestion, the ignored-items list and the Restore check.</summary>
internal static class DocumentCollections
{
    /// <summary>The parts a complete Backup must have, in the order they are named when missing. Week schedules are optional.</summary>
    public static readonly IReadOnlyList<RecordKind> BackupKinds =
    [
        RecordKind.Exercise, RecordKind.Settings, RecordKind.Plan, RecordKind.WeighIn,
        RecordKind.Break, RecordKind.Badge, RecordKind.Guide, RecordKind.Workout,
    ];

    /// <summary>The kinds of record the document carries, with how many of each (null for Settings, which is one record).</summary>
    public static IReadOnlyList<(RecordKind Kind, int? Count)> Present(ExchangeDocument d)
    {
        (RecordKind Kind, int? Count, bool IsPresent)[] all =
        [
            (RecordKind.Exercise, d.Exercises?.Count, d.Exercises is not null),
            (RecordKind.Settings, null, d.Settings is not null),
            (RecordKind.Plan, d.Plans?.Count, d.Plans is not null),
            (RecordKind.WeighIn, d.WeighIns?.Count, d.WeighIns is not null),
            (RecordKind.Break, d.Breaks?.Count, d.Breaks is not null),
            (RecordKind.Badge, d.Badges?.Count, d.Badges is not null),
            (RecordKind.Guide, d.Guides?.Count, d.Guides is not null),
            (RecordKind.Workout, d.Workouts?.Count, d.Workouts is not null),
            (RecordKind.WeekSchedule, d.WeekSchedules?.Count, d.WeekSchedules is not null),
        ];
        return [.. all.Where(p => p.IsPresent).Select(p => (p.Kind, p.Count))];
    }

    public static IReadOnlySet<RecordKind> PresentKinds(ExchangeDocument d) => Present(d).Select(p => p.Kind).ToHashSet();

    /// <summary>The wire names of the Backup parts the document does not have.</summary>
    public static IReadOnlyList<string> MissingFromBackup(ExchangeDocument d)
    {
        var present = PresentKinds(d);
        return [.. BackupKinds.Where(k => !present.Contains(k)).Select(k => k.WireName())];
    }
}
