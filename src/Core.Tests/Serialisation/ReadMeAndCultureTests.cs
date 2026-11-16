using System.Globalization;
using System.Text.Json.Nodes;
using Core.Model;
using Core.Serialisation;

namespace Core.Tests.Serialisation;

public class ReadMeAndCultureTests
{
    [Fact]
    public void A_written_document_carries_a_readMe_block_after_the_version()
    {
        var json = ExchangeFormat.Write(WorkoutTestData.Rich());

        var root = JsonNode.Parse(json)!.AsObject();
        var names = root.Select(p => p.Key).ToList();

        Assert.Equal("formatVersion", names[0]);
        Assert.Equal("readMe", names[1]);
        var readMe = root["readMe"]!.AsObject();
        Assert.Contains("formatVersion", readMe.Select(p => p.Key));
        Assert.Contains("units", readMe.Select(p => p.Key));
        Assert.Contains("ids", readMe.Select(p => p.Key));
    }

    [Fact]
    public void The_readMe_block_is_read_back_with_no_warning_and_no_error()
    {
        var result = ExchangeFormat.Read(ExchangeFormat.Write(WorkoutTestData.Rich()));

        Assert.True(result.IsSuccess, string.Join("; ", result.Errors.Select(e => $"{e.Path}: {e.Message}")));
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void A_hand_written_readMe_of_any_shape_is_ignored_silently()
    {
        var result = ExchangeFormat.Read("""{ "formatVersion": 1, "readMe": "just a note" }""");

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Decimals_round_trip_under_a_comma_decimal_culture()
    {
        var previous = CultureInfo.CurrentCulture;
        var previousUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
            var original = WorkoutTestData.Rich() with
            {
                Settings = TestData.ValidSettings() with { WeightIncrementKg = 1.25, GoalWeightKg = 79.375 },
                WeighIns = [new WeighIn("wi-1", new DateOnly(2026, 11, 16), 84.35)],
            };

            var json = ExchangeFormat.Write(original);
            var result = ExchangeFormat.Read(json);

            Assert.Contains("84.35", json);
            Assert.DoesNotContain("84,35", json);
            Assert.True(result.IsSuccess, string.Join("; ", result.Errors.Select(e => $"{e.Path}: {e.Message}")));
            Assert.Equal(84.35, result.Document!.WeighIns![0].WeightKg);
            Assert.Equal(79.375, result.Document.Settings!.GoalWeightKg);
            Assert.Equal(1.25, result.Document.Settings.WeightIncrementKg);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }
}
