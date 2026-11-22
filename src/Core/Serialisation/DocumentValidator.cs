using System.Globalization;
using Core.Model;
using Core.Rules.Phases;

namespace Core.Serialisation;

/// <summary>
/// The validate phase: every cross-record rule of the exchange format, run over an <see cref="ExchangeDocument"/>. Pure: it reads the document
/// and returns issues, so it works on a document parsed from JSON and on one built in memory (an Import, or the app's own state).
/// <para>
/// It tolerates the placeholders the parse phase leaves behind for fields that failed to read: blank IDs are skipped, and the placeholders
/// listed in <see cref="ParseFindings"/> (a Phase's weeks, a Plan's active flag) are left alone, so nothing is reported twice.
/// Exercise and Session references are checked only when the document contains the referenced collection;
/// otherwise they are resolved when the document is imported.
/// </para>
/// </summary>
internal static class DocumentValidator
{
    public static IReadOnlyList<FormatIssue> Validate(ExchangeDocument document) => Validate(document, ParseFindings.None);

    /// <summary>Validates a document the parse phase produced: <paramref name="findings"/> says which placeholders were unreadable.</summary>
    public static IReadOnlyList<FormatIssue> Validate(ExchangeDocument document, ParseFindings findings)
    {
        var checker = new Checker(document, findings);
        checker.CheckExercises();
        checker.CheckBodyAndGuides();
        checker.CheckPlans();
        checker.CheckWorkouts();
        checker.CheckWeekSchedules();
        return checker.Issues;
    }

    /// <summary>The kinds of record whose IDs must be unique. The label is the name used in messages.</summary>
    private enum RecordKind
    {
        Exercise,
        WeighIn,
        Break,
        Badge,
        Guide,
        Plan,
        Track,
        Session,
        Phase,
        Workout,
        Set,
        PainCheck,
        WeekSchedule,
    }

    private static string Label(RecordKind kind) => kind switch
    {
        RecordKind.WeekSchedule => "Week schedule",
        RecordKind.WeighIn => "Weigh-in",
        RecordKind.PainCheck => "Pain check",
        _ => kind.ToString(),
    };

    /// <summary>A real ID: not null and not the blank placeholder the parse phase leaves for an unreadable one.</summary>
    private static bool IsSet(string? id) => id is { Length: > 0 };

    /// <summary>
    /// The IDs a reference may point at. Unchecked when the document doesn't contain the referenced collection (it is resolved on import),
    /// so nothing is unknown; otherwise any ID outside the set is.
    /// </summary>
    private sealed class ReferenceScope
    {
        private static readonly ReferenceScope Unchecked = new(null);
        private readonly HashSet<string>? _known;

        private ReferenceScope(HashSet<string>? known) => _known = known;

        public static ReferenceScope Of(IEnumerable<string>? ids) => ids is null ? Unchecked : new(ids.ToHashSet());

        public bool IsUnknown(string id) => _known is not null && !_known.Contains(id);
    }

    /// <summary>The Exercise a skipped or swapped entry names, which must come from the Workout's planned rows.</summary>
    private sealed record PlannedReference(string Field, string? ExerciseId, string Verb);

    private sealed class Checker
    {
        private readonly ExchangeDocument _document;
        private readonly ParseFindings _findings;
        private readonly ReferenceScope _exercises;
        private readonly ReferenceScope _sessions;
        private readonly HashSet<string> _dailySessionIds;
        private readonly Dictionary<RecordKind, HashSet<string>> _seenIds = [];

        public Checker(ExchangeDocument document, ParseFindings findings)
        {
            _document = document;
            _findings = findings;
            _exercises = ReferenceScope.Of(document.Exercises?.Select(e => e.Id));
            _sessions = ReferenceScope.Of(document.Plans?.SelectMany(p => p.Sessions).Select(s => s.Id));
            _dailySessionIds = CollectDailySessionIds(document.Plans);
        }

        public List<FormatIssue> Issues { get; } = [];

        private void Error(string path, string code, string message) => Issues.Add(new FormatIssue(path, code, message));

