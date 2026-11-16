using System.Globalization;
using Core.Model;

namespace Core.Serialisation;

/// <summary>
/// Reads the <c>plans</c> collection of a version 1 document: Plans, their Phases and Sessions. Structure only: unique IDs, Exercise and
/// Session references, the single active Plan and Phase week coverage are cross-record rules owned by <see cref="DocumentValidator"/>.
/// A record with a bad field is still returned (the bad field holds a placeholder) so the validator sees its IDs.
/// </summary>
internal static class PlanReader
{
    public static List<Plan>? Read(FieldReader root) =>
        root.OptionalObjectArray("plans")?
            .Select(item => item.Reader.ReadRecord(ReadPlan))
            .OfType<Plan>()
            .ToList();

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

        var sessions = ReadSessions(reader);
        var phases = ReadPhases(reader);

        return new Plan(id, name, isActive, startDate, goal, goalDate, weeklyTarget, streakMin, streakCountsCustom, increment, phases, sessions);
    }

    private static List<Session> ReadSessions(FieldReader plan) =>
        (plan.OptionalObjectArray("sessions") ?? [])
            .Select(item => item.Reader.ReadRecord(reader => new Session(
                reader.RequiredId("id"),
                reader.RequiredString("name"),
                reader.RequiredEnum<DayOfWeek>("defaultDay"),
                reader.RequiredEnum<SessionType>("type"),
                reader.OptionalInt("benchmarkIntervalWeeks", min: 1),
                SessionRowReader.ReadRows(reader, "exercises"))))
            .OfType<Session>()
            .ToList();

    private static List<Phase> ReadPhases(FieldReader plan) =>
        (plan.OptionalObjectArray("phases") ?? [])
            .Select(item => item.Reader.ReadRecord(ReadPhase))
            .OfType<Phase>()
            .ToList();

    private static Phase ReadPhase(FieldReader reader)
    {
        var id = reader.RequiredId("id");
        var focus = reader.RequiredString("focus");
        var start = reader.RequiredInt("startWeek", min: 1);
        var end = reader.RequiredInt("endWeek", min: Math.Max(start, 1));
        return new Phase(id, focus, start, end, ReadGoals(reader), ReadOverrides(reader));
    }

    private static List<PhaseGoal> ReadGoals(FieldReader phase) =>
        (phase.OptionalObjectArray("goals") ?? [])
            .Select(item => item.Reader.ReadRecord(reader => new PhaseGoal(
                reader.RequiredId("exerciseId"),
                reader.RequiredPositiveDouble("weightKg"),
                reader.RequiredInt("reps", min: 1))))
            .OfType<PhaseGoal>()
            .ToList();

    private static List<SessionOverride> ReadOverrides(FieldReader phase) =>
        (phase.OptionalObjectArray("sessionOverrides") ?? [])
            .Select(item => item.Reader.ReadRecord(reader => new SessionOverride(
                reader.RequiredId("sessionId"),
                SessionRowReader.ReadRows(reader, "exercises"))))
            .OfType<SessionOverride>()
            .ToList();
}
