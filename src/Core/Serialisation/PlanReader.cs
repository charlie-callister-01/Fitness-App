using System.Globalization;
using Core.Model;

namespace Core.Serialisation;

/// <summary>
/// Reads and validates the <c>plans</c> collection of a version 1 document: Plans, their Phases and Sessions, and every cross-reference
/// between them. Exercise references are checked against the document's Exercise library when it has one (the context's Exercise scope);
/// a Plan-only document (no <c>exercises</c>) can't be checked here, so its Exercise IDs are resolved when it is imported.
/// </summary>
internal static class PlanReader
{
    /// <summary>The fields that decide whether a Phase takes part in the week-range check.</summary>
    private readonly record struct PhaseHeader(string Id, string Focus, int StartWeek, int EndWeek);

    public static List<Plan>? Read(FieldReader root)
    {
        var items = root.OptionalObjectArray("plans");
        if (items is null)
        {
            return null;
        }

        var plans = new List<Plan>();
        var activePlans = new List<(int Index, string Id)>();
        foreach (var (reader, index) in items)
        {
            var plan = reader.ReadRecord(r =>
            {
                var read = ReadPlan(r);
                r.CheckUnique("Plan", "id", read.Id);
                if (read.IsActive)
                {
                    // Counted even when the Plan has other errors, so one bad field doesn't also cause "no active plan".
                    activePlans.Add((index, read.Id));
                }

                return read;
            });

            if (plan is not null)
            {
                plans.Add(plan);
            }
        }

        CheckExactlyOneActive(items.Count, activePlans, root.Context.Sink);
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

    private static Plan ReadPlan(FieldReader reader)
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
            reader.Report("goalDate", "out_of_range",
                $"{reader.ChildPath("goalDate")} must be on or after the start date {startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)} (it was {goalOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}).");
        }

        // Session IDs are collected even from Sessions with errors, so one bad Session doesn't cause "unknown Session" noise in overrides.
        var sessionIds = new HashSet<string>();
        var sessions = ReadSessions(reader, sessionIds);
        var phases = ReadPhases(reader, ReferenceScope.ForSessionsInPlan(sessionIds));

        return new Plan(id, name, isActive, startDate, goal, goalDate, weeklyTarget, streakMin, streakCountsCustom, increment, phases, sessions);
    }

    private static List<Session> ReadSessions(FieldReader plan, HashSet<string> planSessionIds) =>
        (plan.OptionalObjectArray("sessions") ?? [])
            .Select(item => item.Reader.ReadRecord(reader =>
            {
                var id = reader.RequiredId("id");
                var name = reader.RequiredString("name");
                var day = reader.RequiredEnum<DayOfWeek>("defaultDay");
                var type = reader.RequiredEnum<SessionType>("type");
                var interval = reader.OptionalInt("benchmarkIntervalWeeks", min: 1);
                var rows = SessionRowReader.ReadRows(reader, "exercises");

                reader.CheckUnique("Session", "id", id);
                if (id.Length > 0)
                {
                    planSessionIds.Add(id);
                }

                return new Session(id, name, day, type, interval, rows);
            }))
            .OfType<Session>()
            .ToList();

    private static List<Phase> ReadPhases(FieldReader plan, ReferenceScope planSessions)
    {
        var phases = new List<Phase>();
        var rangedPhases = new List<(PhaseHeader Header, FieldReader Reader)>();
        foreach (var (phaseReader, _) in plan.OptionalObjectArray("phases") ?? [])
        {
            var phase = phaseReader.ReadRecord(reader =>
            {
                var header = reader.Attempt(() =>
                {
                    var id = reader.RequiredId("id");
                    var focus = reader.RequiredString("focus");
                    var start = reader.RequiredInt("startWeek", min: 1);
                    var end = reader.RequiredInt("endWeek", min: Math.Max(start, 1));
                    return new PhaseHeader(id, focus, start, end);
                });
                var goals = ReadGoals(reader);
                var overrides = ReadOverrides(reader, planSessions);

                reader.CheckUnique("Phase", "id", header.Value.Id);
                // A Phase with a bad goal or override still takes part in the week-range check, so that bad field doesn't cause gap noise.
                if (header.IsValid)
                {
                    rangedPhases.Add((header.Value, reader));
                }

                return new Phase(header.Value.Id, header.Value.Focus, header.Value.StartWeek, header.Value.EndWeek, goals, overrides);
            });

            if (phase is not null)
            {
                phases.Add(phase);
            }
        }

        CheckWeekRanges(rangedPhases, plan.Context.Sink);
        return phases;
    }

    /// <summary>Phases may be listed in any order but, by week, must start at week 1 and follow each other with no gap and no overlap.</summary>
    private static void CheckWeekRanges(List<(PhaseHeader Header, FieldReader Reader)> phases, IssueSink sink)
    {
        var expectedStart = 1;
        foreach (var (phase, reader) in phases.OrderBy(p => p.Header.StartWeek))
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

    private static List<PhaseGoal> ReadGoals(FieldReader phase) =>
        (phase.OptionalObjectArray("goals") ?? [])
            .Select(item => item.Reader.ReadRecord(reader => new PhaseGoal(
                reader.RequiredExercise("exerciseId"),
                reader.RequiredPositiveDouble("weightKg"),
                reader.RequiredInt("reps", min: 1))))
            .OfType<PhaseGoal>()
            .ToList();

    private static List<SessionOverride> ReadOverrides(FieldReader phase, ReferenceScope planSessions)
    {
        var seen = new HashSet<string>();
        return (phase.OptionalObjectArray("sessionOverrides") ?? [])
            .Select(item => item.Reader.ReadRecord(reader =>
            {
                var sessionId = reader.RequiredReference("sessionId", planSessions);
                var rows = SessionRowReader.ReadRows(reader, "exercises");

                if (sessionId.Length > 0 && !seen.Add(sessionId))
                {
                    reader.Report("sessionId", "duplicate_id", $"{reader.ChildPath("sessionId")} \"{sessionId}\" is overridden more than once in this Phase.");
                }

                return new SessionOverride(sessionId, rows);
            }))
            .OfType<SessionOverride>()
            .ToList();
    }
}