        /// <summary>IDs are unique per record kind across the whole document. Blank IDs (unreadable in the parse phase) are skipped.</summary>
        private void CheckUnique(RecordKind kind, string path, string field, string id)
        {
            if (!IsSet(id))
            {
                return;
            }

            if (!_seenIds.TryGetValue(kind, out var seen))
            {
                seen = [];
                _seenIds[kind] = seen;
            }

            if (!seen.Add(id))
            {
                var what = field == "id" ? "ID" : field;
                Error(path, "duplicate_id", $"{path} \"{id}\" is used by more than one {Label(kind)}. Every {Label(kind)} needs its own {what}.");
            }
        }

        private bool IsUnknownExercise(string? id) => IsSet(id) && _exercises.IsUnknown(id!);

        private void CheckExercise(string path, string? id)
        {
            if (IsUnknownExercise(id))
            {
                Error(path, "unknown_exercise", $"{path} \"{id}\" is not in the Exercise library. Add it to exercises or use an existing ID.");
            }
        }

        public void CheckExercises()
        {
            foreach (var (exercise, i) in Indexed(_document.Exercises))
            {
                CheckUnique(RecordKind.Exercise, $"exercises[{i}].id", "id", exercise.Id);
            }
        }

        public void CheckBodyAndGuides()
        {
            foreach (var (weighIn, i) in Indexed(_document.WeighIns))
            {
                CheckUnique(RecordKind.WeighIn, $"weighIns[{i}].id", "id", weighIn.Id);
            }

            foreach (var (b, i) in Indexed(_document.Breaks))
            {
                CheckUnique(RecordKind.Break, $"breaks[{i}].id", "id", b.Id);
            }

            foreach (var (badge, i) in Indexed(_document.Badges))
            {
                CheckUnique(RecordKind.Badge, $"badges[{i}].id", "id", badge.Id);
            }

            foreach (var (guide, i) in Indexed(_document.Guides))
            {
                var path = $"guides[{i}].exerciseId";
                CheckExercise(path, guide.ExerciseId);
                CheckUnique(RecordKind.Guide, path, "exerciseId", guide.ExerciseId);
            }
        }

        public void CheckPlans()
        {
            if (_document.Plans is not { Count: > 0 } plans)
            {
                return;
            }

            foreach (var (plan, i) in Indexed(plans))
            {
                CheckUnique(RecordKind.Plan, $"plans[{i}].id", "id", plan.Id);
                CheckTracks(plan, i);
                CheckSessions(plan, i);
            }

            CheckExactlyOneActive(plans);
        }

        private void CheckExactlyOneActive(IReadOnlyList<Plan> plans)
        {
            var active = plans.Select((plan, index) => (plan, index)).Where(p => p.plan.IsActive).ToList();
            if (active.Count == 0)
            {
                if (!_findings.ActiveFlagUnreadable)
                {
                    Error("plans", "no_active_plan", "plans must contain exactly one active Plan, but none has \"isActive\": true.");
                }

                return;
            }

            foreach (var (_, index) in active.Skip(1))
            {
                Error($"plans[{index}].isActive", "multiple_active_plans",
                    $"Only one Plan can be active, but \"{active[0].plan.Id}\" is already active. Set isActive to false on all but one.");
            }
        }

        private void CheckTracks(Plan plan, int planIndex)
        {
            if (plan.Tracks.Count == 0)
            {
                if (!_findings.AreTracksMissing(plan))
                {
                    Error($"plans[{planIndex}].tracks", "missing_field",
                        $"plans[{planIndex}].tracks is required and must contain at least one Track.");
                }

                return;
            }

            if (plan.Tracks[0].Rhythm is TrackRhythm.Daily && !_findings.IsCadenceUnreadable(plan.Tracks[0]))
            {
                Error($"plans[{planIndex}].tracks[0].cadence", "invalid_value",
                    $"plans[{planIndex}].tracks[0].cadence must be weekly: the first Track is the main Track.");
            }

            foreach (var (track, t) in Indexed(plan.Tracks))
            {
                var trackPath = $"plans[{planIndex}].tracks[{t}]";
                CheckUnique(RecordKind.Track, $"{trackPath}.id", "id", track.Id);
                CheckPhases(plan, track, trackPath);
            }
        }

