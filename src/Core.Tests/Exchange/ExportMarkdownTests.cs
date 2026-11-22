using Core.Exchange;
using Core.Model;
using Core.Storage;

namespace Core.Tests.Exchange;

/// <summary>The Markdown Export: a read-only training log in a fixed section order, with labelled canonical units and no IDs outside the library table.</summary>
public class ExportMarkdownTests
{
    private static readonly DateRange NovemberToDecember = new(new DateOnly(2026, 11, 1), new DateOnly(2026, 12, 31));

    private static ExportRequest Everything() =>
        ExportRequest.Default(ExchangeTestData.Today) with { Range = NovemberToDecember, Guides = true, Format = ExportFormat.Markdown };

    /// <summary>The shared dataset with real names ("goblet-squat" becomes "Goblet Squat"), so an ID leaking into the text is detectable.</summary>
    private static DataSnapshot Snapshot()
    {
        var snapshot = ExchangeTestData.Snapshot();
        return snapshot with { Exercises = [.. snapshot.Exercises.Select(e => e with { Name = Named(e) })] };
    }

    private static string Named(Exercise exercise) => exercise.Id switch
    {
        "goblet-squat" => "Goblet Squat",
        "farmers-carry" => "Farmers Carry",
        "5k-run" => "Five K Run",
        "rdl" => "Romanian Deadlift",
        "waist" => "Waist",
        _ => exercise.Name,
    };

    private static string Render(ExportRequest? request = null) =>
        Exporter.Export(Snapshot(), request ?? Everything(), "test", ExchangeTestData.Now).Text;

    /// <summary>The lines under a "## " heading, up to the next one.</summary>
    private static string Section(string text, string heading) =>
        string.Join('\n', text.Split('\n').SkipWhile(l => l.TrimEnd() != heading).Skip(1).TakeWhile(l => !l.StartsWith("## ", StringComparison.Ordinal)));

    /// <summary>The lines under a "### " Workout heading that starts with the given date text, up to the next heading.</summary>
    private static string Workout(string text, string dayHeading) =>
        string.Join('\n', Section(text, "## Workouts").Split('\n').SkipWhile(l => !l.StartsWith($"### {dayHeading}", StringComparison.Ordinal)).Skip(1).TakeWhile(l => !l.StartsWith("### ", StringComparison.Ordinal)));

