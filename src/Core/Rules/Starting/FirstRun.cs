using Core.Model;
using Core.Storage;

namespace Core.Rules.Starting;

/// <summary>The first-run rules: when the app is truly empty, the minimal Plan that gets the owner started, and when the install/storage prompt is still useful.</summary>
public static class FirstRun
{
    /// <summary>True when there are no Plans, no Workouts and no Weigh-ins (the Welcome state). Library Exercises and Settings do not count.</summary>
    public static bool IsEmpty(DataSnapshot snapshot) =>
        snapshot.Plans.Count == 0 && snapshot.Workouts.Count == 0 && snapshot.WeighIns.Count == 0;

    /// <summary>The smallest valid active Plan: one weekly main Track, no Phases and no Sessions.</summary>
    public static Plan EmptyPlan(string planId, string trackId, DateOnly today) =>
        new(
            planId,
            "My Plan",
            true,
            today,
            null,
            null,
            2.5,
            [new Track(trackId, "Main", new TrackRhythm.Weekly(3, 2, false), [])],
            []);

    /// <summary>True while the app is not installed as standalone or the browser has not promised to keep the data.</summary>
    public static bool HasQuickThingsToDo(bool isStandalone, PersistenceStatus persistence) =>
        !isStandalone || persistence == PersistenceStatus.NotGranted;
}