        private void CheckSessions(Plan plan, int planIndex)
        {
            var tracksById = TracksById(plan);
            var canCheckUnknownTrack = plan.Tracks.Count > 0 && plan.Tracks.All(t => IsSet(t.Id));
            var mainTrack = plan.Tracks.FirstOrDefault();

            foreach (var (session, i) in Indexed(plan.Sessions))
            {
                var path = $"plans[{planIndex}].sessions[{i}]";
                CheckUnique(RecordKind.Session, $"{path}.id", "id", session.Id);
                CheckRows(session.Exercises, $"{path}.exercises");

                if (!IsSet(session.TrackId))
                {
                    continue;
                }

                if (!tracksById.TryGetValue(session.TrackId, out var track))
                {
                    if (canCheckUnknownTrack)
                    {
                        Error($"{path}.trackId", "unknown_track", $"{path}.trackId \"{session.TrackId}\" is not a Track in this Plan.");
                    }

                    continue;
                }

                if (!_findings.IsDefaultDayUnreadable(session) && !_findings.IsCadenceUnreadable(track))
                {
                    if (track.Rhythm is TrackRhythm.Daily && session.DefaultDay != DefaultDay.Daily)
                    {
                        Error($"{path}.defaultDay", "invalid_value", $"{path}.defaultDay must be daily on a daily Track.");
                    }
                    else if (track.Rhythm is TrackRhythm.Weekly && session.DefaultDay == DefaultDay.Daily)
                    {
                        Error($"{path}.defaultDay", "invalid_value",
                            $"{path}.defaultDay cannot be daily on a weekly Track. Choose a day from monday to sunday.");
                    }
                }

                if (session.BenchmarkIntervalWeeks is not null && !ReferenceEquals(track, mainTrack))
                {
                    Error($"{path}.trackId", "invalid_value",
                        $"{path}.trackId \"{session.TrackId}\" cannot hold a benchmark Session. Benchmark Sessions must be on the main Track (\"{mainTrack!.Id}\").");
                }
            }
        }

        private void CheckRows(IReadOnlyList<SessionExercise> rows, string path)
        {
            foreach (var (row, i) in Indexed(rows))
            {
                CheckExercise($"{path}[{i}].exerciseId", row.ExerciseId);
            }
        }

        private void CheckPhases(Plan plan, Track track, string trackPath)
        {
            var trackSessionIds = IsSet(track.Id)
                ? plan.Sessions.Where(s => s.TrackId == track.Id).Select(s => s.Id).ToHashSet()
                : null;
            var findings = PhaseRanges.Check(track.Phases, _findings.AreWeeksUnreadable);
            foreach (var (phase, i) in Indexed(track.Phases))
            {
                var path = $"{trackPath}.phases[{i}]";
                CheckUnique(RecordKind.Phase, $"{path}.id", "id", phase.Id);
                foreach (var finding in findings.Where(f => f.Index == i && f.Problem is PhaseRangeProblem.BadStart or PhaseRangeProblem.EndBeforeStart))
                {
                    ReportRange(finding, path);
                }

                foreach (var (goal, g) in Indexed(phase.Goals))
                {
                    CheckExercise($"{path}.goals[{g}].exerciseId", goal.ExerciseId);
                }

                CheckOverrides(phase, path, track.Id, trackSessionIds);
            }

            foreach (var finding in findings.Where(f => f.Problem is PhaseRangeProblem.Overlap or PhaseRangeProblem.Gap or PhaseRangeProblem.LeadingGap))
            {
                ReportRange(finding, $"{trackPath}.phases[{finding.Index}]");
            }
        }

        /// <summary>Turns a finding of the shared Phase week-range rule (<see cref="PhaseRanges"/>) into this document's issue.</summary>
        private void ReportRange(PhaseRangeFinding finding, string path)
        {
            var phase = finding.Phase;
            switch (finding.Problem)
            {
                case PhaseRangeProblem.BadStart:
                    Error($"{path}.startWeek", "out_of_range", $"{path}.startWeek must be 1 or more (it was {phase.StartWeek}).");
                    break;
                case PhaseRangeProblem.EndBeforeStart:
                    Error($"{path}.endWeek", "out_of_range", $"{path}.endWeek must be {phase.StartWeek} or more (it was {phase.EndWeek}).");
                    break;
                case PhaseRangeProblem.Overlap:
                    Error($"{path}.startWeek", "phase_overlap", $"{path}.startWeek is {phase.StartWeek}, but week {phase.StartWeek} already belongs to an earlier Phase. Phase week ranges must not overlap.");
                    break;
                default:
                    var missing = finding.FirstWeek == finding.LastWeek ? $"week {finding.FirstWeek}" : $"weeks {finding.FirstWeek} to {finding.LastWeek}";
                    Error($"{path}.startWeek", "phase_gap", $"{path}.startWeek is {phase.StartWeek}, which leaves {missing} in no Phase. Phases must cover every week from week 1 with no gaps.");
                    break;
            }
        }

