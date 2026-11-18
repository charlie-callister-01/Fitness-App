using System.Text.Json.Nodes;
using Core.Exchange;
using Core.Model;
using Core.Serialisation;
using Core.Tests.Serialisation;

namespace Core.Tests.Exchange;

public class InspectTests
{
    private const string FenceTicks = "```";

    private static string Json(ExchangeDocument document) => ExchangeFormat.Write(document);

    private static Inspected Accepted(string text)
    {
        var result = ImportInput.Inspect(text);
        Assert.True(result.IsAccepted, string.Join("; ", result.Rejections));
        Assert.Empty(result.Rejections);
        return Assert.IsType<Inspected>(result.Inspected);
    }

    private static InspectResult Rejected(string text)
    {
        var result = ImportInput.Inspect(text);
        Assert.False(result.IsAccepted);
        Assert.Null(result.Inspected);
        return result;
    }

    [Fact]
    public void A_clean_document_is_read_with_no_notices_or_notes()
    {
        var inspected = Accepted(Json(TestData.Minimal()));

        Assert.Empty(inspected.Notices);
        Assert.Empty(inspected.Notes);
        Assert.Equal(ExchangeFormat.CurrentVersion, inspected.Document.FormatVersion);
    }

    [Fact]
    public void A_code_fenced_reply_is_unwrapped_with_a_notice()
    {
        var inspected = Accepted($"Here you go:\n{FenceTicks}json\n{Json(TestData.Minimal())}\n{FenceTicks}\nHope that helps!");

        Assert.Equal(["Ignored text around the JSON."], inspected.Notices);
    }

    [Fact]
    public void A_fence_with_no_language_tag_and_windows_line_endings_is_unwrapped()
    {
        var inspected = Accepted($"{FenceTicks}\r\n{Json(TestData.Minimal())}\r\n{FenceTicks}");

        Assert.Single(inspected.Notices);
    }

    [Fact]
    public void A_sentence_before_and_after_the_json_is_ignored_with_a_notice()
    {
        var inspected = Accepted($"Sure! Here is the file {Json(TestData.Minimal())} Let me know if you want changes.");

        Assert.Equal(["Ignored text around the JSON."], inspected.Notices);
    }

    [Fact]
    public void Trailing_text_after_a_document_that_starts_cleanly_is_ignored_with_a_notice()
    {
        var inspected = Accepted($"{Json(TestData.Minimal())}\nThanks!");

        Assert.Single(inspected.Notices);
    }

    [Fact]
    public void A_byte_order_mark_is_removed_silently()
    {
        var inspected = Accepted("﻿" + Json(TestData.Minimal()));

        Assert.Empty(inspected.Notices);
    }

    [Fact]
    public void A_byte_order_mark_before_a_fence_is_handled()
    {
        var inspected = Accepted($"﻿{FenceTicks}json\n{Json(TestData.Minimal())}\n{FenceTicks}");

        Assert.Single(inspected.Notices);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("just some words")]
    [InlineData("[1,2]")]
    public void Unreadable_text_is_rejected_at_the_root(string text)
    {
        var result = Rejected(text);

        var line = Assert.Single(result.Rejections);
        Assert.StartsWith("$: ", line);
    }

    [Fact]
    public void Rejections_show_the_exact_path_then_the_message()
    {
        var root = JsonNode.Parse(Json(TestData.Minimal()))!.AsObject();
        root["exercises"]![0]!.AsObject().Remove("name");

        var result = Rejected(root.ToJsonString());

        var issue = Assert.Single(result.Issues);
        Assert.Equal($"{issue.Path}: {issue.Message}", Assert.Single(result.Rejections));
        Assert.Equal("exercises[0].name", issue.Path);
        Assert.Equal("missing_field", issue.Code);
    }

    [Fact]
    public void More_than_ten_issues_are_cut_with_a_count_of_the_rest()
    {
        var exercises = new JsonArray();
        for (var i = 0; i < 14; i++)
        {
            exercises.Add(new JsonObject());
        }

        var root = JsonNode.Parse(Json(TestData.Minimal()))!.AsObject();
        root["exercises"] = exercises;

        var result = Rejected(root.ToJsonString());

        Assert.True(result.Issues.Count > 11);
        Assert.Equal(11, result.Rejections.Count);
        Assert.Equal($"and {result.Issues.Count - 10} more", result.Rejections[10]);
        Assert.Equal($"{result.Issues[0].Path}: {result.Issues[0].Message}", result.Rejections[0]);
    }

