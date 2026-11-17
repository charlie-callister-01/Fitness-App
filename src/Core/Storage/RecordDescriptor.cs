using System.Text.Json;
using Core.Model;

namespace Core.Storage;

/// <summary>
/// Everything the storage layer knows about one record kind, in one place: its wire name, how its key is read, and how a record of the
/// kind is put into or removed from a <see cref="DataSnapshot"/>. <see cref="Change"/>, <see cref="StoredRecords"/> and
/// <see cref="RecordKeys"/> all work through these, so a new kind is one entry in <see cref="RecordDescriptors"/>.
/// </summary>
internal abstract class RecordDescriptor
{
    private protected RecordDescriptor(RecordKind kind, string property)
    {
        Kind = kind;
        Property = property;
    }

    public RecordKind Kind { get; }

    /// <summary>The top-level property of the exchange document that holds this kind.</summary>
    public string Property { get; }

    /// <summary>True when the kind is one record rather than a collection (Settings): always stored under one key, never deleted.</summary>
    public abstract bool IsSingleton { get; }

    /// <summary>Splits the document's value for this kind into one stored record per top-level record.</summary>
    public abstract IEnumerable<StoredRecord> Split(JsonElement value);

    /// <summary>Writes the document's value for this kind from its readable stored records.</summary>
    public abstract void WriteValue(Utf8JsonWriter writer, IReadOnlyList<StoredRecord> records);

    /// <summary>The record that an item index of a reader error path points at, or null when none does.</summary>
    public abstract StoredRecord? Pick(IReadOnlyList<StoredRecord> records, int? index);

    public abstract DataSnapshot ApplyDelete(DataSnapshot snapshot, string key);
}

/// <summary>A <see cref="RecordDescriptor"/> that also knows the record's CLR type.</summary>
internal abstract class RecordDescriptor<T> : RecordDescriptor
{
    private protected RecordDescriptor(RecordKind kind, string property) : base(kind, property) { }

    public abstract string KeyOf(T record);

    /// <summary>Inserts or replaces the record. A replaced record keeps its position; a new one is appended.</summary>
    public abstract DataSnapshot ApplyPut(DataSnapshot snapshot, T record);

    /// <summary>
    /// The record as it is stored: the exchange format's own JSON for it. Written by the same serialiser as a Backup (through a document
    /// holding only this record), so stored JSON and Backup JSON cannot differ.
    /// </summary>
    public StoredRecord ToStored(T record) =>
        StoredRecords.FromDocument(ApplyPut(DataSnapshot.Empty, record).ToDocument()).Single(stored => stored.Kind == Kind);
}

/// <summary>A kind with many records, each keyed by the JSON field <c>keyField</c>.</summary>
internal sealed class CollectionDescriptor<T>(
    RecordKind kind,
    string property,
    string keyField,
    Func<T, string> keyOf,
    Func<DataSnapshot, IReadOnlyList<T>> read,
    Func<DataSnapshot, IReadOnlyList<T>, DataSnapshot> replace) : RecordDescriptor<T>(kind, property)
{
    public override bool IsSingleton => false;

    public override string KeyOf(T record) => keyOf(record);

    public override IEnumerable<StoredRecord> Split(JsonElement value) =>
        value.EnumerateArray().Select(item => new StoredRecord(Kind, item.GetProperty(keyField).GetString()!, item.GetRawText()));

    public override void WriteValue(Utf8JsonWriter writer, IReadOnlyList<StoredRecord> records)
    {
        writer.WriteStartArray();
        foreach (var record in records)
        {
            writer.WriteRawValue(record.Json);
        }

        writer.WriteEndArray();
    }

    public override StoredRecord? Pick(IReadOnlyList<StoredRecord> records, int? index) =>
        index is { } i && i < records.Count ? records[i] : null;

    public override DataSnapshot ApplyPut(DataSnapshot snapshot, T record)
    {
        var key = keyOf(record);
        var next = read(snapshot).ToList();
        var position = next.FindIndex(existing => keyOf(existing) == key);
        if (position >= 0)
        {
            next[position] = record;
        }
        else
        {
            next.Add(record);
        }

        return replace(snapshot, [.. next]);
    }

    public override DataSnapshot ApplyDelete(DataSnapshot snapshot, string key) =>
        read(snapshot).Any(item => keyOf(item) == key) ? replace(snapshot, [.. read(snapshot).Where(item => keyOf(item) != key)]) : snapshot;
}

