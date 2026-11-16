using Core.Model;
using Core.Serialisation;

namespace Core.Tests.Serialisation;

public class BodyAndGuideTests
{
    private static Guide FullGuide() => new(
        ExerciseId: "goblet-squat",
        Works: "Quads and glutes",
        WhyInPlan: "Builds squat strength for the sled push",
        Setup: "Hold the bell at your chest",
        HowTo: "Sit between your heels, then stand",
        Cues: ["Chest up", "Knees out"],
        CommonMistakes: ["Heels lifting"],
        Easier: "Box squat",
        Harder: "Heavier bell",
        WatchOuts: ["Lower back rounding"],
        VideoUrl: "https://example.com/goblet",
        PersonalNotes: "Left knee clicks");

    private static ExchangeDocument Rich() => TestData.Minimal() with
    {
        WeighIns = [new WeighIn("w1", new DateOnly(2026, 11, 16), 84.35), new WeighIn("w2", new DateOnly(2026, 11, 17), 84.1)],
        Breaks = [new Break("b1", new DateOnly(2026, 12, 20), new DateOnly(2027, 1, 2), "Holiday")],
        Badges = [new Badge("badge-1", "100 kg squat", "maxWeightKg", 100)],
        Guides = [FullGuide()],
    };

    private static ExchangeDocument Partial(Func<ExchangeDocument, ExchangeDocument> shape) =>
        shape(new ExchangeDocument(1, null, null, null, null));

    private static FormatIssue SingleError(string json)
    {
        var result = ExchangeFormat.Read(json);
        Assert.False(result.IsSuccess);
        return Assert.Single(result.Errors);
    }

    private static string Mutate(ExchangeDocument doc, string from, string to)
    {
        var json = ExchangeFormat.Write(doc);
        Assert.Contains(from, json);
        return json.Replace(from, to);
    }

    [Fact]
    public void Rich_dataset_round_trips_to_identical_json_and_values()
    {
        var written = ExchangeFormat.Write(Rich());

        var result = ExchangeFormat.Read(written);

        Assert.True(result.IsSuccess, string.Join("; ", result.Errors.Select(e => e.Path + " " + e.Message)));
        Assert.Empty(result.Warnings);
        Assert.Equal(written, ExchangeFormat.Write(result.Document!));
        Assert.Equal(new DateOnly(2026, 11, 16), result.Document!.WeighIns![0].Date);
        Assert.Equal(84.35, result.Document.WeighIns[0].WeightKg);
        Assert.Equal("Holiday", result.Document.Breaks![0].Reason);
        Assert.Equal(100, result.Document.Badges![0].Target);
        Assert.Equal("Left knee clicks", result.Document.Guides![0].PersonalNotes);
        Assert.Equal(["Chest up", "Knees out"], result.Document.Guides[0].Cues);
    }

    [Fact]
    public void Dates_are_written_as_plain_calendar_dates()
    {
        var json = ExchangeFormat.Write(Rich());

        Assert.Contains("\"date\":\"2026-11-16\"", json);
        Assert.Contains("\"startDate\":\"2026-12-20\"", json);
    }

    [Fact]
    public void Guide_content_and_personal_notes_are_separate_fields()
    {
        var withoutNotes = Partial(d => d with { Guides = [FullGuide() with { PersonalNotes = null }] });

        var json = ExchangeFormat.Write(withoutNotes);

        Assert.DoesNotContain("personalNotes", json);
        Assert.Null(ExchangeFormat.Read(json).Document!.Guides![0].PersonalNotes);
    }