        private void CheckOverrides(Phase phase, string phasePath, string trackId, HashSet<string>? trackSessionIds)
        {
            var overridden = new HashSet<string>();
            foreach (var (item, i) in Indexed(phase.SessionOverrides))
            {
                var path = $"{phasePath}.sessionOverrides[{i}]";
                var sessionPath = $"{path}.sessionId";
                if (IsSet(item.SessionId) && trackSessionIds is not null && !trackSessionIds.Contains(item.SessionId))
                {
                    Error(sessionPath, "unknown_session", $"{sessionPath} \"{item.SessionId}\" is not a Session on Track \"{trackId}\".");
                }

                if (IsSet(item.SessionId) && !overridden.Add(item.SessionId))
                {
                    Error(sessionPath, "duplicate_id", $"{sessionPath} \"{item.SessionId}\" is overridden more than once in this Phase.");
                }

                CheckRows(item.Exercises, $"{path}.exercises");
            }
        }

        public void CheckWorkouts()
        {
            foreach (var (workout, i) in Indexed(_document.Workouts))
            {
                var path = $"workouts[{i}]";
                CheckUnique(RecordKind.Workout, $"{path}.id", "id", workout.Id);
                CheckWorkoutSession(workout, path);
                CheckRows(workout.Planned, $"{path}.planned");
                CheckEntries(workout, path);
                CheckPainCheck(workout.PainCheck, path);
            }
        }

        private void CheckWorkoutSession(Workout workout, string path)
        {
            if (IsSet(workout.SessionId) && _sessions.IsUnknown(workout.SessionId!))
            {
                Error($"{path}.sessionId", "unknown_session", $"{path}.sessionId \"{workout.SessionId}\" is not a Session in any Plan in this document.");
            }
        }

        private void CheckEntries(Workout workout, string workoutPath)
        {
            var planned = workout.Planned.Select(r => r.ExerciseId).ToHashSet();
            foreach (var (entry, i) in Indexed(workout.Entries))
            {
                var path = $"{workoutPath}.entries[{i}]";
                CheckExercise($"{path}.exerciseId", entry.ExerciseId);
                CheckExercise($"{path}.swappedFromExerciseId", entry.SwappedFromExerciseId);
                CheckAgainstPlanned(entry, path, planned);

                foreach (var (set, s) in Indexed(entry.Sets))
                {
                    CheckUnique(RecordKind.Set, $"{path}.sets[{s}].id", "id", set.Id);
                }
            }
        }

        /// <summary>
        /// Once a Workout has a planned snapshot, a skipped entry must be one of its planned Exercises and a swapped entry must replace one.
        /// An unknown Exercise is already reported, so it is not reported a second time here.
        /// </summary>
        private void CheckAgainstPlanned(WorkoutEntry entry, string path, HashSet<string> planned)
        {
            if (planned.Count == 0 || PlannedReferenceOf(entry) is not { } reference)
            {
                return;
            }

            if (IsSet(reference.ExerciseId) && !planned.Contains(reference.ExerciseId!) && !IsUnknownExercise(reference.ExerciseId))
            {
                Error($"{path}.{reference.Field}", "not_in_planned",
                    $"{path}.{reference.Field} \"{reference.ExerciseId}\" is not in this Workout's planned rows, so it can't be {reference.Verb}. Name an Exercise from planned.");
            }
        }

        private static PlannedReference? PlannedReferenceOf(WorkoutEntry entry) => entry.Status switch
        {
            EntryStatus.Skipped => new PlannedReference("exerciseId", entry.ExerciseId, "skipped"),
            EntryStatus.Swapped => new PlannedReference("swappedFromExerciseId", entry.SwappedFromExerciseId, "swapped"),
            _ => null,
        };

