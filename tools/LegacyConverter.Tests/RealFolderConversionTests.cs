using Core.Exchange;
using Core.Serialisation;
using Core.Storage;

namespace LegacyConverter.Tests;

public sealed class RealFolderConversionTests
{
    [Fact]
    public async Task RealFolder_ConvertsAndRestoresWhenEnvVarSet()
    {
        var fitnessFolder = Environment.GetEnvironmentVariable("LEGACY_FITNESS_FOLDER");
        if (string.IsNullOrWhiteSpace(fitnessFolder))
        {
            return;
        }

        var folder = LegacyFolderLoader.Load(fitnessFolder);
        var result = LegacyConverter.Convert(folder);
        var doc = result.Document;

        Assert.Equal(50, doc.Exercises!.Count);
        var plan = Assert.Single(doc.Plans!);
        Assert.Equal(2, plan.Tracks.Count);
        Assert.Equal(7, plan.Sessions.Count);
        Assert.Equal(3, plan.Tracks[0].Phases.Count);
        Assert.Equal(25, doc.Badges!.Count);
        Assert.Equal(5, doc.Workouts!.Count);
        Assert.True(doc.WeighIns!.Count >= 2);
        Assert.NotEmpty(doc.Guides!);

        var json = ExchangeFormat.Write(doc, indented: true);
        var inspect = ImportInput.Inspect(json);
        Assert.True(inspect.IsAccepted, string.Join("; ", inspect.Rejections));

        var appData = await AppData.OpenAsync(new InMemoryDataStore());
        var preview = ImportPreviewer.Preview(
            appData,
            inspect.Inspected!,
            ImportKind.Restore,
            ImportDecisions.None,
            new DateTimeOffset(2026, 11, 22, 12, 0, 0, TimeSpan.Zero));
        Assert.True(preview.IsReady);
    }
}