/// <summary>A kind with exactly one record, always stored under <see cref="StoredKey"/>.</summary>
internal sealed class SingletonDescriptor<T>(RecordKind kind, string property, Func<DataSnapshot, T, DataSnapshot> replace)
    : RecordDescriptor<T>(kind, property)
{
    public const string StoredKey = "settings";

    public override bool IsSingleton => true;

    public override string KeyOf(T record) => StoredKey;

    public override IEnumerable<StoredRecord> Split(JsonElement value) => [new StoredRecord(Kind, StoredKey, value.GetRawText())];

    public override void WriteValue(Utf8JsonWriter writer, IReadOnlyList<StoredRecord> records) => writer.WriteRawValue(records[0].Json);

    public override StoredRecord? Pick(IReadOnlyList<StoredRecord> records, int? index) => records[0];

    public override DataSnapshot ApplyPut(DataSnapshot snapshot, T record) => replace(snapshot, record);

    public override DataSnapshot ApplyDelete(DataSnapshot snapshot, string key) =>
        throw new InvalidOperationException($"A {Kind} record cannot be deleted, only replaced.");
}

/// <summary>The descriptor of every record kind, in the order their properties are written to a document.</summary>
internal static class RecordDescriptors
{
    public static readonly CollectionDescriptor<Exercise> Exercise = new(
        RecordKind.Exercise, "exercises", "id", exercise => exercise.Id, s => s.Exercises, (s, items) => s with { Exercises = items });

    public static readonly SingletonDescriptor<Settings> Settings = new(
        RecordKind.Settings, "settings", (s, settings) => s with { Settings = settings });

    public static readonly CollectionDescriptor<Plan> Plan = new(
        RecordKind.Plan, "plans", "id", plan => plan.Id, s => s.Plans, (s, items) => s with { Plans = items });

    public static readonly CollectionDescriptor<WeighIn> WeighIn = new(
        RecordKind.WeighIn, "weighIns", "id", weighIn => weighIn.Id, s => s.WeighIns, (s, items) => s with { WeighIns = items });

    public static readonly CollectionDescriptor<Break> Break = new(
        RecordKind.Break, "breaks", "id", @break => @break.Id, s => s.Breaks, (s, items) => s with { Breaks = items });

    public static readonly CollectionDescriptor<Badge> Badge = new(
        RecordKind.Badge, "badges", "id", badge => badge.Id, s => s.Badges, (s, items) => s with { Badges = items });

    /// <summary>A Guide is keyed by the Exercise it describes.</summary>
    public static readonly CollectionDescriptor<Guide> Guide = new(
        RecordKind.Guide, "guides", "exerciseId", guide => guide.ExerciseId, s => s.Guides, (s, items) => s with { Guides = items });

    public static readonly CollectionDescriptor<Workout> Workout = new(
        RecordKind.Workout, "workouts", "id", workout => workout.Id, s => s.Workouts, (s, items) => s with { Workouts = items });

    /// <summary>A Week schedule is keyed by its Monday date.</summary>
    public static readonly CollectionDescriptor<WeekSchedule> WeekSchedule = new(
        RecordKind.WeekSchedule, "weekSchedules", "weekStart", schedule => RecordKeys.ForWeekStart(schedule.WeekStart),
        s => s.WeekSchedules, (s, items) => s with { WeekSchedules = items });

    public static readonly IReadOnlyList<RecordDescriptor> All =
        [Exercise, Settings, Plan, WeighIn, Break, Badge, Guide, Workout, WeekSchedule];

    public static RecordDescriptor For(RecordKind kind) => All.Single(descriptor => descriptor.Kind == kind);

    /// <summary>The descriptor for records of type <typeparamref name="T"/>; throws for a type that is not a stored record.</summary>
    public static RecordDescriptor<T> For<T>() =>
        All.OfType<RecordDescriptor<T>>().SingleOrDefault()
        ?? throw new ArgumentException($"{typeof(T).Name} is not a stored record type.", nameof(T));
}
