using System.Globalization;
using System.Text;
using Core.Model;
using Core.Rules.Performance;

namespace Core.Exchange;

/// <summary>
/// A read-only Markdown rendering of an Export for pasting into a chat. Never imported. Units are always the canonical ones
/// (kg, km, m, m:ss), whatever the display-unit Setting says, so a chat tool never has to guess. Exercise IDs appear only in the library table.
/// </summary>
internal static class MarkdownRenderer
{
    /// <param name="sessionNames">Session names by ID from the whole snapshot, so a Workout still shows its Session when the Plan itself is not exported.</param>
    public static string Render(ExchangeDocument document, ExportRequest request, IReadOnlyList<string> summaryLines, IReadOnlyDictionary<string, string> sessionNames)
    {
        var names = (document.Exercises ?? []).ToDictionary(e => e.Id, e => e.Name);
        var kinds = (document.Exercises ?? []).ToDictionary(e => e.Id, e => e.LogKind);
        var text = new StringBuilder();

        About(text, document, request, summaryLines);
        if (document.Plans is not null) Plans(text, document.Plans, names);
        if (document.Workouts is not null) Workouts(text, document.Workouts, sessionNames, names, kinds);
        if (document.WeighIns is not null) WeighIns(text, document.WeighIns);
        if (document.Exercises is { Count: > 0 }) Library(text, document.Exercises);
        if (document.Guides is not null) Guides(text, document.Guides, names);

        return text.ToString();
    }

    private static void About(StringBuilder text, ExchangeDocument document, ExportRequest request, IReadOnlyList<string> summaryLines)
    {
        text.AppendLine("# Training export");
        text.AppendLine();
        text.AppendLine($"Exported {Date(DateOnly.FromDateTime(document.ExportedAt!.Value.DateTime))}.");
        text.AppendLine();
        foreach (var line in summaryLines)
        {
            text.AppendLine($"- {line}");
        }

        var left = new (bool Ticked, string Label)[]
        {
            (request.Workouts, "Workouts"), (request.Plan, "Plan"), (request.WeighIns, "Weigh-ins"), (request.PainChecks, "Pain checks"), (request.Guides, "Guides"),
        }.Where(t => !t.Ticked).Select(t => t.Label).ToList();
        if (left.Count > 0)
        {
            text.AppendLine($"- Not included: {string.Join(", ", left)}");
        }

        text.AppendLine("- Units: weights in kg, distances in km or m, durations as m:ss (or h:mm:ss).");
        text.AppendLine("- Read-only: this text cannot be imported back.");
    }

    private static void Plans(StringBuilder text, IReadOnlyList<Plan> plans, Dictionary<string, string> names)
    {
        text.AppendLine().AppendLine("## Plan");
        foreach (var plan in plans)
        {
            text.AppendLine().AppendLine($"### {plan.Name}").AppendLine();
            if (plan.Goal is { } goal)
            {
                text.AppendLine($"- Goal: {goal}{(plan.GoalDate is { } goalDate ? $" on {Date(goalDate)}" : "")}");
            }

            text.AppendLine($"- Starts {Date(plan.StartDate)}; weekly target {plan.WeeklyTarget} Workouts");
            foreach (var phase in plan.Phases)
            {
                text.AppendLine($"- {PhaseText(phase, plan, names)}");
            }

            foreach (var session in plan.Sessions)
            {
                var interval = session.BenchmarkIntervalWeeks is { } weeks ? $", every {weeks} weeks" : "";
                text.AppendLine().AppendLine($"#### {session.Name} ({session.DefaultDay}, {session.Type}{interval})").AppendLine();
                foreach (var row in session.Exercises)
                {
                    text.AppendLine($"- {RowText(row, names)}");
                }
            }
        }
    }

    private static string PhaseText(Phase phase, Plan plan, Dictionary<string, string> names)
    {
        var goals = phase.Goals.Select(g => $" Goal: {Name(names, g.ExerciseId)} {Num(g.WeightKg)} kg x {g.Reps}.");
        var overrides = phase.SessionOverrides.Select(o =>
            $" {plan.Sessions.FirstOrDefault(s => s.Id == o.SessionId)?.Name ?? "A Session"} becomes: {string.Join("; ", o.Exercises.Select(r => RowText(r, names)))}.");
        return $"Weeks {phase.StartWeek}-{phase.EndWeek}: {phase.Focus}.{string.Concat(goals)}{string.Concat(overrides)}".TrimEnd('.');
    }

