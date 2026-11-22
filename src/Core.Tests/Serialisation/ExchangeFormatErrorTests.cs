using Core.Ids;
using Core.Serialisation;

namespace Core.Tests.Serialisation;

public class ExchangeFormatErrorTests
{
    private static FormatIssue SingleError(string json)
    {
        var result = ExchangeFormat.Read(json);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Document);
        return Assert.Single(result.Errors);
    }

    private static string Mutate(string from, string to)
    {
        var json = TestData.MinimalJson();
        Assert.Contains(from, json);
        return json.Replace(from, to);
    }

    [Theory]
    [InlineData("", "empty_input")]
    [InlineData("   ", "empty_input")]
    [InlineData("{not json", "malformed_json")]
    [InlineData("[1,2]", "wrong_type")]
    public void Unreadable_input_gives_a_root_error_not_an_exception(string input, string code)
    {
        var error = SingleError(input);

        Assert.Equal(code, error.Code);
        Assert.Equal("$", error.Path);
        Assert.False(string.IsNullOrWhiteSpace(error.Message));
    }

    [Fact]
    public void Missing_version_is_rejected()
    {
        var error = SingleError("{}");
        Assert.Equal(("formatVersion", "missing_field"), (error.Path, error.Code));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1.5")]
    [InlineData("\"1\"")]
    public void Zero_negative_or_non_integer_version_is_rejected(string version)
    {
        var error = SingleError($$"""{"formatVersion":{{version}}}""");

        Assert.Equal("formatVersion", error.Path);
        Assert.Contains(error.Code, new[] { "invalid_version", "wrong_type" });
    }

    [Fact]
    public void Newer_version_says_to_update_the_app_and_names_the_current_version()
    {
        var error = SingleError("""{"formatVersion":3}""");

        Assert.Equal(("formatVersion", "unsupported_version"), (error.Path, error.Code));
        Assert.Contains("update the app", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2", error.Message);
    }

    [Fact]
    public void Exercise_with_blank_name_names_the_exact_path()
    {
        var error = SingleError(Mutate("\"Goblet squat\"", "\"  \""));
        Assert.Equal(("exercises[0].name", "missing_field"), (error.Path, error.Code));
    }

    [Fact]
    public void Unknown_log_kind_lists_the_allowed_values()
    {
        var error = SingleError(Mutate("\"logKind\":\"load\"", "\"logKind\":\"weights\""));

        Assert.Equal(("exercises[0].logKind", "unknown_value"), (error.Path, error.Code));
        Assert.Contains("bodyweightLoad", error.Message);
    }

    [Fact]
    public void Wrong_type_for_benchmark_flag_is_reported()
    {
        var error = SingleError(Mutate("\"isBenchmark\":true", "\"isBenchmark\":\"yes\""));
        Assert.Equal(("exercises[0].isBenchmark", "wrong_type"), (error.Path, error.Code));
    }

    [Theory]
    [InlineData("has space")]
    [InlineData("")]
    [InlineData("under/score")]
    public void Badly_formatted_id_is_rejected(string id)
    {
        var error = SingleError(Mutate("\"goblet-squat\"", $"\"{id}\""));

        Assert.Equal("exercises[0].id", error.Path);
        Assert.Contains(error.Code, new[] { "invalid_id", "missing_field" });
    }

    [Fact]
    public void Duplicate_exercise_ids_name_the_second_occurrence()
    {
        var doc = TestData.Minimal();
        var twin = doc.Exercises![0] with { Name = "Other" };
        var json = Core.Serialisation.ExchangeFormat.Write(doc with { Exercises = [doc.Exercises[0], twin] });

        var error = SingleError(json);

        Assert.Equal(("exercises[1].id", "duplicate_id"), (error.Path, error.Code));
    }

    [Fact]
    public void Generated_guid_ids_are_valid()
    {
        var id = new GuidIdGenerator().NewId();
        var doc = TestData.Minimal() with { Exercises = [TestData.GobletSquat() with { Id = id }] };

        Assert.True(ExchangeFormat.Read(ExchangeFormat.Write(doc)).IsSuccess);
    }

    [Theory]
    [InlineData("\"restTimerSeconds\":90", "\"restTimerSeconds\":-1", "settings.restTimerSeconds")]
    [InlineData("\"weightIncrementKg\":2.5", "\"weightIncrementKg\":0", "settings.weightIncrementKg")]
    [InlineData("\"streakMinWorkoutsPerWeek\":3", "\"streakMinWorkoutsPerWeek\":0", "settings.streakMinWorkoutsPerWeek")]
    [InlineData("\"goalWeightKg\":82.5", "\"goalWeightKg\":-5", "settings.goalWeightKg")]
    public void Out_of_range_settings_are_reported_at_their_path(string from, string to, string path)
    {
        var error = SingleError(Mutate(from, to));
        Assert.Equal((path, "out_of_range"), (error.Path, error.Code));
    }

    [Fact]
    public void Missing_setting_and_bad_unit_are_both_reported()
    {
        var json = Mutate("\"restTimerSeconds\":90,", "").Replace("\"displayWeightUnit\":\"kg\"", "\"displayWeightUnit\":\"stone\"");

        var errors = ExchangeFormat.Read(json).Errors;

        Assert.Contains(errors, e => e.Path == "settings.restTimerSeconds" && e.Code == "missing_field");
        Assert.Contains(errors, e => e.Path == "settings.displayWeightUnit" && e.Code == "unknown_value");
    }

    [Fact]
    public void Unknown_fields_warn_but_do_not_fail()
    {
        var json = Mutate("\"name\":\"Goblet squat\"", "\"name\":\"Goblet squat\",\"colour\":\"red\"");

        var result = ExchangeFormat.Read(json);

        Assert.True(result.IsSuccess);
        var warning = Assert.Single(result.Warnings);
        Assert.Equal(("exercises[0].colour", "unknown_field"), (warning.Path, warning.Code));
    }

    [Fact]
    public void Several_errors_are_all_collected()
    {
        var json = Mutate("\"Goblet squat\"", "\"\"").Replace("\"logKind\":\"load\"", "\"logKind\":5");

        Assert.True(ExchangeFormat.Read(json).Errors.Count >= 2);
    }
}
