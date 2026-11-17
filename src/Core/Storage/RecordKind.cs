using System.Globalization;
using Core.Model;

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

/// <summary>The key rule for each record kind. Every kind is keyed by its permanent ID except Settings, Guide and Week schedule.</summary>
public static class RecordKeys
{
    /// <summary>Settings is a singleton: it is always stored under this one key.</summary>
    public const string SettingsKey = "settings";

    public static string ForExercise(Exercise exercise) => exercise.Id;

    public static string ForPlan(Plan plan) => plan.Id;

    public static string ForWeighIn(WeighIn weighIn) => weighIn.Id;

    public static string ForBreak(Break @break) => @break.Id;

    public static string ForBadge(Badge badge) => badge.Id;

    /// <summary>A Guide is keyed by the Exercise it describes.</summary>
    public static string ForGuide(Guide guide) => guide.ExerciseId;

    public static string ForWorkout(Workout workout) => workout.Id;

    /// <summary>A Week schedule is keyed by its Monday date, as yyyy-MM-dd.</summary>
    public static string ForWeekSchedule(WeekSchedule schedule) => ForWeekStart(schedule.WeekStart);

    public static string ForWeekStart(DateOnly monday) => monday.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
