using System.Globalization;
using Core.Model;

namespace Core.Serialisation;

/// <summary>
/// Reads and validates the <c>plans</c> collection of a version 1 document: Plans, their Phases and Sessions, and every cross-reference
/// between them. Exercise references are checked against the document's Exercise library when it has one; a Plan-only document
/// (no <c>exercises</c>) can't be checked here, so its Exercise IDs are resolved when it is imported.
/// </summary>
internal static class PlanReader
{
    public static List<Plan>? Read(FieldReader root, IReadOnlyList<Exercise>? exercises, IssueSink sink)
    {
        var items = root.OptionalObjectArray("plans");
        if (items is null)
        {
            return null;
        }

        var context = new Context(sink, exercises?.Select(e => e.Id).ToHashSet());
        var plans = new List<Plan>();
        var seenPlanIds = new HashSet<string>();
        var activePlans = new List<(int Index, string Id)>();
        foreach (var (reader, index) in items)
        {
            var errorsBefore = sink.Errors.Count;
            var plan = ReadPlan(reader, context);

            if (plan.Id.Length > 0 && !seenPlanIds.Add(plan.Id))
            {
                var path = reader.ChildPath("id");
                sink.Error(path, "duplicate_id", $"{path} \"{plan.Id}\" is used by more than one Plan. Every Plan needs its own ID.");
            }

            if (plan.IsActive)
            {
                activePlans.Add((index, plan.Id));
            }

            if (sink.Errors.Count == errorsBefore)
            {
                plans.Add(plan);
            }
        }

        CheckExactlyOneActive(items.Count, activePlans, sink);
        return plans;
    }

    private static void CheckExactlyOneActive(int planCount, List<(int Index, string Id)> activePlans, IssueSink sink)
    {
        if (planCount == 0)
        {
            return;
        }

        if (activePlans.Count == 0)
        {
            sink.Error("plans", "no_active_plan", "plans must contain exactly one active Plan, but none has \"isActive\": true.");
            return;
        }

        foreach (var (index, _) in activePlans.Skip(1))
        {
            sink.Error($"plans[{index}].isActive", "multiple_active_plans",
                $"Only one Plan can be active, but \"{activePlans[0].Id}\" is already active. Set isActive to false on all but one.");
        }
    }

    private static Plan ReadPlan(FieldReader reader, Context context)
    {
        var id = reader.RequiredId("id");
        var name = reader.RequiredString("name");
        var isActive = reader.RequiredBool("isActive");
        var startDate = reader.RequiredDate("startDate");
        var goal = reader.OptionalString("goal");
        var goalDate = reader.OptionalDate("goalDate");
        var weeklyTarget = reader.RequiredInt("weeklyTarget", min: 1);
        var streakMin = reader.RequiredInt("streakMinWorkoutsPerWeek", min: 1);
        var streakCountsCustom = reader.RequiredBool("streakCountsCustomWorkouts");
        var increment = reader.RequiredPositiveDouble("weightIncrementKg");

        if (goalDate is { } goalOn && startDate != default && goalOn < startDate)
        {
            var path = reader.ChildPath("goalDate");
            context.Sink.Error(path, "out_of_range",
                $"{path} must be on or after the start date {startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)} (it was {goalOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}).");
        }

        var sessions = ReadSessions(reader, context);
        var phases = ReadPhases(reader, context, context.PlanSessionIds);
        reader.WarnAboutUnknownFields();

        return new Plan(id, name, isActive, startDate, goal, goalDate, weeklyTarget, streakMin, streakCountsCustom, increment, phases, sessions);
    }

    private static List<Session> ReadSessions(FieldReader plan, Context context)
    {
        var sessions = new List<Session>();
        context.PlanSessionIds = [];
        foreach (var (reader, _) in plan.OptionalObjectArray("sessions") ?? [])
        {
            var errorsBefore = context.Sink.Errors.Count;
            var id = reader.RequiredId("id");
            var name = reader.RequiredString("name");
            var day = reader.RequiredEnum<DayOfWeek>("defaultDay");
            var type = reader.RequiredEnum<SessionType>("type");
            var interval = reader.OptionalInt("benchmarkIntervalWeeks", min: 1);
            var rows = ReadRows(reader, context);
            reader.WarnAboutUnknownFields();

            context.CheckUnique(context.SessionIds, reader, "Session", id);
            if (id.Length > 0)
            {
                // Remembered even if the Session itself has errors, so one bad Session doesn't cause "unknown Session" noise.
                context.PlanSessionIds.Add(id);
            }

            if (context.Sink.Errors.Count == errorsBefore)
            {
                sessions.Add(new Session(id, name, day, type, interval, rows));
            }
        }

        return sessions;
    }

    private static List<SessionExercise> ReadRows(FieldReader parent, Context context) =>
        SessionRowReader.ReadRows(parent, "exercises", context.Sink, context.ExerciseIds);