    /// <summary>A planned row: "Goblet Squat: 3 x 8-12 (Chest tall)", "Farmers Carry: 3 sets".</summary>
    private static string RowText(SessionExercise row, Dictionary<string, string> names)
    {
        var target = (row.Reps, row.RepsMax) switch
        {
            ({ } reps, { } max) => $"{row.Sets} x {reps}-{max}",
            ({ } reps, null) => $"{row.Sets} x {reps}",
            _ => $"{row.Sets} set{(row.Sets == 1 ? "" : "s")}",
        };
        return $"{Name(names, row.ExerciseId)}: {target}{(row.Cue is { } cue ? $" ({cue})" : "")}";
    }

    private static void Workouts(StringBuilder text, IReadOnlyList<Workout> workouts, IReadOnlyDictionary<string, string> sessionNames, Dictionary<string, string> names, Dictionary<string, LogKind> kinds)
    {
        text.AppendLine().AppendLine("## Workouts");
        foreach (var workout in workouts.OrderBy(w => w.Date))
        {
            text.AppendLine().AppendLine($"### {Date(workout.Date)}: {WorkoutTitle(workout, sessionNames)}").AppendLine();
            WorkoutLines(text, workout, names, kinds);
        }
    }

    private static string WorkoutTitle(Workout workout, IReadOnlyDictionary<string, string> sessionNames)
    {
        var name = workout.SessionId is { } id && sessionNames.TryGetValue(id, out var session) ? session : "Custom workout";
        return workout.LoggedAfterwards ? $"{name} (logged afterwards)" : name;
    }

    private static void WorkoutLines(StringBuilder text, Workout workout, Dictionary<string, string> names, Dictionary<string, LogKind> kinds)
    {
        foreach (var entry in workout.Entries)
        {
            text.AppendLine($"- {EntryText(entry, names, kinds)}");
        }

        if (workout.Entries.Count == 0 && (workout.Planned.Count > 0 || workout.PainCheck is null))
        {
            var planned = workout.Planned.Count > 0 ? $" Planned: {string.Join(", ", workout.Planned.Select(r => Name(names, r.ExerciseId)))}" : "";
            text.AppendLine($"- Nothing logged.{planned}");
        }

        if (workout.PainCheck is { } pain)
        {
            text.AppendLine($"- Pain check: {PainText(pain, names)}");
        }
    }

    private static string EntryText(WorkoutEntry entry, Dictionary<string, string> names, Dictionary<string, LogKind> kinds)
    {
        var name = Name(names, entry.ExerciseId);
        if (entry.Status == EntryStatus.Skipped)
        {
            return $"{name}: skipped{(entry.SkipReason is { } reason ? $" ({reason})" : "")}";
        }

        var label = entry.SwappedFromExerciseId is { } from ? $"{name} (swapped from {Name(names, from)})" : name;
        var kind = kinds.GetValueOrDefault(entry.ExerciseId);
        var sets = entry.Sets.Count == 0 ? "done" : string.Join("; ", entry.Sets.Select(s => SetText(s, kind)));
        return $"{label}: {sets}{(entry.Rpe is { } rpe ? $", RPE {rpe}" : "")}";
    }

    /// <summary>One Set in canonical units: "24.5 kg x 10", "32 kg x 40 m in 1:00", "5 km in 25:30", "bodyweight x 8".</summary>
    private static string SetText(WorkoutSet set, LogKind kind)
    {
        var main = set switch
        {
            { WeightKg: { } w and > 0, Reps: { } r } => $"{Num(w)} kg x {r}",
            { WeightKg: { } w and > 0, DistanceM: { } d } => $"{Num(w)} kg x {Num(d)} m",
            { WeightKg: { } w and > 0 } => $"{Num(w)} kg",
            { Reps: { } r } when kind == LogKind.BodyweightLoad => $"bodyweight x {r}",
            { Reps: { } r } => $"{r} reps",
            { DistanceM: { } d } => $"{Num(d)} m",
            { DistanceKm: { } d } => $"{Num(d)} km",
            { MeasurementCm: { } cm } => $"{Num(cm)} cm",
            _ => null,
        };
        var duration = set.DurationSeconds is { } seconds ? TargetRules.Clock(seconds) : null;
        var text = (main, duration) switch
        {
            (null, null) => "done",
            (null, _) => duration,
            (_, null) => main,
            _ => $"{main} in {duration}",
        };
        return set.IsMax ? $"{text} (max)" : text;
    }

