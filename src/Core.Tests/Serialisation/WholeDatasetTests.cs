using System.Text.Json.Nodes;
using Core.Model;
using Core.Serialisation;

namespace Core.Tests.Serialisation;

/// <summary>The whole exchange document at once: every entity, every enum value, unicode and decimals, references across entities.</summary>
public class WholeDatasetTests
{
    private static ExchangeDocument Everything()
    {
        var logKinds = Enum.GetValues<LogKind>().Select(k => new Exercise($"ex-{k}".ToLowerInvariant(), $"Übung {k} — 深蹲 🏋️", k, null, false)).ToList();
        var rich = WorkoutTestData.Rich();
        var tagged = rich.Exercises![0] with { Tags = new ExerciseTags(["quads", "Gesäß"], ["kettlebell"], ["squat"]), IsBenchmark = true };
        var exercises = rich.Exercises.Skip(1).Prepend(tagged).Concat(logKinds).ToList();

        var sessionTypes = Enum.GetValues<SessionType>()
            .Select(t => new Session($"s-{t}".ToLowerInvariant(), $"Session {t} ☕", DayOfWeek.Sunday, t, null, [new SessionExercise("ex-load", 1, 5, null, "Zürich")]))
            .ToList();
        var plan = rich.Plans![0];
        var plans = new List<Plan> { plan with { Sessions = plan.Sessions.Concat(sessionTypes).ToList() }, rich.Plans[1] };

        var everyBodyArea = new PainCheck("p-all", PainStatus.Reported, Enum.GetValues<BodyArea>(), PainSeverity.Sharp, "Ouch — ça fait mal", "ex-load");
        var extraWorkouts = Enum.GetValues<EntryStatus>().Select((status, i) => new Workout(
            $"w-status-{i}", new DateOnly(2026, 11, 20 + i), "s-test", false, null, null, [],
            [new WorkoutEntry("ex-load", status, status == EntryStatus.Skipped ? "Tired" : null, status == EntryStatus.Swapped ? "goblet-squat" : null, null,
                status == EntryStatus.Skipped ? [] : [new WorkoutSet($"s-status-{i}", 0.125, 1, null, null, null, false)])],
            i == 0 ? everyBodyArea : new PainCheck($"p-status-{i}", PainStatus.Reported, [BodyArea.Other], i == 1 ? PainSeverity.Mild : PainSeverity.Moderate, null, null)));

        return rich with
        {
            ExportedAt = new DateTimeOffset(2026, 11, 16, 9, 30, 0, TimeSpan.FromHours(-5)),
            ExportedFrom = "fitness-app 0.1 ✓",
            Exercises = exercises,
            Settings = TestData.ValidSettings() with { DisplayWeightUnit = WeightUnit.Lb, DisplayDistanceUnit = DistanceUnit.Mi, WeightIncrementKg = 1.25, GoalWeightKg = 79.375 },
            Plans = plans,
            Workouts = rich.Workouts!.Concat(extraWorkouts).ToList(),
            WeighIns = [new WeighIn("wi-1", new DateOnly(2026, 11, 16), 84.35), new WeighIn("wi-2", new DateOnly(2026, 11, 17), 83.1)],
            Breaks = [new Break("xmas", new DateOnly(2026, 12, 20), new DateOnly(2027, 1, 2), "Weihnachten 🎄"), new Break("one-day", new DateOnly(2027, 2, 1), new DateOnly(2027, 2, 1), "Sick")],
            Badges = [new Badge("squat-100", "100 kg squat 💪", "maxWeightKg", 100), new Badge("decimal", "Half kilo", "totalVolumeKg", 0.5)],
            Guides =
            [
                new Guide("goblet-squat", "Quads", "Why — ünïcode", "Setup", "How", ["Chest up", "Knees out"], ["Heels lifting"], "Box squat", "Heavier", ["Back"], "https://example.com/v?x=1&y=2", "Notes: 膝"),
                new Guide("rdl", null, null, null, null, [], [], null, null, [], null, null),
            ],
        };
    }

    [Fact]
    public void Every_entity_survives_a_round_trip_unchanged()
    {
        var original = Everything();

        var first = ExchangeFormat.Write(original);
        var result = ExchangeFormat.Read(first);

        Assert.True(result.IsSuccess, string.Join("; ", result.Errors.Select(e => $"{e.Path}: {e.Message}")));
        Assert.Empty(result.Warnings);
        Assert.Equal(first, ExchangeFormat.Write(result.Document!));
        AssertSameContent(original, result.Document!);
    }

    [Fact]
    public void Round_trip_keeps_unicode_and_decimal_values_exactly()
    {
        var read = ExchangeFormat.Read(ExchangeFormat.Write(Everything(), indented: true)).Document!;

        Assert.Contains(read.Exercises!, e => e.Name == "Übung Load — 深蹲 🏋️");
        Assert.Equal(79.375, read.Settings!.GoalWeightKg);
        Assert.Equal(0.125, read.Workouts!.SelectMany(w => w.Entries).SelectMany(e => e.Sets).First(s => s.Id == "s-status-0").WeightKg);
        Assert.Equal(84.35, read.WeighIns![0].WeightKg);
        Assert.Equal("Weihnachten 🎄", read.Breaks![0].Reason);
    }

