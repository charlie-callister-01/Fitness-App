using Core.Model;

namespace Core.Serialisation;

/// <summary>
/// Reads the <c>plans</c> collection of version 1 and version 2 documents: Plans, Tracks, Phases and Sessions.
/// Structure only: unique IDs, Exercise/Track/Session references, the single active Plan, main Track cadence, and Phase week ranges
/// are cross-record rules owned by <see cref="DocumentValidator"/>.
/// A record with a bad field is still returned (the bad field holds a placeholder) so the validator sees its IDs; what could not be read is
/// noted in <see cref="ParseFindings"/>.
/// </summary>
internal static class PlanReader
{
    private static readonly string[] WeeklyOnlyFields =
        ["weeklyTarget", "streakMinWorkoutsPerWeek", "streakCountsCustomWorkouts"];

    public static List<Plan>? ReadV1(FieldReader root) => root.ReadRecords("plans", ReadV1Plan);

    public static List<Plan>? ReadV2(FieldReader root) => root.ReadRecords("plans", ReadV2Plan);

    private sealed record PlanHeader(
        string Id,
        string Name,
        bool IsActive,
        DateOnly StartDate,
        string? Goal,
        DateOnly? GoalDate,
        double WeightIncrementKg);

    private static PlanHeader ReadHeader(FieldReader reader)
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
        var increment = reader.RequiredPositiveDouble("weightIncrementKg");
        reader.CheckDateOrder("startDate", "goalDate", startDate, goalDate, code: "out_of_range");

        return new PlanHeader(id, name, isActive.Value, startDate ?? default, goal, goalDate, increment);
    }

    private static Plan ReadV1Plan(FieldReader reader)
    {
        var header = ReadHeader(reader);
        var weeklyTarget = reader.RequiredInt("weeklyTarget", min: 1);
        var streakMin = reader.RequiredInt("streakMinWorkoutsPerWeek", min: 1);
        var streakCountsCustom = reader.RequiredBool("streakCountsCustomWorkouts");

        var sessions = ReadV1Sessions(reader);
        var phases = reader.ReadRecords("phases", ReadPhase) ?? [];

        return V1ToV2Upgrader.UpgradePlan(new V1Plan(
            header.Id, header.Name, header.IsActive, header.StartDate, header.Goal, header.GoalDate,
            weeklyTarget, streakMin, streakCountsCustom, header.WeightIncrementKg, phases, sessions));
    }

    private static Plan ReadV2Plan(FieldReader reader)
    {
        var header = ReadHeader(reader);
        var tracksAttempt = reader.Attempt(() => reader.ReadRecords("tracks", ReadTrack));
        var tracks = tracksAttempt.Value ?? [];

        var sessions = reader.ReadRecords("sessions", ReadV2Session) ?? [];
        var plan = new Plan(
            header.Id, header.Name, header.IsActive, header.StartDate, header.Goal, header.GoalDate,
            header.WeightIncrementKg, tracks, sessions);

        if (!tracksAttempt.IsValid && tracks.Count == 0)
        {
            reader.Context.Findings.MarkTracksMissing(plan);
        }

        return plan;
    }

    private static Track ReadTrack(FieldReader reader)
    {
        var id = reader.RequiredId("id");
        var name = reader.RequiredString("name");
        var cadence = reader.Attempt(() => reader.RequiredEnum<TrackCadence>("cadence"));

        if (!cadence.IsValid)
        {
            reader.Ignore([.. WeeklyOnlyFields, "dayDonePercent"]);
            var badPhases = reader.ReadRecords("phases", ReadPhase) ?? [];
            var badTrack = new Track(id, name, new TrackRhythm.Weekly(0, 0, false), badPhases);
            reader.Context.Findings.MarkCadenceUnreadable(badTrack);
            return badTrack;
        }

        TrackRhythm rhythm;
        if (cadence.Value == TrackCadence.Weekly)
        {
            var weeklyTarget = reader.RequiredInt("weeklyTarget", min: 1);
            var streakMin = reader.RequiredInt("streakMinWorkoutsPerWeek", min: 1);
            var streakCountsCustom = reader.RequiredBool("streakCountsCustomWorkouts");
            if (reader.Has("dayDonePercent"))
            {
                reader.Report("dayDonePercent", "invalid_value", $"{reader.ChildPath("dayDonePercent")} only applies to a daily Track.");
            }

            rhythm = new TrackRhythm.Weekly(weeklyTarget, streakMin, streakCountsCustom);
        }
        else
        {
            var dayDonePercent = reader.RequiredIntInRange("dayDonePercent", 1, 100);
            foreach (var field in WeeklyOnlyFields)
            {
                if (reader.Has(field))
                {
                    reader.Report(field, "invalid_value", $"{reader.ChildPath(field)} only applies to a weekly Track.");
                }
            }

            rhythm = new TrackRhythm.Daily(dayDonePercent);
        }

        var phases = reader.ReadRecords("phases", ReadPhase) ?? [];
        return new Track(id, name, rhythm, phases);
    }

    private static List<V1Session> ReadV1Sessions(FieldReader plan) =>
        plan.ReadRecords("sessions", reader => new V1Session(
            reader.RequiredId("id"),
            reader.RequiredString("name"),
            reader.RequiredEnum<DefaultDay>("defaultDay"),
            reader.RequiredEnum<SessionType>("type"),
            reader.OptionalInt("benchmarkIntervalWeeks", min: 1),
            SessionRowReader.ReadRows(reader, "exercises"))) ?? [];

    private static Session ReadV2Session(FieldReader reader)
    {
        var id = reader.RequiredId("id");
        var name = reader.RequiredString("name");
        var trackId = reader.RequiredId("trackId");
        var defaultDay = reader.Attempt(() => reader.RequiredEnum<DefaultDay>("defaultDay"));
        var type = reader.RequiredEnum<SessionType>("type");
        var benchmarkIntervalWeeks = reader.OptionalInt("benchmarkIntervalWeeks", min: 1);
        var exercises = SessionRowReader.ReadRows(reader, "exercises");

        var session = new Session(id, name, trackId, defaultDay.Value, type, benchmarkIntervalWeeks, exercises);
        if (!defaultDay.IsValid)
        {
            reader.Context.Findings.MarkDefaultDayUnreadable(session);
        }

        return session;
    }

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