    [Fact]
    public void Sections_appear_in_the_fixed_order()
    {
        var text = Render();

        var headings = text.Split('\n').Where(l => l.StartsWith("## ", StringComparison.Ordinal)).Select(l => l.TrimEnd()).ToList();

        Assert.Equal(["## Plan", "## Workouts", "## Weigh-ins", "## Exercise library", "## Guides"], headings);
        Assert.StartsWith("# Training export", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Workouts_are_listed_by_date_under_their_Session_name_with_Sets_RPE_skips_swaps_and_the_Pain_check()
    {
        var text = Render();

        Assert.True(text.IndexOf("### Tue 10 Nov 2026", StringComparison.Ordinal) < text.IndexOf("### Mon 16 Nov 2026", StringComparison.Ordinal));
        Assert.Equal(
            """
            - Goblet Squat: 24.5 kg x 10; 28 kg x 3 (max), RPE 8
            - Farmers Carry: skipped (Grip was gone)
            - Five K Run (swapped from Romanian Deadlift): 5 km in 25:30, RPE 6
            - Pain check: Knee, Lower back, moderate, during Goblet Squat: "Twinge on the way up"
            """.ReplaceLineEndings("\n"),
            Workout(text, "Mon 16 Nov 2026: A: Lower + core").Trim().ReplaceLineEndings("\n"));
    }

    [Fact]
    public void Custom_and_unlogged_Workouts_and_a_clear_Pain_check_read_plainly()
    {
        var text = Render();

        Assert.Equal(
            """
            - Farmers Carry: 32 kg x 40 m in 1:00
            - Waist: 86.5 cm
            - Pain check: none
            """.ReplaceLineEndings("\n"),
            Workout(text, "Tue 10 Nov 2026: Custom workout (logged afterwards)").Trim().ReplaceLineEndings("\n"));
        Assert.Contains("- Nothing logged. Planned: Five K Run", Workout(text, "Tue 17 Nov 2026: C: Engine"));
    }

    [Fact]
    public void The_Plan_lists_its_Phases_and_Sessions_with_their_rows()
    {
        var plan = Section(Render(), "## Plan");

        Assert.Contains("### HYROX London", plan);
        Assert.Contains("- Goal: HYROX London Doubles on Sat 24 Apr 2027", plan);
        Assert.Contains("- Weeks 1-8: Foundation. Goal: Goblet Squat 24 kg x 10", plan);
        Assert.Contains("- Weeks 9-19: Build. A: Lower + core becomes: Romanian Deadlift: 4 x 6 (Hinge, don't squat)", plan);
        Assert.Contains("#### A: Lower + core (Monday, Strength)", plan);
        Assert.Contains("- Goblet Squat: 3 x 8-12 (Chest tall)", plan);
        Assert.Contains("- Farmers Carry: 3 sets", plan);
        Assert.Contains("#### Benchmark test (Saturday, Test, every 4 weeks)", plan);
    }

    [Fact]
    public void Weigh_ins_are_a_table_in_kg_oldest_first_with_no_averages()
    {
        var weighIns = Section(Render(), "## Weigh-ins");

        Assert.Equal(
            """
            | Date | Weight (kg) |
            | --- | --- |
            | Mon 16 Nov 2026 | 84.35 |
            | Tue 17 Nov 2026 | 83.1 |
            """.ReplaceLineEndings("\n"),
            weighIns.Trim().ReplaceLineEndings("\n"));
    }

    [Fact]
    public void The_Exercise_library_is_a_table_of_ID_name_log_kind_and_tags_with_pipes_escaped()
    {
        var library = Section(Render(), "## Exercise library");

        Assert.Contains("| ID | Name | Log kind | Tags |", library);
        Assert.Contains("| goblet-squat | Goblet Squat | Load | muscles: quads, Gesäß; equipment: kettlebell; patterns: squat |", library);
        Assert.Contains("| farmers-carry | Farmers Carry | Carry |  |", library);

        var piped = ExchangeTestData.Snapshot();
        var text = Exporter.Export(piped with { Exercises = [.. piped.Exercises.Select(e => e.Id == "rdl" ? e with { Name = "A|B" } : e)] }, Everything(), "t", ExchangeTestData.Now).Text;
        Assert.Contains("| rdl | A\\|B | Load |", text);
    }

    [Fact]
    public void Guides_show_their_content_by_Exercise_name_and_never_the_personal_notes()
    {
        var guides = Section(Render(), "## Guides");

        Assert.Contains("### Goblet Squat", guides);
        Assert.Contains("- Why it is in the plan: Why — ünïcode", guides);
        Assert.Contains("- Cues: Chest up; Knees out", guides);
        Assert.Contains("- Video: https://example.com/v?x=1&y=2", guides);
        Assert.DoesNotContain("Notes: 膝", guides);
    }

    [Fact]
    public void No_ID_appears_outside_the_library_table()
    {
        var snapshot = ExchangeTestData.Snapshot();
        var ids = snapshot.Exercises.Select(e => e.Id)
            .Concat(snapshot.Plans.SelectMany(p => new[] { p.Id }
                .Concat(p.Tracks.SelectMany(t => t.Phases).Select(ph => ph.Id))
                .Concat(p.Sessions.Select(s => s.Id))))
            .Concat(snapshot.Workouts.SelectMany(w => new[] { w.Id }.Concat(w.Entries.SelectMany(e => e.Sets.Select(s => s.Id))).Concat(w.PainCheck is { } p ? [p.Id] : [])))
            .Concat(snapshot.WeighIns.Select(w => w.Id))
            .Where(id => id != "waist")
            .ToList();

        var rendered = Render();
        var text = rendered.Replace(Section(rendered, "## Exercise library"), "");

        Assert.All(ids, id => Assert.DoesNotMatch($@"(?<![\w-]){System.Text.RegularExpressions.Regex.Escape(id)}(?![\w-])", text));
    }

    [Fact]
    public void Units_are_always_canonical_and_labelled_whatever_the_display_Setting()
    {
        var text = Render();

        Assert.DoesNotMatch(@"\b(lb|lbs|mi|miles)\b", text);
        Assert.Contains("kg", text);
        Assert.Contains("- Units: weights in kg, distances in km or m, durations as m:ss", text);
    }

    [Fact]
    public void The_header_states_the_range_and_what_is_included()
    {
        var text = Render(Everything() with { PainChecks = false, Guides = false });

        Assert.Contains("- 1 Nov to 31 Dec 2026", text);
        Assert.Contains("- Not included: Pain checks, Guides", text);
        Assert.DoesNotContain("Not included", Render(Everything() with { Exercises = true }));
    }

    [Fact]
    public void Unticked_sections_are_left_out()
    {
        var text = Render(Everything() with { Guides = false, WeighIns = false, Plan = false });

        Assert.DoesNotContain("## Guides", text);
        Assert.DoesNotContain("## Weigh-ins", text);
        Assert.DoesNotContain("## Plan", text);
        Assert.Contains("## Workouts", text);
    }
}
