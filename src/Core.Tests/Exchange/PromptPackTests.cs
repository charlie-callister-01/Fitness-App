using Core.Exchange;
using Core.Model;
using Core.Serialisation;
using Core.Storage;

namespace Core.Tests.Exchange;

public class PromptPackTests
{
    private static readonly Exercise Goblet = new("goblet-squat", "Goblet squat", LogKind.Load,
        new ExerciseTags(["quads"], ["kettlebell"], ["squat"]), false);

    private static readonly Exercise Carry = new("farmers-carry", "Farmers carry", LogKind.Carry, null, false);
    private static readonly Exercise Rdl = new("rdl", "Romanian deadlift", LogKind.Load, null, false);

    private static DataSnapshot Library(params Guide[] guides) =>
        new() { Exercises = [Goblet, Carry, Rdl], Guides = guides };

    private static Guide GuideFor(string exerciseId) =>
        new(exerciseId, "Quads", null, null, null, [], [], null, null, [], null, null);

    [Fact]
    public void WritePlan_carries_the_instructions_reply_rules_and_blanks()
    {
        var prompt = PromptPack.WritePlan(Library());

        Assert.Contains("Plan", prompt);
        Assert.Contains("reply with one JSON object and nothing else", prompt);
        Assert.Contains("use these Exercise IDs", prompt);
        Assert.Contains("lowercase-hyphen", prompt);
        Assert.Contains("[goal]", prompt);
        Assert.Contains("[days a week]", prompt);
        Assert.Contains("[equipment]", prompt);
        Assert.Contains("[injuries]", prompt);
    }

    [Fact]
    public void WritePlan_lists_every_Exercise_in_the_library_with_its_ID_name_and_log_kind()
    {
        var prompt = PromptPack.WritePlan(Library());

        Assert.Contains("\"id\":\"goblet-squat\"", prompt);
        Assert.Contains("\"name\":\"Goblet squat\"", prompt);
        Assert.Contains("\"logKind\":\"load\"", prompt);
        Assert.Contains("\"tags\":{\"muscleGroups\":[\"quads\"],\"equipment\":[\"kettlebell\"],\"movementPatterns\":[\"squat\"]}", prompt);
        Assert.Contains("\"id\":\"farmers-carry\"", prompt);
        Assert.Contains("\"logKind\":\"carry\"", prompt);
        Assert.Contains("\"id\":\"rdl\"", prompt);
    }

    [Fact]
    public void WritePlan_generates_its_enum_lists_from_the_C_sharp_enums()
    {
        var prompt = PromptPack.WritePlan(Library());

        Assert.Contains(string.Join(", ", Enum.GetNames<LogKind>().Select(CamelCase)), prompt);
        Assert.Contains(string.Join(", ", Enum.GetNames<SessionType>().Select(CamelCase)), prompt);
        Assert.Contains(string.Join(", ", Enum.GetNames<TrackCadence>().Select(CamelCase)), prompt);
        Assert.Contains("monday, tuesday, wednesday, thursday, friday, saturday, sunday, daily", prompt);
    }

    [Fact]
    public void WritePlan_describes_Tracks_and_states_the_first_Track_is_main_and_must_be_weekly()
    {
        var prompt = PromptPack.WritePlan(Library());

        Assert.Contains("`tracks`", prompt);
        Assert.Contains("`trackId`", prompt);
        Assert.Contains("`dayDonePercent`", prompt);
        Assert.Contains("first Track is the main Track and must be `weekly`", prompt);
    }

    private static string CamelCase(string name) => char.ToLowerInvariant(name[0]) + name[1..];

    [Fact]
    public void WritePlan_includes_the_worked_example_and_no_carriage_returns()
    {
        var prompt = PromptPack.WritePlan(Library());

        Assert.Contains(PromptPack.PlanExample, prompt);
        Assert.DoesNotContain('\r', prompt);
    }

    [Fact]
    public void WriteGuides_defaults_to_the_Exercises_with_no_Guide_content()
    {
        var snapshot = Library(GuideFor("goblet-squat"), new Guide("rdl", null, null, null, null, [], [], null, null, [], null, "my notes"));

        var prompt = PromptPack.WriteGuides(snapshot);

        Assert.DoesNotContain("\"id\":\"goblet-squat\"", prompt);
        Assert.Contains("\"id\":\"farmers-carry\"", prompt);
        Assert.Contains("\"id\":\"rdl\"", prompt);
    }

    [Fact]
    public void WriteGuides_writes_for_exactly_the_chosen_Exercises_and_ignores_unknown_IDs()
    {
        var prompt = PromptPack.WriteGuides(Library(GuideFor("goblet-squat")), ["goblet-squat", "nope"]);

        Assert.Contains("\"id\":\"goblet-squat\"", prompt);
        Assert.DoesNotContain("\"id\":\"farmers-carry\"", prompt);
        Assert.DoesNotContain("nope", prompt);
    }