    private static List<Phase> ReadPhases(FieldReader plan, Context context, HashSet<string> sessionIds)
    {
        var phases = new List<(Phase Phase, FieldReader Reader)>();
        foreach (var (reader, _) in plan.OptionalObjectArray("phases") ?? [])
        {
            var errorsBefore = context.Sink.Errors.Count;
            var id = reader.RequiredId("id");
            var focus = reader.RequiredString("focus");
            var start = reader.RequiredInt("startWeek", min: 1);
            var end = reader.RequiredInt("endWeek", min: Math.Max(start, 1));
            var weeksAreValid = context.Sink.Errors.Count == errorsBefore;
            var goals = ReadGoals(reader, context);
            var overrides = ReadOverrides(reader, context, sessionIds);
            reader.WarnAboutUnknownFields();

            context.CheckUnique(context.PhaseIds, reader, "Phase", id);
            // A Phase with other problems still takes part in the week-range check, so one bad field doesn't cause gap noise.
            if (weeksAreValid)
            {
                phases.Add((new Phase(id, focus, start, end, goals, overrides), reader));
            }
        }

        CheckWeekRanges(phases, context.Sink);
        return phases.Select(p => p.Phase).ToList();
    }

    /// <summary>Phases may be listed in any order but, by week, must start at week 1 and follow each other with no gap and no overlap.</summary>
    private static void CheckWeekRanges(List<(Phase Phase, FieldReader Reader)> phases, IssueSink sink)
    {
        var expectedStart = 1;
        foreach (var (phase, reader) in phases.OrderBy(p => p.Phase.StartWeek))
        {
            var path = reader.ChildPath("startWeek");
            if (phase.StartWeek > expectedStart)
            {
                var missing = phase.StartWeek - 1 == expectedStart ? $"week {expectedStart}" : $"weeks {expectedStart} to {phase.StartWeek - 1}";
                sink.Error(path, "phase_gap",
                    $"{path} is {phase.StartWeek}, which leaves {missing} in no Phase. Phases must cover every week from week 1 with no gaps.");
            }
            else if (phase.StartWeek < expectedStart)
            {
                sink.Error(path, "phase_overlap",
                    $"{path} is {phase.StartWeek}, but week {phase.StartWeek} already belongs to an earlier Phase. Phase week ranges must not overlap.");
            }

            expectedStart = Math.Max(expectedStart, phase.EndWeek + 1);
        }
    }

    private static List<PhaseGoal> ReadGoals(FieldReader phase, Context context)
    {
        var goals = new List<PhaseGoal>();
        foreach (var (reader, _) in phase.OptionalObjectArray("goals") ?? [])
        {
            var errorsBefore = context.Sink.Errors.Count;
            var exerciseId = ReadExerciseReference(reader, context);
            var weightKg = reader.RequiredPositiveDouble("weightKg");
            var reps = reader.RequiredInt("reps", min: 1);
            reader.WarnAboutUnknownFields();

            if (context.Sink.Errors.Count == errorsBefore)
            {
                goals.Add(new PhaseGoal(exerciseId, weightKg, reps));
            }
        }

        return goals;
    }

    private static List<SessionOverride> ReadOverrides(FieldReader phase, Context context, HashSet<string> sessionIds)
    {
        var overrides = new List<SessionOverride>();
        var seen = new HashSet<string>();
        foreach (var (reader, _) in phase.OptionalObjectArray("sessionOverrides") ?? [])
        {
            var errorsBefore = context.Sink.Errors.Count;
            var sessionId = reader.RequiredId("sessionId");
            var rows = ReadRows(reader, context);
            reader.WarnAboutUnknownFields();

            var path = reader.ChildPath("sessionId");
            if (sessionId.Length > 0 && !sessionIds.Contains(sessionId))
            {
                context.Sink.Error(path, "unknown_session", $"{path} \"{sessionId}\" is not a Session in this Plan.");
            }
            else if (sessionId.Length > 0 && !seen.Add(sessionId))
            {
                context.Sink.Error(path, "duplicate_id", $"{path} \"{sessionId}\" is overridden more than once in this Phase.");
            }

            if (context.Sink.Errors.Count == errorsBefore)
            {
                overrides.Add(new SessionOverride(sessionId, rows));
            }
        }

        return overrides;
    }

    private static string ReadExerciseReference(FieldReader reader, Context context) =>
        SessionRowReader.ReadExerciseReference(reader, "exerciseId", context.Sink, context.ExerciseIds);

    /// <summary>Shared state for one read: where errors go, the Exercise IDs references are checked against, and IDs seen so far.</summary>
    private sealed class Context(IssueSink sink, HashSet<string>? exerciseIds)
    {
        public IssueSink Sink { get; } = sink;

        /// <summary>Null when the document has no Exercise library, which switches reference checking off.</summary>
        public HashSet<string>? ExerciseIds { get; } = exerciseIds;

        public HashSet<string> SessionIds { get; } = [];

        /// <summary>IDs of the Sessions of the Plan being read.</summary>
        public HashSet<string> PlanSessionIds { get; set; } = [];

        public HashSet<string> PhaseIds { get; } = [];

        public void CheckUnique(HashSet<string> seen, FieldReader reader, string kind, string id)
        {
            if (id.Length > 0 && !seen.Add(id))
            {
                var path = reader.ChildPath("id");
                Sink.Error(path, "duplicate_id", $"{path} \"{id}\" is used by more than one {kind}. Every {kind} needs its own ID.");
            }
        }
    }
}
