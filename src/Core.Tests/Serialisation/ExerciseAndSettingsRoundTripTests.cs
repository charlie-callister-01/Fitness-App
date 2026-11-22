using Core.Model;
using Core.Serialisation;

namespace Core.Tests.Serialisation;

public class ExerciseAndSettingsRoundTripTests
{
    [Fact]
    public void Valid_minimal_dataset_round_trips_to_identical_json()
    {
        var written = ExchangeFormat.Write(TestData.Minimal());

        var result = ExchangeFormat.Read(written);

        Assert.True(result.IsSuccess, string.Join("; ", result.Errors.Select(e => e.Path + " " + e.Message)));
        Assert.Equal(written, ExchangeFormat.Write(result.Document!));
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Round_trip_preserves_values()
    {
        var doc = ExchangeFormat.Read(TestData.MinimalJson()).Document!;

        var exercise = Assert.Single(doc.Exercises!);
        Assert.Equal("goblet-squat", exercise.Id);
        Assert.Equal("Goblet squat", exercise.Name);
        Assert.Equal(LogKind.Load, exercise.LogKind);
        Assert.True(exercise.IsBenchmark);
        Assert.Equal(["quads", "glutes"], exercise.Tags!.MuscleGroups);
        Assert.Equal(82.5, doc.Settings!.GoalWeightKg);
        Assert.Equal(new DateTimeOffset(2026, 11, 16, 9, 30, 0, TimeSpan.FromHours(1)), doc.ExportedAt);
    }

    [Fact]
    public void Output_is_camelCase_with_string_enums()
    {
        var json = ExchangeFormat.Write(TestData.Minimal());

        Assert.Contains("\"formatVersion\":2", json);
        Assert.Contains("\"logKind\":\"load\"", json);
        Assert.Contains("\"weightIncrementKg\":2.5", json);
        Assert.Contains("\"displayWeightUnit\":\"kg\"", json);
    }

    [Fact]
    public void Indented_output_reads_back_the_same()
    {
        var indented = ExchangeFormat.Write(TestData.Minimal(), indented: true);

        var result = ExchangeFormat.Read(indented);

        Assert.Contains("\n", indented);
        Assert.Equal(TestData.MinimalJson(), ExchangeFormat.Write(result.Document!));
    }

    [Fact]
    public void Unicode_names_and_every_log_kind_round_trip()
    {
        var exercises = Enum.GetValues<LogKind>()
            .Select((kind, i) => new Exercise($"ex-{i}", $"Übung «{kind}» 日本語, 2.5", kind, null, false))
            .ToList();
        var doc = TestData.Minimal() with { Exercises = exercises };
        var json = ExchangeFormat.Write(doc);

        var back = ExchangeFormat.Read(json);

        Assert.True(back.IsSuccess);
        Assert.Equal(json, ExchangeFormat.Write(back.Document!));
        Assert.Equal(exercises.Select(e => (e.Name, e.LogKind)), back.Document!.Exercises!.Select(e => (e.Name, e.LogKind)));
    }

    [Fact]
    public void Partial_document_without_exercises_or_settings_is_accepted()
    {
        var result = ExchangeFormat.Read("""{"formatVersion":1}""");

        Assert.True(result.IsSuccess);
        Assert.Null(result.Document!.Exercises);
        Assert.Null(result.Document.Settings);
    }
}
