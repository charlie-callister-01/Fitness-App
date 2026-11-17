using System.Text.Json.Nodes;
using Core.Model;
using Core.Serialisation;

namespace Core.Tests.Serialisation;

/// <summary>The optional Week schedule collection: round trip, absent versus empty, and every validator rule with its exact path and code.</summary>
public class WeekScheduleTests
{
    private static readonly DateOnly Monday = new(2026, 11, 16);

    private static ExchangeDocument WithSchedules(params WeekSchedule[] schedules) =>
        PlanTestData.Rich() with { WeekSchedules = schedules };

    private static WeekSchedule Week(DateOnly weekStart, params SessionMove[] moves) => new(weekStart, moves);

    private static SessionMove Move(string sessionId, DateOnly date) => new(sessionId, date);

    private static ExchangeDocument Valid() => WithSchedules(
        Week(Monday, Move("a-lower", Monday.AddDays(2)), Move("c-engine", Monday.AddDays(6))),
        Week(Monday.AddDays(7)));

    private static (string Path, string Code) OnlyIssue(ExchangeDocument document)
    {
        var issue = Assert.Single(ExchangeFormat.Validate(document));
        return (issue.Path, issue.Code);
    }

    private static string Broken(Action<JsonObject> edit)
    {
        var root = JsonNode.Parse(ExchangeFormat.Write(Valid()))!.AsObject();
        edit(root);
        return root.ToJsonString();
    }

    private static JsonObject Week0(JsonObject root) => root["weekSchedules"]![0]!.AsObject();

    private static FormatIssue SingleError(string json)
    {
        var result = ExchangeFormat.Read(json);
        Assert.False(result.IsSuccess);
        return Assert.Single(result.Errors);
    }

    [Fact]
    public void Week_schedules_round_trip_to_identical_json_and_values()
    {
        var written = ExchangeFormat.Write(Valid());

        var result = ExchangeFormat.Read(written);

        Assert.True(result.IsSuccess, string.Join("; ", result.Errors.Select(e => e.Path + " " + e.Message)));
        Assert.Empty(result.Warnings);
        Assert.Equal(written, ExchangeFormat.Write(result.Document!));
        var schedules = result.Document!.WeekSchedules!;
        Assert.Equal(Monday, schedules[0].WeekStart);
        Assert.Equal(new SessionMove("c-engine", new DateOnly(2026, 11, 22)), schedules[0].Moves[1]);
        Assert.Empty(schedules[1].Moves);
    }

    [Fact]
    public void Dates_are_written_as_plain_calendar_dates()
    {
        var json = ExchangeFormat.Write(Valid());

        Assert.Contains("\"weekStart\":\"2026-11-16\"", json);
        Assert.Contains("{\"sessionId\":\"a-lower\",\"date\":\"2026-11-18\"}", json);
    }

    [Fact]
    public void Absent_means_none_and_is_not_written()
    {
        var json = ExchangeFormat.Write(PlanTestData.Rich());

        Assert.DoesNotContain("weekSchedules", json);
        Assert.Null(ExchangeFormat.Read(json).Document!.WeekSchedules);
    }

    [Fact]
    public void Present_but_empty_stays_empty()
    {
        var json = ExchangeFormat.Write(PlanTestData.Rich() with { WeekSchedules = [] });

        var result = ExchangeFormat.Read(json);

        Assert.Contains("\"weekSchedules\":[]", json);
        Assert.Empty(result.Document!.WeekSchedules!);
    }

