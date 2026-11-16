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
    public void Worked_example_in_the_data_spec_is_read_cleanly_by_the_real_reader()
    {
        var example = JsonBlock().Match(File.ReadAllText(SpecPath())).Groups["json"].Value;
        Assert.NotEmpty(example);

        var result = ExchangeFormat.Read(example);

        Assert.True(result.IsSuccess, string.Join("; ", result.Errors.Select(e => $"{e.Path}: {e.Message}")));
        Assert.Empty(result.Warnings);
        Assert.Equal(3, result.Document!.Exercises!.Count);
        Assert.NotNull(result.Document.Settings);
    }
}