    [Fact]
    public void WriteGuides_carries_the_reply_rules_and_the_worked_example()
    {
        var prompt = PromptPack.WriteGuides(Library());

        Assert.Contains("reply with one JSON object and nothing else", prompt);
        Assert.Contains("Do not invent personal notes", prompt);
        Assert.Contains("using its exact ID as `exerciseId`", prompt);
        Assert.Contains(PromptPack.GuidesExample, prompt);
        Assert.Contains("[goal]", prompt);
        Assert.DoesNotContain('\r', prompt);
    }

    [Fact]
    public void The_Plan_example_reads_validates_and_previews_as_a_Plan_Import_with_no_blockers()
    {
        var inspected = Inspect(PromptPack.PlanExample);

        Assert.Empty(ExchangeFormat.Validate(inspected.Document));
        Assert.Empty(inspected.Notes);
        var preview = ImportPreviewer.Preview(Library(), inspected, ImportKind.Plan, ImportDecisions.None);

        Assert.True(preview.IsReady, string.Join("; ", preview.Blockers.Select(b => b.Text)));
        Assert.Contains(preview.Items, i => i.Action == ImportItemAction.Add && i.Text.Contains("Cat-cow"));
        Assert.Contains(preview.Items, i => i.Action == ImportItemAction.Add && i.Text.Contains("Hip flexor stretch"));
        Assert.Contains(preview.Items, i => i.Action == ImportItemAction.Add && i.Text.Contains("HYROX London") && i.Text.Contains("2 Tracks"));
        var plan = Assert.Single(inspected.Document.Plans!);
        Assert.Equal(2, plan.Tracks.Count);
        Assert.Equal(TrackCadence.Daily, plan.Tracks[1].Rhythm.Cadence);
    }

    [Fact]
    public void The_Plan_example_is_suggested_as_a_Plan_Import()
    {
        Assert.Equal(ImportKind.Plan, Inspect(PromptPack.PlanExample).SuggestedKind);
    }

    [Fact]
    public void The_Guides_example_reads_validates_and_previews_as_a_Guides_Import_with_no_blockers()
    {
        var inspected = Inspect(PromptPack.GuidesExample);

        Assert.Empty(ExchangeFormat.Validate(inspected.Document));
        Assert.Empty(inspected.Notes);
        Assert.Equal(ImportKind.Guides, inspected.SuggestedKind);
        var preview = ImportPreviewer.Preview(Library(), inspected, ImportKind.Guides, ImportDecisions.None);

        Assert.True(preview.IsReady, string.Join("; ", preview.Blockers.Select(b => b.Text)));
        Assert.Equal(2, preview.Items.Count(i => i.Action == ImportItemAction.Add));
    }

    private static Inspected Inspect(string text)
    {
        var result = ImportInput.Inspect(text);
        Assert.True(result.IsAccepted, string.Join("; ", result.Rejections));
        return result.Inspected!;
    }

    [Fact]
    public void Analyse_states_the_range_and_the_units()
    {
        var export = new ExportResult("{}", "x.json", [], false);

        var ranged = PromptPack.Analyse(ExchangeTestData.Snapshot(), export, new DateRange(new DateOnly(2026, 8, 26), new DateOnly(2026, 11, 17)));
        var everything = PromptPack.Analyse(ExchangeTestData.Snapshot(), export, null);

        Assert.Contains("2026-08-26 to 2026-11-17", ranged);
        Assert.Contains("all time", everything);
        Assert.Contains("kg", ranged);
        Assert.Contains("m:ss", ranged);
        Assert.Contains("[goal]", ranged);
    }

    [Fact]
    public void Analyse_asks_for_a_plain_reply_not_JSON()
    {
        var prompt = PromptPack.Analyse(ExchangeTestData.Snapshot(), new ExportResult("{}", "x.json", [], false), null);

        Assert.Contains("Reply in plain text", prompt);
        Assert.DoesNotContain("JSON object", prompt);
    }

    [Fact]
    public void Analyse_embeds__the_export_text_verbatim_after_the_instructions()
    {
        var request = ExportRequest.Default(ExchangeTestData.Today);
        var export = Exporter.Export(ExchangeTestData.Snapshot(), request, "test", ExchangeTestData.Now);

        var prompt = PromptPack.Analyse(ExchangeTestData.Snapshot(), export, request.Range);

        Assert.EndsWith(export.Text, prompt.TrimEnd('\r', '\n'));
        Assert.True(prompt.IndexOf(export.Text, StringComparison.Ordinal) > prompt.IndexOf("analyse", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Analyse_carries_the_Exercise_library_so_IDs_line_up()
    {
        var snapshot = ExchangeTestData.Snapshot();

        var prompt = PromptPack.Analyse(snapshot, new ExportResult("{}", "x.json", [], false), null);

        // Names in the test data are non-ASCII and the JSON escapes them, so check the IDs and that every Exercise has a name.
        Assert.All(snapshot.Exercises, e => Assert.Contains($"\"id\":\"{e.Id}\"", prompt));
        Assert.Equal(snapshot.Exercises.Count, prompt.Split("\"name\":").Length - 1);
    }
}