    [Fact]
    public void A_week_without_a_moves_field_has_no_moves()
    {
        var json = Broken(root => Week0(root).Remove("moves"));

        var result = ExchangeFormat.Read(json);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Document!.WeekSchedules![0].Moves);
    }

    [Fact]
    public void Unknown_fields_are_warned_about_at_every_depth()
    {
        var json = Broken(root =>
        {
            Week0(root)["colour"] = "red";
            Week0(root)["moves"]![0]!["note"] = "x";
        });

        var result = ExchangeFormat.Read(json);

        Assert.True(result.IsSuccess);
        Assert.Equal(["weekSchedules[0].colour", "weekSchedules[0].moves[0].note"], result.Warnings.Select(w => w.Path).Order());
    }

    [Fact]
    public void Missing_week_start_is_reported()
    {
        var error = SingleError(Broken(root => Week0(root).Remove("weekStart")));

        Assert.Equal(("weekSchedules[0].weekStart", "missing_field"), (error.Path, error.Code));
    }

    [Fact]
    public void Bad_move_date_is_reported_once()
    {
        var error = SingleError(Broken(root => Week0(root)["moves"]![0]!["date"] = "next tuesday"));

        Assert.Equal(("weekSchedules[0].moves[0].date", "invalid_value"), (error.Path, error.Code));
    }

    [Fact]
    public void Missing_session_id_is_reported_once()
    {
        var error = SingleError(Broken(root => Week0(root)["moves"]![0]!.AsObject().Remove("sessionId")));

        Assert.Equal(("weekSchedules[0].moves[0].sessionId", "missing_field"), (error.Path, error.Code));
    }

    [Fact]
    public void A_valid_document_has_no_issues() => Assert.Empty(ExchangeFormat.Validate(Valid()));

    [Fact]
    public void Unknown_session_is_reported_at_the_move()
    {
        var document = WithSchedules(Week(Monday, Move("a-lower", Monday), Move("ghost", Monday.AddDays(1))));

        Assert.Equal(("weekSchedules[0].moves[1].sessionId", "unknown_session"), OnlyIssue(document));
    }

    [Fact]
    public void Sessions_are_not_checked_when_the_document_has_no_plans()
    {
        var document = WithSchedules(Week(Monday, Move("ghost", Monday.AddDays(1)))) with { Plans = null };

        Assert.Empty(ExchangeFormat.Validate(document));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    public void Move_date_outside_the_week_is_reported(int offset)
    {
        var document = WithSchedules(Week(Monday, Move("a-lower", Monday.AddDays(offset))));

        Assert.Equal(("weekSchedules[0].moves[0].date", "out_of_range"), OnlyIssue(document));
    }

    [Fact]
    public void Move_dates_on_the_first_and_last_day_of_the_week_are_valid()
    {
        var document = WithSchedules(Week(Monday, Move("a-lower", Monday), Move("c-engine", Monday.AddDays(6))));

        Assert.Empty(ExchangeFormat.Validate(document));
    }

    [Fact]
    public void Week_start_that_is_not_a_monday_is_reported_once()
    {
        var document = WithSchedules(Week(Monday.AddDays(1), Move("a-lower", Monday.AddDays(3))));

        Assert.Equal(("weekSchedules[0].weekStart", "invalid_value"), OnlyIssue(document));
    }

    [Fact]
    public void Duplicate_weeks_are_reported_at_the_second()
    {
        var document = WithSchedules(Week(Monday), Week(Monday.AddDays(7)), Week(Monday));

        Assert.Equal(("weekSchedules[2].weekStart", "duplicate_id"), OnlyIssue(document));
    }

    [Fact]
    public void The_same_session_moved_twice_in_a_week_is_reported_at_the_second_move()
    {
        var document = WithSchedules(Week(Monday, Move("a-lower", Monday), Move("c-engine", Monday), Move("a-lower", Monday.AddDays(3))));

        Assert.Equal(("weekSchedules[0].moves[2].sessionId", "duplicate_id"), OnlyIssue(document));
    }

    [Fact]
    public void The_same_session_may_move_in_different_weeks()
    {
        var document = WithSchedules(Week(Monday, Move("a-lower", Monday)), Week(Monday.AddDays(7), Move("a-lower", Monday.AddDays(8))));

        Assert.Empty(ExchangeFormat.Validate(document));
    }

    [Fact]
    public void Read_reports_validator_errors_for_a_document_from_json()
    {
        var json = ExchangeFormat.Write(WithSchedules(Week(Monday, Move("a-lower", Monday.AddDays(9)))));

        var error = SingleError(json);

        Assert.Equal(("weekSchedules[0].moves[0].date", "out_of_range"), (error.Path, error.Code));
    }
}
