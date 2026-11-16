using System.Text.RegularExpressions;
using Core.Serialisation;

namespace Core.Tests.Serialisation;

public partial class DataSpecConformanceTests
{
    [GeneratedRegex(@"```json\r?\n(?<json>.*?)\r?\n```", RegexOptions.Singleline)]
    private static partial Regex JsonBlock();

    private static string SpecPath()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "docs", "data-spec.md");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException("docs/data-spec.md not found above the test output directory.");
    }

    [Fact]
    public void Plan_example_in_the_data_spec_is_read_cleanly_by_the_real_reader()
    {
        var example = JsonBlock().Matches(File.ReadAllText(SpecPath())).Select(m => m.Groups["json"].Value).Single(j => j.Contains("\"plans\""));

        var result = ExchangeFormat.Read(example);

        Assert.True(result.IsSuccess, string.Join("; ", result.Errors.Select(e => $"{e.Path}: {e.Message}")));
        Assert.Empty(result.Warnings);
        var plan = Assert.Single(result.Document!.Plans!);
        Assert.Equal((2, 2), (plan.Phases.Count, plan.Sessions.Count));
    }

    [Fact]
    public void Worked_example_in_the_data_spec_is_read_cleanly_by_the_real_reader()
    {
        var example = JsonBlock().Matches(File.ReadAllText(SpecPath())).Select(m => m.Groups["json"].Value).First(j => j.Contains("\"exportedAt\""));
        Assert.NotEmpty(example);

        var result = ExchangeFormat.Read(example);

        Assert.True(result.IsSuccess, string.Join("; ", result.Errors.Select(e => $"{e.Path}: {e.Message}")));
        Assert.Empty(result.Warnings);
        Assert.Equal(3, result.Document!.Exercises!.Count);
        Assert.NotNull(result.Document.Settings);
    }

    [Fact]
    public void Workout_example_in_the_data_spec_is_read_cleanly_by_the_real_reader()
    {
        var example = JsonBlock().Matches(File.ReadAllText(SpecPath())).Select(m => m.Groups["json"].Value).Single(j => j.Contains("\"workouts\""));

        var result = ExchangeFormat.Read(example);

        Assert.True(result.IsSuccess, string.Join("; ", result.Errors.Select(e => $"{e.Path}: {e.Message}")));
        Assert.Empty(result.Warnings);
        var workouts = result.Document!.Workouts!;
        Assert.Equal(2, workouts.Count);
        Assert.Equal(3, workouts[0].Entries.Count);
        Assert.True(workouts[0].Entries[0].Sets[1].IsMax);
        Assert.Equal(Core.Model.PainStatus.Reported, workouts[0].PainCheck!.Status);
        Assert.True(workouts[1].LoggedAfterwards);
    }

    [Fact]
    public void Worked_example_covers_weigh_ins_breaks_badges_and_guides()
    {
        var example = JsonBlock().Matches(File.ReadAllText(SpecPath())).Select(m => m.Groups["json"].Value).First(j => j.Contains("\"exportedAt\""));

        var document = ExchangeFormat.Read(example).Document!;

        Assert.Single(document.WeighIns!);
        Assert.Single(document.Breaks!);
        Assert.Single(document.Badges!);
        Assert.Equal("goblet-squat", Assert.Single(document.Guides!).ExerciseId);
    }
}
