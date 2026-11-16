using Core.Model;

namespace Core.Serialisation;

/// <summary>
/// The validate phase: every cross-record rule of the exchange format, run over an <see cref="ExchangeDocument"/>. Pure: it reads the document
/// and returns issues, so it works on a document parsed from JSON and on one built in memory (an Import, or the app's own state).
/// <para>
/// It tolerates the placeholders the parse phase leaves behind for fields that failed to read (blank IDs, zero weeks): those are skipped, never
/// reported again. Exercise and Session references are checked only when the document contains the referenced collection;
/// otherwise they are resolved when the document is imported.
/// </para>
/// </summary>
internal static class DocumentValidator
{
    public static IReadOnlyList<FormatIssue> Validate(ExchangeDocument document)
    {
        var run = new Run(document);
        run.CheckExercises();
        run.CheckBodyAndGuides();
        run.CheckPlans();
        run.CheckWorkouts();
        return run.Issues;
    }

    private sealed class Run
    {
        private readonly ExchangeDocument _document;
        private readonly HashSet<string>? _exerciseIds;
        private readonly HashSet<string>? _sessionIds;
        private readonly Dictionary<string, HashSet<string>> _seenIds = [];

        public Run(ExchangeDocument document)
        {
            _document = document;
            _exerciseIds = document.Exercises?.Select(e => e.Id).ToHashSet();
            _sessionIds = document.Plans?.SelectMany(p => p.Sessions).Select(s => s.Id).ToHashSet();
        }

        public List<FormatIssue> Issues { get; } = [];

        private void Error(string path, string code, string message) => Issues.Add(new FormatIssue(path, code, message));

        /// <summary>IDs are unique per record kind across the whole document. Blank IDs (unreadable in the parse phase) are skipped.</summary>
        private void CheckUnique(string kind, string path, string field, string id)
        {
            if (id.Length == 0)
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
                Error(path, "duplicate_id", $"{path} \"{id}\" is used by more than one {kind}. Every {kind} needs its own {what}.");
            }
        }

