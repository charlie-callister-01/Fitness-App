using System.Globalization;

namespace Core.Storage;

/// <summary>The kinds of top-level record the store keeps, one per top-level collection of the exchange format.</summary>
public enum RecordKind
{
    Exercise,
    Settings,
    Plan,
    WeighIn,
    Break,
    Badge,
    Guide,
    Workout,
    WeekSchedule,
}

public static class RecordKindExtensions
{
    /// <summary>
    /// The kind's wire name: its top-level property in the exchange format, and the name the IndexedDB module files its records under.
    /// </summary>
    public static string WireName(this RecordKind kind) => RecordDescriptors.For(kind).Property;
}

/// <summary>The key rule for each record kind, read from <see cref="RecordDescriptors"/>.</summary>
public static class RecordKeys
{
    /// <summary>The key of a record: its permanent ID, except a Guide (its Exercise's ID), a Week schedule (its Monday date) and Settings (a fixed key).</summary>
    public static string For<T>(T record) => RecordDescriptors.For<T>().KeyOf(record);

    /// <summary>A Week schedule is keyed by its Monday date, as yyyy-MM-dd.</summary>
    public static string ForWeekStart(DateOnly monday) => monday.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