    [Fact]
    public void Guides_only_document_is_accepted()
    {
        var json = ExchangeFormat.Write(Partial(d => d with { Guides = [FullGuide()] }));

        var result = ExchangeFormat.Read(json);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Document!.Exercises);
        Assert.Single(result.Document.Guides!);
    }

    [Fact]
    public void Weigh_ins_only_document_is_accepted()
    {
        var json = ExchangeFormat.Write(Partial(d => d with { WeighIns = [new WeighIn("w1", new DateOnly(2026, 1, 5), 90)] }));

        Assert.True(ExchangeFormat.Read(json).IsSuccess);
    }

    [Fact]
    public void Guide_with_only_an_exercise_id_is_valid()
    {
        var json = """{"formatVersion":1,"guides":[{"exerciseId":"goblet-squat"}]}""";

        Assert.True(ExchangeFormat.Read(json).IsSuccess);
    }

    [Fact]
    public void Guide_for_an_unknown_exercise_is_rejected_when_the_document_has_exercises()
    {
        var doc = Rich() with { Guides = [FullGuide() with { ExerciseId = "mystery-lift" }] };

        var error = SingleError(ExchangeFormat.Write(doc));

        Assert.Equal(("guides[0].exerciseId", "unknown_exercise"), (error.Path, error.Code));
        Assert.Contains("mystery-lift", error.Message);
    }

    [Fact]
    public void Two_guides_for_one_exercise_are_rejected()
    {
        var doc = Partial(d => d with { Guides = [FullGuide(), FullGuide()] });

        var error = SingleError(ExchangeFormat.Write(doc));

        Assert.Equal(("guides[1].exerciseId", "duplicate_id"), (error.Path, error.Code));
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("ftp://example.com/x")]
    [InlineData("/relative/path")]
    public void Bad_video_url_is_rejected(string url)
    {
        var error = SingleError(Mutate(Rich(), "https://example.com/goblet", url));

        Assert.Equal(("guides[0].videoUrl", "invalid_value"), (error.Path, error.Code));
    }

    [Fact]
    public void Break_ending_before_it_starts_is_rejected_at_the_end_date()
    {
        var error = SingleError(Mutate(Rich(), "\"endDate\":\"2027-01-02\"", "\"endDate\":\"2026-12-19\""));

        Assert.Equal(("breaks[0].endDate", "end_before_start"), (error.Path, error.Code));
    }

    [Fact]
    public void One_day_break_is_valid()
    {
        Assert.True(ExchangeFormat.Read(Mutate(Rich(), "\"endDate\":\"2027-01-02\"", "\"endDate\":\"2026-12-20\"")).IsSuccess);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-3")]
    public void Non_positive_weigh_in_is_rejected(string kg)
    {
        var error = SingleError(Mutate(Rich(), "\"weightKg\":84.35", $"\"weightKg\":{kg}"));

        Assert.Equal(("weighIns[0].weightKg", "out_of_range"), (error.Path, error.Code));
    }

    [Theory]
    [InlineData("2026-13-01")]
    [InlineData("16/11/2026")]
    [InlineData("2026-11-16T09:00:00+00:00")]
    public void Malformed_date_is_rejected_with_the_expected_format(string date)
    {
        var error = SingleError(Mutate(Rich(), "\"2026-11-16\"", $"\"{date}\""));

        Assert.Equal(("weighIns[0].date", "invalid_value"), (error.Path, error.Code));
        Assert.Contains("yyyy-MM-dd", error.Message);
    }

    [Fact]
    public void Duplicate_weigh_in_ids_name_the_second_one()
    {
        var doc = Partial(d => d with { WeighIns = [new WeighIn("w1", new DateOnly(2026, 1, 5), 90), new WeighIn("w1", new DateOnly(2026, 1, 6), 90)] });

        var error = SingleError(ExchangeFormat.Write(doc));

        Assert.Equal(("weighIns[1].id", "duplicate_id"), (error.Path, error.Code));
    }

    [Fact]
    public void Badge_needs_a_name_metric_and_positive_target()
    {
        var json = """{"formatVersion":1,"badges":[{"id":"b","name":" ","metric":"","target":0}]}""";

        var errors = ExchangeFormat.Read(json).Errors;

        Assert.Contains(errors, e => e is { Path: "badges[0].name", Code: "missing_field" });
        Assert.Contains(errors, e => e is { Path: "badges[0].metric", Code: "missing_field" });
        Assert.Contains(errors, e => e is { Path: "badges[0].target", Code: "out_of_range" });
    }

    [Fact]
    public void Break_without_a_reason_is_rejected()
    {
        var error = SingleError(Mutate(Rich(), "\"reason\":\"Holiday\"", "\"reason\":\"\""));

        Assert.Equal(("breaks[0].reason", "missing_field"), (error.Path, error.Code));
    }

    [Fact]
    public void Badge_progress_is_not_part_of_the_format()
    {
        var json = Mutate(Rich(), "\"target\":100", "\"target\":100,\"progress\":42");

        var result = ExchangeFormat.Read(json);

        Assert.True(result.IsSuccess);
        Assert.Equal("badges[0].progress", Assert.Single(result.Warnings).Path);
    }
}