        private void CheckPainCheck(PainCheck? pain, string workoutPath)
        {
            if (pain is null)
            {
                return;
            }

            var path = $"{workoutPath}.painCheck";
            CheckUnique(RecordKind.PainCheck, $"{path}.id", "id", pain.Id);
            CheckExercise($"{path}.exerciseId", pain.ExerciseId);
        }

        public void CheckWeekSchedules()
        {
            foreach (var (week, i) in Indexed(_document.WeekSchedules))
            {
                var path = $"weekSchedules[{i}]";
                CheckUnique(RecordKind.WeekSchedule, $"{path}.weekStart", "weekStart", week.WeekStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                (DateOnly First, DateOnly Last)? validRange = null;
                if (week.WeekStart.DayOfWeek == DayOfWeek.Monday)
                {
                    validRange = (week.WeekStart, week.WeekStart.AddDays(6));
                }
                else
                {
                    Error($"{path}.weekStart", "invalid_value", $"{path}.weekStart ({week.WeekStart:yyyy-MM-dd}) must be a Monday, because weeks run Monday to Sunday.");
                }

                CheckMoves(week, path, validRange);
            }
        }

        /// <summary>A move's date is judged against <paramref name="validRange"/>, the keyed week's Monday to Sunday. It is null when the week doesn't start on a Monday (already reported), so no date is judged.</summary>
        private void CheckMoves(WeekSchedule week, string weekPath, (DateOnly First, DateOnly Last)? validRange)
        {
            var moved = new HashSet<string>();
            foreach (var (move, i) in Indexed(week.Moves))
            {
                var path = $"{weekPath}.moves[{i}]";
                if (IsSet(move.SessionId) && _sessions.IsUnknown(move.SessionId))
                {
                    Error($"{path}.sessionId", "unknown_session", $"{path}.sessionId \"{move.SessionId}\" is not a Session in any Plan in this document.");
                }
                else if (IsSet(move.SessionId) && _dailySessionIds.Contains(move.SessionId))
                {
                    Error($"{path}.sessionId", "invalid_value", $"{path}.sessionId \"{move.SessionId}\" is a daily Session and cannot be moved.");
                }

                if (IsSet(move.SessionId) && !moved.Add(move.SessionId))
                {
                    Error($"{path}.sessionId", "duplicate_id", $"{path}.sessionId \"{move.SessionId}\" is moved more than once in the week of {week.WeekStart:yyyy-MM-dd}. A Session can have one move per week.");
                }

                if (validRange is { } range && (move.Date < range.First || move.Date > range.Last))
                {
                    Error($"{path}.date", "out_of_range",
                        $"{path}.date ({move.Date:yyyy-MM-dd}) is outside the week of {week.WeekStart:yyyy-MM-dd}. A move must land between that Monday and the Sunday after it.");
                }
            }
        }

        private static Dictionary<string, Track> TracksById(Plan plan)
        {
            var byId = new Dictionary<string, Track>(StringComparer.Ordinal);
            foreach (var track in plan.Tracks)
            {
                if (IsSet(track.Id))
                {
                    byId.TryAdd(track.Id, track);
                }
            }

            return byId;
        }

        private static HashSet<string> CollectDailySessionIds(IReadOnlyList<Plan>? plans)
        {
            var daily = new HashSet<string>(StringComparer.Ordinal);
            foreach (var plan in plans ?? [])
            {
                var tracksById = TracksById(plan);
                foreach (var session in plan.Sessions)
                {
                    if (!IsSet(session.Id))
                    {
                        continue;
                    }

                    var onDailyTrack = IsSet(session.TrackId)
                        && tracksById.TryGetValue(session.TrackId, out var track)
                        && track.Rhythm is TrackRhythm.Daily;
                    if (onDailyTrack || session.DefaultDay == DefaultDay.Daily)
                    {
                        daily.Add(session.Id);
                    }
                }
            }

            return daily;
        }

        private static IEnumerable<(T Item, int Index)> Indexed<T>(IEnumerable<T>? items) =>
            (items ?? []).Select((item, index) => (item, index));
    }
}