        private bool IsUnknownExercise(string? id) => id is { Length: > 0 } && _exerciseIds is not null && !_exerciseIds.Contains(id);

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
                CheckUnique("Exercise", $"exercises[{i}].id", "id", exercise.Id);
            }
        }

        public void CheckBodyAndGuides()
        {
            foreach (var (weighIn, i) in Indexed(_document.WeighIns))
            {
                CheckUnique("Weigh-in", $"weighIns[{i}].id", "id", weighIn.Id);
            }

            foreach (var (b, i) in Indexed(_document.Breaks))
            {
                CheckUnique("Break", $"breaks[{i}].id", "id", b.Id);
            }

            foreach (var (badge, i) in Indexed(_document.Badges))
            {
                CheckUnique("Badge", $"badges[{i}].id", "id", badge.Id);
            }

            foreach (var (guide, i) in Indexed(_document.Guides))
            {
                var path = $"guides[{i}].exerciseId";
                CheckExercise(path, guide.ExerciseId);
                CheckUnique("Guide", path, "exerciseId", guide.ExerciseId);
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
                CheckUnique("Plan", $"plans[{i}].id", "id", plan.Id);
                CheckSessions(plan, i);
                CheckPhases(plan, i);
            }

            CheckExactlyOneActive(plans);
        }

        private void CheckExactlyOneActive(IReadOnlyList<Plan> plans)
        {
            var active = plans.Select((plan, index) => (plan, index)).Where(p => p.plan.IsActive).ToList();
            if (active.Count == 0)
            {
                Error("plans", "no_active_plan", "plans must contain exactly one active Plan, but none has \"isActive\": true.");
                return;
            }

            foreach (var (_, index) in active.Skip(1))
            {
                Error($"plans[{index}].isActive", "multiple_active_plans",
                    $"Only one Plan can be active, but \"{active[0].plan.Id}\" is already active. Set isActive to false on all but one.");
            }
        }

        private void CheckSessions(Plan plan, int planIndex)
        {
            foreach (var (session, i) in Indexed(plan.Sessions))
            {
                var path = $"plans[{planIndex}].sessions[{i}]";
                CheckUnique("Session", $"{path}.id", "id", session.Id);
                CheckRows(session.Exercises, $"{path}.exercises");
            }
        }

        private void CheckRows(IReadOnlyList<SessionExercise> rows, string path)
        {
            foreach (var (row, i) in Indexed(rows))
            {
                CheckExercise($"{path}[{i}].exerciseId", row.ExerciseId);
            }
        }

        private void CheckPhases(Plan plan, int planIndex)
        {
            var planSessionIds = plan.Sessions.Select(s => s.Id).ToHashSet();
            foreach (var (phase, i) in Indexed(plan.Phases))
            {
                var path = $"plans[{planIndex}].phases[{i}]";
                CheckUnique("Phase", $"{path}.id", "id", phase.Id);
                CheckWeekRange(phase, path);
                foreach (var (goal, g) in Indexed(phase.Goals))
                {
                    CheckExercise($"{path}.goals[{g}].exerciseId", goal.ExerciseId);
                }

                CheckOverrides(phase, path, planSessionIds);
            }

            CheckWeekCoverage(plan.Phases, planIndex);
        }

        private void CheckOverrides(Phase phase, string phasePath, HashSet<string> planSessionIds)
        {
            var overridden = new HashSet<string>();
            foreach (var (item, i) in Indexed(phase.SessionOverrides))
            {
                var path = $"{phasePath}.sessionOverrides[{i}]";
                var sessionPath = $"{path}.sessionId";
                if (item.SessionId.Length > 0 && !planSessionIds.Contains(item.SessionId))
                {
                    Error(sessionPath, "unknown_session", $"{sessionPath} \"{item.SessionId}\" is not a Session in this Plan.");
                }

                if (item.SessionId.Length > 0 && !overridden.Add(item.SessionId))
                {
                    Error(sessionPath, "duplicate_id", $"{sessionPath} \"{item.SessionId}\" is overridden more than once in this Phase.");
                }

                CheckRows(item.Exercises, $"{path}.exercises");
            }
        }

        /// <summary>A Phase's range is valid when it starts at week 1 or later and ends no earlier than it starts.</summary>
        private static bool HasValidRange(Phase phase) => phase.StartWeek >= 1 && phase.EndWeek >= phase.StartWeek;

        /// <summary>Reports an invalid range once, so a document built in memory is covered. For parsed JSON the reader drops this when the parse phase already reported the field.</summary>
        private void CheckWeekRange(Phase phase, string path)
        {
            if (phase.StartWeek < 1)
            {
                Error($"{path}.startWeek", "out_of_range", $"{path}.startWeek must be 1 or more (it was {phase.StartWeek}).");
            }
            else if (phase.EndWeek < phase.StartWeek)
            {
                Error($"{path}.endWeek", "out_of_range", $"{path}.endWeek must be {phase.StartWeek} or more (it was {phase.EndWeek}).");
            }
        }

        /// <summary>
        /// Phases may be listed in any order but, by week, must start at week 1 and follow each other with no gap and no overlap.
        /// A Plan with a Phase whose range is invalid is not checked: that Phase is reported once, and without it the coverage can't be judged.
        /// </summary>
        private void CheckWeekCoverage(IReadOnlyList<Phase> phases, int planIndex)
        {
            if (phases.Any(p => !HasValidRange(p)))
            {
                return;
            }

            var expectedStart = 1;
            foreach (var (phase, original) in phases.Select((phase, index) => (phase, index)).OrderBy(p => p.phase.StartWeek))
            {
                var path = $"plans[{planIndex}].phases[{original}].startWeek";
                if (phase.StartWeek > expectedStart)
                {
                    var missing = phase.StartWeek - 1 == expectedStart ? $"week {expectedStart}" : $"weeks {expectedStart} to {phase.StartWeek - 1}";
                    Error(path, "phase_gap", $"{path} is {phase.StartWeek}, which leaves {missing} in no Phase. Phases must cover every week from week 1 with no gaps.");
                }
                else if (phase.StartWeek < expectedStart)
                {
                    Error(path, "phase_overlap", $"{path} is {phase.StartWeek}, but week {phase.StartWeek} already belongs to an earlier Phase. Phase week ranges must not overlap.");
                }

                expectedStart = Math.Max(expectedStart, phase.EndWeek + 1);
            }
        }

        public void CheckWorkouts()
        {
            foreach (var (workout, i) in Indexed(_document.Workouts))
            {
                var path = $"workouts[{i}]";
                CheckUnique("Workout", $"{path}.id", "id", workout.Id);
                CheckWorkoutSession(workout, path);
                CheckRows(workout.Planned, $"{path}.planned");
                CheckEntries(workout, path);
                CheckPainCheck(workout.PainCheck, path);
            }
        }

        private void CheckWorkoutSession(Workout workout, string path)
        {
            if (workout.SessionId is { Length: > 0 } id && _sessionIds is not null && !_sessionIds.Contains(id))
            {
                Error($"{path}.sessionId", "unknown_session", $"{path}.sessionId \"{id}\" is not a Session in any Plan in this document.");
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
                    CheckUnique("Set", $"{path}.sets[{s}].id", "id", set.Id);
                }
            }
        }

        /// <summary>
        /// Once a Workout has a planned snapshot, a skipped entry must be one of its planned Exercises and a swapped entry must replace one.
        /// An unknown Exercise is already reported, so it is not reported a second time here.
        /// </summary>
        private void CheckAgainstPlanned(WorkoutEntry entry, string path, HashSet<string> planned)
        {
            if (planned.Count == 0)
            {
                return;
            }

            var (field, id, what) = entry.Status switch
            {
                EntryStatus.Skipped => ("exerciseId", entry.ExerciseId, "skipped"),
                EntryStatus.Swapped => ("swappedFromExerciseId", entry.SwappedFromExerciseId, "swapped"),
                _ => default((string, string?, string)),
            };

            if (id is { Length: > 0 } && !planned.Contains(id) && !IsUnknownExercise(id))
            {
                Error($"{path}.{field}", "not_in_planned",
                    $"{path}.{field} \"{id}\" is not in this Workout's planned rows, so it can't be {what}. Name an Exercise from planned.");
            }
        }

        private void CheckPainCheck(PainCheck? pain, string workoutPath)
        {
            if (pain is null)
            {
                return;
            }

            var path = $"{workoutPath}.painCheck";
            CheckUnique("Pain check", $"{path}.id", "id", pain.Id);
            CheckExercise($"{path}.exerciseId", pain.ExerciseId);
        }

        private static IEnumerable<(T Item, int Index)> Indexed<T>(IEnumerable<T>? items) =>
            (items ?? []).Select((item, index) => (item, index));
    }
}
