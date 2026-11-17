using System.Collections.Immutable;
using Core.Model;
using Core.Serialisation;

namespace Core.Storage;

/// <summary>
/// One typed step of a commit: put (insert or replace by key) or delete a single record. Puts keep the position of the record they replace
/// and append new ones. Kinds and keys come from <see cref="RecordKeys"/>; the stored JSON is the exchange format's own, written by
/// <see cref="StoredRecords"/>, so there is no second mapping.
/// </summary>
public sealed class Change
{
    private readonly Func<DataSnapshot, DataSnapshot> _applyTo;
    private readonly ExchangeDocument? _fragment;

    private Change(RecordKind kind, string key, ExchangeDocument? fragment, Func<DataSnapshot, DataSnapshot> applyTo)
    {
        Kind = kind;
        Key = key;
        _fragment = fragment;
        _applyTo = applyTo;
    }

    public RecordKind Kind { get; }

    public string Key { get; }

    /// <summary>A document holding only the one record being put, so saving it never serialises the rest of the dataset.</summary>
    private static ExchangeDocument Fragment() => new(ExchangeFormat.CurrentVersion, null, null, null, null);

    public static Change Put(Exercise exercise) => new(RecordKind.Exercise, RecordKeys.ForExercise(exercise),
        Fragment() with { Exercises = [exercise] },
        s => s with { Exercises = Upsert(s.Exercises, exercise, RecordKeys.ForExercise) });

    public static Change Put(Settings settings) => new(RecordKind.Settings, RecordKeys.SettingsKey,
        Fragment() with { Settings = settings },
        s => s with { Settings = settings });

    public static Change Put(Plan plan) => new(RecordKind.Plan, RecordKeys.ForPlan(plan),
        Fragment() with { Plans = [plan] },
        s => s with { Plans = Upsert(s.Plans, plan, RecordKeys.ForPlan) });

    public static Change Put(WeighIn weighIn) => new(RecordKind.WeighIn, RecordKeys.ForWeighIn(weighIn),
        Fragment() with { WeighIns = [weighIn] },
        s => s with { WeighIns = Upsert(s.WeighIns, weighIn, RecordKeys.ForWeighIn) });

    public static Change Put(Break @break) => new(RecordKind.Break, RecordKeys.ForBreak(@break),
        Fragment() with { Breaks = [@break] },
        s => s with { Breaks = Upsert(s.Breaks, @break, RecordKeys.ForBreak) });

    public static Change Put(Badge badge) => new(RecordKind.Badge, RecordKeys.ForBadge(badge),
        Fragment() with { Badges = [badge] },
        s => s with { Badges = Upsert(s.Badges, badge, RecordKeys.ForBadge) });

    public static Change Put(Guide guide) => new(RecordKind.Guide, RecordKeys.ForGuide(guide),
        Fragment() with { Guides = [guide] },
        s => s with { Guides = Upsert(s.Guides, guide, RecordKeys.ForGuide) });

    public static Change Put(Workout workout) => new(RecordKind.Workout, RecordKeys.ForWorkout(workout),
        Fragment() with { Workouts = [workout] },
        s => s with { Workouts = Upsert(s.Workouts, workout, RecordKeys.ForWorkout) });

    public static Change Put(WeekSchedule schedule) => new(RecordKind.WeekSchedule, RecordKeys.ForWeekSchedule(schedule),
        Fragment() with { WeekSchedules = [schedule] },
        s => s with { WeekSchedules = Upsert(s.WeekSchedules, schedule, RecordKeys.ForWeekSchedule) });

    /// <summary>Deletes the record with this key (see <see cref="RecordKeys"/>). A missing key is not an error. Settings cannot be deleted, only replaced.</summary>
    public static Change Delete(RecordKind kind, string key) => kind switch
    {
        RecordKind.Exercise => Remove(kind, key, s => s with { Exercises = Without(s.Exercises, RecordKeys.ForExercise, key) }),
        RecordKind.Plan => Remove(kind, key, s => s with { Plans = Without(s.Plans, RecordKeys.ForPlan, key) }),
        RecordKind.WeighIn => Remove(kind, key, s => s with { WeighIns = Without(s.WeighIns, RecordKeys.ForWeighIn, key) }),
        RecordKind.Break => Remove(kind, key, s => s with { Breaks = Without(s.Breaks, RecordKeys.ForBreak, key) }),
        RecordKind.Badge => Remove(kind, key, s => s with { Badges = Without(s.Badges, RecordKeys.ForBadge, key) }),
        RecordKind.Guide => Remove(kind, key, s => s with { Guides = Without(s.Guides, RecordKeys.ForGuide, key) }),
        RecordKind.Workout => Remove(kind, key, s => s with { Workouts = Without(s.Workouts, RecordKeys.ForWorkout, key) }),
        RecordKind.WeekSchedule => Remove(kind, key, s => s with { WeekSchedules = Without(s.WeekSchedules, RecordKeys.ForWeekSchedule, key) }),
        _ => throw new ArgumentException($"A {kind} record cannot be deleted.", nameof(kind)),
    };

    internal DataSnapshot ApplyTo(DataSnapshot snapshot) => _applyTo(snapshot);

    /// <summary>What to hand the store for this change: the stored JSON for a put, a delete otherwise.</summary>
    internal StoreChange ToStoreChange() => _fragment is null
        ? new StoreChange.Delete(Kind, Key)
        : new StoreChange.Put(StoredRecords.FromDocument(_fragment).Single());

    private static Change Remove(RecordKind kind, string key, Func<DataSnapshot, DataSnapshot> applyTo) => new(kind, key, null, applyTo);

    private static IReadOnlyList<T> Upsert<T>(IReadOnlyList<T> items, T item, Func<T, string> keyOf)
    {
        var key = keyOf(item);
        var next = items.ToList();
        var index = next.FindIndex(existing => keyOf(existing) == key);
        if (index >= 0)
        {
            next[index] = item;
        }
        else
        {
            next.Add(item);
        }

        return [.. next];
    }

    private static IReadOnlyList<T> Without<T>(IReadOnlyList<T> items, Func<T, string> keyOf, string key) =>
        items.Any(item => keyOf(item) == key) ? [.. items.Where(item => keyOf(item) != key)] : items;
}
