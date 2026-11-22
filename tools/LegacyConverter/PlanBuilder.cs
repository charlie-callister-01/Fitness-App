using System.Globalization;
using System.Text.RegularExpressions;
using Core.Model;

namespace LegacyConverter;

internal sealed record ParsedSetsReps(int Sets, int? Reps, int? RepsMax, string? Cue);

internal static partial class PlanBuilder
{
    private static readonly Dictionary<string, (string Id, DefaultDay? Day, SessionType Type, int? BenchmarkWeeks)> SessionMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["M"] = ("m-mobility", DefaultDay.Daily, SessionType.Mobility, null),
            ["A"] = ("a-lower", DefaultDay.Monday, SessionType.Strength, null),
            ["B"] = ("b-upper", DefaultDay.Tuesday, SessionType.Strength, null),
            ["C"] = ("c-engine", DefaultDay.Wednesday, SessionType.Cardio, null),
            ["D"] = ("d-full-body", DefaultDay.Thursday, SessionType.Strength, null),
            ["E"] = ("e-long", DefaultDay.Saturday, SessionType.Cardio, null),
            ["T"] = ("benchmark", DefaultDay.Friday, SessionType.Test, 4),
        };

    private static readonly HashSet<string> ExplicitTextTargets = new(StringComparer.OrdinalIgnoreCase)
    {
        "every hour",
        "in one go",
        "today",
        "weekly",
        "monthly",
        "1 all-out",
        "1 hold",
        "1 set",
    };

    public static bool TryGetSessionId(string sessionCode, out string sessionId)
    {
        if (SessionMap.TryGetValue(sessionCode, out var mapped))
        {
            sessionId = mapped.Id;
            return true;
        }

        sessionId = string.Empty;
        return false;
    }

    public static Settings BuildSettings(ParsedConfig config)
    {
        var streakMin = ReadInt(config.Settings, "streak min", 4);
        var incrementKg = ReadDouble(config.Settings, "increment kg", 2);

        return Settings.Default with
        {
            WeightIncrementKg = incrementKg,
            GoalWeightKg = null,
            StreakMinWorkoutsPerWeek = streakMin,
            StreakCountsCustomWorkouts = false,
        };
    }

    public static Plan BuildPlan(ParsedConfig config, List<ConversionNote> notes)
    {
        var startDate = DateOnly.Parse(config.Settings["start"], CultureInfo.InvariantCulture);
        var goalDate = DateOnly.Parse(config.Settings["race"], CultureInfo.InvariantCulture);
        var weeklyTarget = ReadInt(config.Settings, "sessions per week", 5);
        var streakMin = ReadInt(config.Settings, "streak min", 4);
        var incrementKg = ReadDouble(config.Settings, "increment kg", 2);
        var mobilityDonePercent = ReadInt(config.Settings, "mobility done percent", 75);

        var phases = config.Phases
            .Select(p => new Phase(
                Id: LegacyIds.ToId(p.Focus),
                Focus: p.Focus,
                StartWeek: p.FromWeek,
                EndWeek: p.ToWeek,
                Goals: [],
                SessionOverrides: []))
            .ToList();

        var tracks = new List<Track>
        {
            new(
                Id: "main",
                Name: "Main",
                Rhythm: new TrackRhythm.Weekly(
                    WeeklyTarget: weeklyTarget,
                    StreakMinWorkoutsPerWeek: streakMin,
                    StreakCountsCustomWorkouts: false),
                Phases: phases),
            new(
                Id: "mobility",
                Name: "Morning mobility",
                Rhythm: new TrackRhythm.Daily(DayDonePercent: mobilityDonePercent),
                Phases: []),
        };

        var sessions = new List<Session>();
        foreach (var row in config.Sessions)
        {
            if (string.Equals(row.Code, "W", StringComparison.OrdinalIgnoreCase))
            {
                notes.Add(new ConversionNote(
                    ConversionNoteKind.Dropped,
                    "Session: W (Weigh-in)",
                    "Weigh-in is a standalone action in the app."));
                continue;
            }

            if (!SessionMap.TryGetValue(row.Code, out var mapped))
            {
                throw new FormatException($"Unknown session code '{row.Code}' in Config.md.");
            }

            var trackId = mapped.Type == SessionType.Mobility ? "mobility" : "main";
            var day = mapped.Day ?? ParseDefaultDay(row.Day, row.Code);

            if (string.Equals(row.Code, "T", StringComparison.OrdinalIgnoreCase))
            {
                notes.Add(new ConversionNote(
                    ConversionNoteKind.Guessed,
                    "Session: Benchmark test",
                    "DefaultDay set to Friday (free day; Config.md says Every 4 weeks)."));
            }

            var rows = config.Exercises
                .Where(e => e.SessionCodes.Any(c => string.Equals(c, row.Code, StringComparison.OrdinalIgnoreCase)))
                .Select(e =>
                {
                    var parsed = ParseSetsReps(e.SetsReps, e.Cue, notes, e.Name);
                    return new SessionExercise(
                        ExerciseId: LegacyIds.ToId(e.Name),
                        Sets: parsed.Sets,
                        Reps: parsed.Reps,
                        RepsMax: parsed.RepsMax,
                        Cue: parsed.Cue);
                })
                .ToList();

            sessions.Add(new Session(
                Id: mapped.Id,
                Name: row.Name,
                TrackId: trackId,
                DefaultDay: day,
                Type: mapped.Type,
                BenchmarkIntervalWeeks: mapped.BenchmarkWeeks,
                Exercises: rows));
        }

        return new Plan(
            Id: "hyrox-2027",
            Name: "HYROX London",
            IsActive: true,
            StartDate: startDate,
            Goal: "HYROX London Doubles",
            GoalDate: goalDate,
            WeightIncrementKg: incrementKg,
            Tracks: tracks,
            Sessions: sessions);
    }

    internal static ParsedSetsReps ParseSetsReps(
        string raw,
        string? existingCue,
        List<ConversionNote>? notes = null,
        string? exerciseName = null)
    {
        var text = raw.Trim();
        var cue = string.IsNullOrWhiteSpace(existingCue) ? null : existingCue.Trim();

        // Compound slashes like "2 × 10 / 2 × 6 per side" or "2 × 15 / 2 × 10"
        if (text.Contains('/', StringComparison.Ordinal) && text.Contains('×', StringComparison.Ordinal))
        {
            return new ParsedSetsReps(1, null, null, AppendCue(cue, text));
        }

        // Range of sets with duration like "6–10 × 30 s hard"
        if (SetRangeDurationRegex().IsMatch(text))
        {
            return new ParsedSetsReps(1, null, null, AppendCue(cue, text));
        }

        // Sets × duration like "3 × 20–30 s per side"
        var setsDuration = SetsDurationRegex().Match(text);
        if (setsDuration.Success)
        {
            var sets = int.Parse(setsDuration.Groups["sets"].Value, CultureInfo.InvariantCulture);
            var rest = setsDuration.Groups["rest"].Value.Trim();
            return new ParsedSetsReps(sets, null, null, AppendCue(cue, rest));
        }

        // Standard "N × A–B [suffix]" or "N × A [suffix]"
        var setsReps = SetsRepsRegex().Match(text);
        if (setsReps.Success)
        {
            var sets = int.Parse(setsReps.Groups["sets"].Value, CultureInfo.InvariantCulture);
            var reps = int.Parse(setsReps.Groups["min"].Value, CultureInfo.InvariantCulture);
            int? repsMax = setsReps.Groups["max"].Success
                ? int.Parse(setsReps.Groups["max"].Value, CultureInfo.InvariantCulture)
                : null;
            var suffix = setsReps.Groups["suffix"].Value.Trim();
            return new ParsedSetsReps(sets, reps, repsMax, AppendCue(cue, suffix));
        }

        // Leading "× A [suffix]" (single set / mobility drill)
        var leadingX = LeadingCrossRegex().Match(text);
        if (leadingX.Success)
        {
            var reps = int.Parse(leadingX.Groups["reps"].Value, CultureInfo.InvariantCulture);
            var suffix = leadingX.Groups["suffix"].Value.Trim();
            return new ParsedSetsReps(1, reps, null, AppendCue(cue, suffix));
        }

        // "N rounds of ..."
        var rounds = RoundsRegex().Match(text);
        if (rounds.Success)
        {
            var sets = int.Parse(rounds.Groups["sets"].Value, CultureInfo.InvariantCulture);
            return new ParsedSetsReps(sets, null, null, AppendCue(cue, text));
        }

        // Pure duration text like "5 min", "20–25 min", "45 s per side", "30 s each + 10 circles"
        if (DurationOnlyRegex().IsMatch(text))
        {
            return new ParsedSetsReps(1, null, null, AppendCue(cue, text));
        }

        // Known descriptive non-numeric targets
        if (ExplicitTextTargets.Contains(text))
        {
            return new ParsedSetsReps(1, null, null, AppendCue(cue, text));
        }

        notes?.Add(new ConversionNote(
            ConversionNoteKind.Warning,
            exerciseName is null ? $"Sets × reps: {raw}" : $"Exercise: {exerciseName}",
            $"Unreadable Sets × reps value: '{raw}'."));
        throw new FormatException($"Unreadable Sets × reps value: '{raw}'.");
    }

    private static string? AppendCue(string? cue, string? extra)
    {
        if (string.IsNullOrWhiteSpace(extra))
        {
            return cue;
        }

        var trimmedExtra = extra.Trim();
        if (string.IsNullOrWhiteSpace(cue))
        {
            return trimmedExtra;
        }

        if (cue.Contains(trimmedExtra, StringComparison.OrdinalIgnoreCase))
        {
            return cue;
        }

        return $"{cue} ({trimmedExtra})";
    }

    private static DefaultDay ParseDefaultDay(string raw, string sessionCode) => raw.Trim().ToLowerInvariant() switch
    {
        "sun" or "sunday" => DefaultDay.Sunday,
        "mon" or "monday" => DefaultDay.Monday,
        "tue" or "tuesday" => DefaultDay.Tuesday,
        "wed" or "wednesday" => DefaultDay.Wednesday,
        "thu" or "thursday" => DefaultDay.Thursday,
        "fri" or "friday" => DefaultDay.Friday,
        "sat" or "saturday" => DefaultDay.Saturday,
        "daily" => DefaultDay.Daily,
        _ => throw new FormatException($"Unknown day '{raw}' for session '{sessionCode}'."),
    };

    private static int ReadInt(IReadOnlyDictionary<string, string> settings, string key, int fallback) =>
        settings.TryGetValue(key, out var raw)
            ? int.Parse(raw, CultureInfo.InvariantCulture)
            : fallback;

    private static double ReadDouble(IReadOnlyDictionary<string, string> settings, string key, double fallback) =>
        settings.TryGetValue(key, out var raw)
            ? double.Parse(raw, CultureInfo.InvariantCulture)
            : fallback;

    [GeneratedRegex(@"^\d+\s*[–-]\s*\d+\s*×\s*.+$", RegexOptions.Compiled)]
    private static partial Regex SetRangeDurationRegex();

    [GeneratedRegex(@"^(?<sets>\d+)\s*×\s*(?<rest>\d+(?:[–-]\d+)?\s*s\b.*)$", RegexOptions.Compiled)]
    private static partial Regex SetsDurationRegex();

    [GeneratedRegex(@"^(?<sets>\d+)\s*×\s*(?<min>\d+)(?:\s*[–-]\s*(?<max>\d+))?(?<suffix>.*)$", RegexOptions.Compiled)]
    private static partial Regex SetsRepsRegex();

    [GeneratedRegex(@"^×\s*(?<reps>\d+)(?<suffix>.*)$", RegexOptions.Compiled)]
    private static partial Regex LeadingCrossRegex();

    [GeneratedRegex(@"^(?<sets>\d+)\s+rounds?\s+of\b.+$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex RoundsRegex();

    [GeneratedRegex(@"^\d+(?:[–-]\d+)?\s*(?:s|min)\b.*$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex DurationOnlyRegex();
}
