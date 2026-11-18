using Core.Model;
using Core.Serialisation;
using Core.Storage;

namespace Core.Tests.Serialisation;

public class ThemeAndModeSettingsTests
{
    private const string LastField = "\"streakCountsCustomWorkouts\":true";

    private static string WithoutThemeAndMode() => TestData.MinimalJson()
        .Replace(",\"theme\":\"keep\"", "").Replace(",\"mode\":\"system\"", "");

    [Fact]
    public void Defaults_are_Keep_and_System()
    {
        Assert.Equal((Theme.Keep, Mode.System), (Settings.Default.Theme, Settings.Default.Mode));
    }

    [Fact]
    public void Writer_writes_both_fields_and_the_reader_returns_them()
    {
        var doc = TestData.Minimal() with { Settings = TestData.ValidSettings() with { Theme = Theme.Clean, Mode = Mode.Dark } };

        var json = ExchangeFormat.Write(doc);
        var result = ExchangeFormat.Read(json);

        Assert.Contains("\"theme\":\"clean\"", json);
        Assert.Contains("\"mode\":\"dark\"", json);
        Assert.Equal((Theme.Clean, Mode.Dark), (result.Document!.Settings!.Theme, result.Document.Settings.Mode));
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void A_document_without_the_fields_reads_with_defaults_and_round_trips()
    {
        var older = WithoutThemeAndMode();
        Assert.DoesNotContain("\"theme\"", older);

        var result = ExchangeFormat.Read(older);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Warnings);
        Assert.Equal((Theme.Keep, Mode.System), (result.Document!.Settings!.Theme, result.Document.Settings.Mode));
        Assert.Equal(TestData.ValidSettings(), result.Document.Settings);
        Assert.Equal(TestData.MinimalJson(), ExchangeFormat.Write(result.Document));
    }

    [Theory]
    [InlineData(",\"theme\":\"neon\"", "settings.theme")]
    [InlineData(",\"mode\":\"dim\"", "settings.mode")]
    [InlineData(",\"theme\":3", "settings.theme")]
    [InlineData(",\"mode\":true", "settings.mode")]
    public void Unknown_or_malformed_value_falls_back_to_the_default_with_a_warning(string extra, string path)
    {
        var json = WithoutThemeAndMode().Replace(LastField, LastField + extra);

        var result = ExchangeFormat.Read(json);

        Assert.True(result.IsSuccess, string.Join("; ", result.Errors.Select(e => e.Path + " " + e.Message)));
        Assert.Equal((Theme.Keep, Mode.System), (result.Document!.Settings!.Theme, result.Document.Settings.Mode));
        var warning = Assert.Single(result.Warnings);
        Assert.Equal(path, warning.Path);
    }

    [Fact]
    public void One_bad_field_does_not_lose_the_other()
    {
        var json = WithoutThemeAndMode().Replace(LastField, LastField + ",\"theme\":\"clean\",\"mode\":\"dim\"");

        var result = ExchangeFormat.Read(json);

        Assert.Equal((Theme.Clean, Mode.System), (result.Document!.Settings!.Theme, result.Document.Settings.Mode));
    }

    [Fact]
    public async Task A_Settings_commit_changing_only_the_Theme_persists_and_reloads()
    {
        var store = new InMemoryDataStore();
        Assert.IsType<StoreResult<Unit>.Success>(await store.ReplaceAllAsync(StoredRecords.FromDocument(TestData.Minimal()), new StoreMetadata(null, null, null, true)));
        var app = await AppData.OpenAsync(store);
        var settings = TestData.ValidSettings() with { Theme = Theme.Clean, Mode = Mode.Light };

        await app.CommitAsync(Change.Put(settings));

        Assert.Equal(settings, app.Snapshot.Settings);
        Assert.Equal(settings, (await AppData.OpenAsync(store)).Snapshot.Settings);
    }
}