    [Fact]
    public void Exactly_ten_issues_are_all_shown_with_no_and_more_line()
    {
        var exercises = new JsonArray();
        for (var i = 0; i < 10; i++)
        {
            exercises.Add(new JsonObject { ["id"] = $"e{i}", ["name"] = $"E{i}", ["logKind"] = "nonsense" });
        }

        var root = JsonNode.Parse(Json(TestData.Minimal()))!.AsObject();
        root["exercises"] = exercises;

        var result = Rejected(root.ToJsonString());

        Assert.Equal(10, result.Issues.Count);
        Assert.Equal(10, result.Rejections.Count);
    }

    [Fact]
    public void A_newer_format_version_is_refused_and_says_to_update_the_app()
    {
        var root = JsonNode.Parse(Json(TestData.Minimal()))!.AsObject();
        root["formatVersion"] = ExchangeFormat.CurrentVersion + 1;

        var result = Rejected(root.ToJsonString());

        var issue = Assert.Single(result.Issues);
        Assert.Equal("unsupported_version", issue.Code);
        Assert.Contains("update the app", Assert.Single(result.Rejections), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void An_older_format_version_reads_as_normal()
    {
        // Version 1 is the oldest version and the current build's; a document at that version must read with no rejection.
        var root = JsonNode.Parse(Json(TestData.Minimal()))!.AsObject();
        root["formatVersion"] = 1;

        Accepted(root.ToJsonString());
    }

    [Fact]
    public void Unknown_fields_become_notes_not_errors()
    {
        var root = JsonNode.Parse(Json(WholeDatasetTests.Everything()))!.AsObject();
        root["plans"]![0]!.AsObject()["colour"] = "red";

        var inspected = Accepted(root.ToJsonString());

        Assert.Equal(["ignored: plans[0].colour"], inspected.Notes);
    }

    [Fact]
    public void A_complete_backup_suggests_restore_even_without_week_schedules()
    {
        var everything = WholeDatasetTests.Everything();

        Assert.Equal(ImportKind.Restore, Accepted(Json(everything with { WeekSchedules = null })).SuggestedKind);
        Assert.Equal(ImportKind.Restore, Accepted(Json(everything)).SuggestedKind);
    }

    [Fact]
    public void A_backup_missing_a_collection_or_settings_suggests_merge()
    {
        var everything = WholeDatasetTests.Everything();

        Assert.Equal(ImportKind.Merge, Accepted(Json(everything with { Badges = null })).SuggestedKind);
        Assert.Equal(ImportKind.Merge, Accepted(Json(everything with { Settings = null })).SuggestedKind);
    }

    [Fact]
    public void A_plans_only_file_suggests_plan_with_or_without_exercises()
    {
        var rich = PlanTestData.Rich();

        Assert.Equal(ImportKind.Plan, Accepted(Json(rich)).SuggestedKind);
        Assert.Equal(ImportKind.Plan, Accepted(Json(rich with { Exercises = null })).SuggestedKind);
    }

    [Fact]
    public void A_guides_only_file_suggests_guides_with_or_without_exercises()
    {
        var everything = WholeDatasetTests.Everything();
        var guidesOnly = new ExchangeDocument(1, null, null, everything.Exercises, null, Guides: everything.Guides);

        Assert.NotEmpty(everything.Guides!);
        Assert.Equal(ImportKind.Guides, Accepted(Json(guidesOnly)).SuggestedKind);
        Assert.Equal(ImportKind.Guides, Accepted(Json(guidesOnly with { Exercises = null })).SuggestedKind);
    }

    [Fact]
    public void A_mixed_or_other_file_suggests_merge()
    {
        var everything = WholeDatasetTests.Everything();

        Assert.Equal(ImportKind.Merge, Accepted(Json(WorkoutTestData.Rich())).SuggestedKind);
        Assert.Equal(ImportKind.Merge, Accepted(Json(everything with { Guides = null, Plans = null })).SuggestedKind);
        Assert.Equal(ImportKind.Merge, Accepted(Json(TestData.Minimal())).SuggestedKind);
    }
}