    [Fact]
    public void Every_enum_value_is_covered_by_the_dataset()
    {
        var d = Everything();
        var entries = d.Workouts!.SelectMany(w => w.Entries).ToList();
        var checks = d.Workouts!.Select(w => w.PainCheck).OfType<PainCheck>().ToList();

        Assert.Equal(Enum.GetValues<LogKind>().Order(), d.Exercises!.Select(e => e.LogKind).Distinct().Order());
        Assert.Equal(Enum.GetValues<SessionType>().Order(), d.Plans!.SelectMany(p => p.Sessions).Select(s => s.Type).Distinct().Order());
        Assert.Equal(Enum.GetValues<EntryStatus>().Order(), entries.Select(e => e.Status).Distinct().Order());
        Assert.Equal(Enum.GetValues<PainStatus>().Order(), checks.Select(c => c.Status).Distinct().Order());
        Assert.Equal(Enum.GetValues<PainSeverity>().Order(), checks.Select(c => c.Severity).OfType<PainSeverity>().Distinct().Order());
        Assert.Equal(Enum.GetValues<BodyArea>().Order(), checks.SelectMany(c => c.Areas).Distinct().Order());
        Assert.Equal((WeightUnit.Lb, DistanceUnit.Mi), (d.Settings!.DisplayWeightUnit, d.Settings.DisplayDistanceUnit));
    }

    [Fact]
    public void A_reference_to_a_missing_exercise_is_caught_anywhere_in_the_document()
    {
        var json = Mutate(root => root["guides"]![0]!["exerciseId"] = "nope");

        var errors = ExchangeFormat.Read(json).Errors;

        Assert.Contains(errors, e => e.Path == "guides[0].exerciseId" && e.Code == "unknown_exercise");
    }

    [Fact]
    public void A_workout_pointing_at_a_missing_session_is_caught_in_the_whole_document()
    {
        var json = Mutate(root => root["workouts"]![0]!["sessionId"] = "nope");

        Assert.Contains(ExchangeFormat.Read(json).Errors, e => e.Path == "workouts[0].sessionId" && e.Code == "unknown_session");
    }

    [Fact]
    public void Unknown_fields_warn_with_their_path_at_every_depth()
    {
        var json = Mutate(root =>
        {
            root["extra"] = 1;
            root["settings"]!["extra"] = 1;
            root["exercises"]![0]!["tags"]!["extra"] = 1;
            root["plans"]![0]!["extra"] = 1;
            root["plans"]![0]!["phases"]![0]!["goals"]![0]!["extra"] = 1;
            root["plans"]![0]!["sessions"]![0]!["exercises"]![0]!["extra"] = 1;
            root["workouts"]![0]!["extra"] = 1;
            root["workouts"]![0]!["entries"]![0]!["sets"]![0]!["extra"] = 1;
            root["workouts"]![0]!["painCheck"]!["extra"] = 1;
            root["weighIns"]![0]!["extra"] = 1;
            root["breaks"]![0]!["extra"] = 1;
            root["badges"]![0]!["extra"] = 1;
            root["guides"]![0]!["extra"] = 1;
        });

        var result = ExchangeFormat.Read(json);

        Assert.True(result.IsSuccess, string.Join("; ", result.Errors.Select(e => $"{e.Path}: {e.Message}")));
        Assert.Equal(
            new[]
            {
                "extra", "settings.extra", "exercises[0].tags.extra", "plans[0].extra", "plans[0].phases[0].goals[0].extra",
                "plans[0].sessions[0].exercises[0].extra", "workouts[0].extra", "workouts[0].entries[0].sets[0].extra",
                "workouts[0].painCheck.extra", "weighIns[0].extra", "breaks[0].extra", "badges[0].extra", "guides[0].extra",
            }.Order(),
            result.Warnings.Select(w => w.Path).Order());
        Assert.All(result.Warnings, w => Assert.Equal("unknown_field", w.Code));
        Assert.NotNull(result.Document);
    }

    private static string Mutate(Action<JsonObject> edit)
    {
        var root = JsonNode.Parse(ExchangeFormat.Write(Everything()))!.AsObject();
        edit(root);
        return root.ToJsonString();
    }

    /// <summary>Records compare their lists by reference, so compare the JSON each side writes instead, plus counts per collection.</summary>
    private static void AssertSameContent(ExchangeDocument expected, ExchangeDocument actual)
    {
        Assert.Equal(ExchangeFormat.Write(expected, indented: true), ExchangeFormat.Write(actual, indented: true));
        Assert.Equal(expected.Exercises!.Count, actual.Exercises!.Count);
        Assert.Equal(expected.Workouts!.Count, actual.Workouts!.Count);
        Assert.Equal(expected.Guides!.Count, actual.Guides!.Count);
        Assert.Equal(expected.Settings, actual.Settings);
        Assert.Equal(expected.ExportedAt, actual.ExportedAt);
    }
}
