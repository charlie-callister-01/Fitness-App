using Core.Model;

namespace Core.Serialisation;

/// <summary>
/// Reads the <c>plans</c> collection of a version 1 document: Plans, their Phases and Sessions. Structure only: unique IDs, Exercise and
/// Session references, the single active Plan and Phase week ranges and coverage are cross-record rules owned by <see cref="DocumentValidator"/>.
/// A record with a bad field is still returned (the bad field holds a placeholder) so the validator sees its IDs; what could not be read is
/// noted in <see cref="ParseFindings"/>.
/// </summary>
internal static class PlanReader
{
    public static List<Plan>? Read(FieldReader root) => root.ReadRecords("plans", ReadPlan);

    private static Plan ReadPlan(FieldReader reader)
    {
        var id = reader.RequiredId("id");
        var name = reader.RequiredString("name");
        var isActive = reader.Attempt(() => reader.RequiredBool("isActive"));
        if (!isActive.IsValid)
        {
            reader.Context.Findings.MarkActiveFlagUnreadable();
        }

        var startDate = reader.RequiredDateOrNull("startDate");
        var goal = reader.OptionalString("goal");
        var goalDate = reader.OptionalDate("goalDate");
        var weeklyTarget = reader.RequiredInt("weeklyTarget", min: 1);
        var streakMin = reader.RequiredInt("streakMinWorkoutsPerWeek", min: 1);
        var streakCountsCustom = reader.RequiredBool("streakCountsCustomWorkouts");
        var increment = reader.RequiredPositiveDouble("weightIncrementKg");
        reader.CheckDateOrder("startDate", "goalDate", startDate, goalDate, code: "out_of_range");

        var sessions = ReadSessions(reader);
        var phases = reader.ReadRecords("phases", ReadPhase) ?? [];

        return new Plan(id, name, isActive.Value, startDate ?? default, goal, goalDate, weeklyTarget, streakMin, streakCountsCustom, increment, phases, sessions);
    }

    private static List<Session> ReadSessions(FieldReader plan) =>
        plan.ReadRecords("sessions", reader => new Session(
            reader.RequiredId("id"),
            reader.RequiredString("name"),
            reader.RequiredEnum<DayOfWeek>("defaultDay"),
            reader.RequiredEnum<SessionType>("type"),
            reader.OptionalInt("benchmarkIntervalWeeks", min: 1),
            SessionRowReader.ReadRows(reader, "exercises"))) ?? [];

    /// <summary>
    /// Each week number is checked on its own (a whole number of 1 or more). Whether the range runs forwards is the validator's rule,
    /// so a Phase built in memory is checked the same way; a Phase whose weeks failed to read is noted so it is not judged twice.
    /// </summary>
    private static Phase ReadPhase(FieldReader reader)
    {
        var id = reader.RequiredId("id");
        var focus = reader.RequiredString("focus");
        var weeks = reader.Attempt(() => (Start: reader.RequiredInt("startWeek", min: 1), End: reader.RequiredInt("endWeek", min: 1)));
        var phase = new Phase(id, focus, weeks.Value.Start, weeks.Value.End, ReadGoals(reader), ReadOverrides(reader));
        if (!weeks.IsValid)
        {
            reader.Context.Findings.MarkWeeksUnreadable(phase);
        }

        return phase;
    }

    private static List<PhaseGoal> ReadGoals(FieldReader phase) =>
        phase.ReadRecords("goals", reader => new PhaseGoal(
            reader.RequiredId("exerciseId"),
            reader.RequiredPositiveDouble("weightKg"),
            reader.RequiredInt("reps", min: 1))) ?? [];

    private static List<SessionOverride> ReadOverrides(FieldReader phase) =>
        phase.ReadRecords("sessionOverrides", reader => new SessionOverride(
            reader.RequiredId("sessionId"),
            SessionRowReader.ReadRows(reader, "exercises"))) ?? [];
}