    private static string PainText(PainCheck pain, Dictionary<string, string> names)
    {
        if (pain.Status == PainStatus.None)
        {
            return "none";
        }

        var parts = pain.Areas.Select(Area).Concat(pain.Severity is { } severity ? [severity.ToString().ToLowerInvariant()] : []);
        var during = pain.ExerciseId is { } id ? $", during {Name(names, id)}" : "";
        var note = pain.Note is { } n ? $": \"{n}\"" : "";
        return $"{string.Join(", ", parts)}{during}{note}";
    }

    private static string Area(BodyArea area) =>
        string.Concat(area.ToString().Select((c, i) => i > 0 && char.IsUpper(c) ? $" {char.ToLowerInvariant(c)}" : c.ToString()));

    /// <summary>A plain table; the weekly averages are left to the reader.</summary>
    private static void WeighIns(StringBuilder text, IReadOnlyList<WeighIn> weighIns)
    {
        text.AppendLine().AppendLine("## Weigh-ins").AppendLine();
        text.AppendLine("| Date | Weight (kg) |").AppendLine("| --- | --- |");
        foreach (var weighIn in weighIns.OrderBy(w => w.Date))
        {
            text.AppendLine($"| {Date(weighIn.Date)} | {Num(weighIn.WeightKg)} |");
        }
    }

    /// <summary>The only place Exercise IDs appear, so the reader can map a name to the ID an Import would use.</summary>
    private static void Library(StringBuilder text, IReadOnlyList<Exercise> exercises)
    {
        text.AppendLine().AppendLine("## Exercise library").AppendLine();
        text.AppendLine("| ID | Name | Log kind | Tags |").AppendLine("| --- | --- | --- | --- |");
        foreach (var exercise in exercises)
        {
            text.AppendLine($"| {Cell(exercise.Id)} | {Cell(exercise.Name)} | {exercise.LogKind} | {Cell(TagsText(exercise.Tags))} |");
        }
    }

    private static string TagsText(ExerciseTags? tags) => tags is null
        ? ""
        : string.Join("; ", new[] { ("muscles", tags.MuscleGroups), ("equipment", tags.Equipment), ("patterns", tags.MovementPatterns) }
            .Where(t => t.Item2.Count > 0)
            .Select(t => $"{t.Item1}: {string.Join(", ", t.Item2)}"));

    private static string Cell(string value) => value.Replace("|", "\\|", StringComparison.Ordinal).ReplaceLineEndings(" ");

    private static void Guides(StringBuilder text, IReadOnlyList<Guide> guides, Dictionary<string, string> names)
    {
        text.AppendLine().AppendLine("## Guides");
        foreach (var guide in guides)
        {
            text.AppendLine().AppendLine($"### {Name(names, guide.ExerciseId)}").AppendLine();
            var fields = new (string Label, string? Value)[]
            {
                ("Works", guide.Works), ("Why it is in the plan", guide.WhyInPlan), ("Setup", guide.Setup), ("How to", guide.HowTo),
                ("Cues", Joined(guide.Cues)), ("Common mistakes", Joined(guide.CommonMistakes)), ("Easier", guide.Easier), ("Harder", guide.Harder),
                ("Watch outs", Joined(guide.WatchOuts)), ("Video", guide.VideoUrl),
            };
            foreach (var (label, value) in fields.Where(f => !string.IsNullOrWhiteSpace(f.Value)))
            {
                text.AppendLine($"- {label}: {value}");
            }
        }
    }

    private static string? Joined(IReadOnlyList<string> items) => items.Count == 0 ? null : string.Join("; ", items);

    private static string Name(Dictionary<string, string> names, string id) => names.GetValueOrDefault(id, id);

    private static string Num(double n) => TargetRules.Num(n);

    private static string Date(DateOnly date) => date.ToString("ddd d MMM yyyy", CultureInfo.InvariantCulture);
}
