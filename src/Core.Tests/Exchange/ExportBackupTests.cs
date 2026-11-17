using System.Text.Json.Nodes;
using Core.Exchange;
using Core.Model;
using Core.Serialisation;
using Core.Storage;

namespace Core.Tests.Exchange;

public class ExportBackupTests
{
    private static readonly string[] EveryCollection =
        ["exercises", "weighIns", "breaks", "badges", "guides", "plans", "workouts", "weekSchedules"];

    private static ExportResult Backup(DataSnapshot snapshot) =>
        Exporter.Export(snapshot, ExportRequest.Backup, "fitness-app 1.2", ExchangeTestData.Now);

    [Fact]
    public void A_Backup_holds_every_collection_and_Settings_even_when_the_dataset_is_empty()
    {
        var json = JsonNode.Parse(Backup(DataSnapshot.Empty).Text)!.AsObject();

        foreach (var name in EveryCollection)
        {
            var collection = Assert.IsType<JsonArray>(json[name]);
            Assert.Empty(collection);
        }

        Assert.NotNull(json["settings"]);
    }

    [Fact]
    public void A_Backup_reads_back_with_everything_including_personal_notes_and_inactive_Plans()
    {
        var snapshot = ExchangeTestData.Snapshot();

        var document = ExchangeTestData.ReadBack(Backup(snapshot).Text);

        Assert.Equal(snapshot.Plans.Select(p => p.Id), document.Plans!.Select(p => p.Id));
        Assert.Equal(snapshot.Workouts.Select(w => w.Id), document.Workouts!.Select(w => w.Id));
        Assert.Equal(snapshot.Breaks.Count, document.Breaks!.Count);
        Assert.Equal(snapshot.Badges.Count, document.Badges!.Count);
        Assert.Equal(snapshot.WeekSchedules.Count, document.WeekSchedules!.Count);
        Assert.Equal(snapshot.Settings, document.Settings);
        Assert.Contains(document.Guides!, g => g.PersonalNotes == "Notes: 膝");
        Assert.Contains(document.Workouts!, w => w.PainCheck is not null);
    }

    [Fact]
    public void A_Backup_restores_the_exact_dataset()
    {
        var snapshot = ExchangeTestData.Snapshot();

        var document = ExchangeTestData.ReadBack(Backup(snapshot).Text);

        Assert.Equal(
            ExchangeFormat.Write(snapshot.ToDocument()),
            ExchangeFormat.Write(document with { ExportedAt = null, ExportedFrom = null }));
    }

    [Fact]
    public void A_Backup_counts_as_a_Backup_and_a_slice_does_not()
    {
        var snapshot = ExchangeTestData.Snapshot();

        Assert.True(Backup(snapshot).CountsAsBackup);
        Assert.False(Exporter.Export(snapshot, ExportRequest.Default(ExchangeTestData.Today), "t", ExchangeTestData.Now).CountsAsBackup);
    }

    [Fact]
    public void Only_all_time_with_every_kind_ticked_as_JSON_is_a_Backup()
    {
        Assert.True(ExportRequest.Backup.IsBackup);
        Assert.False((ExportRequest.Backup with { Guides = false }).IsBackup);
        Assert.False((ExportRequest.Backup with { Range = new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1)) }).IsBackup);
        Assert.False((ExportRequest.Backup with { Format = ExportFormat.Markdown }).IsBackup);
        Assert.False(ExportRequest.Default(ExchangeTestData.Today).IsBackup);
    }

    [Fact]
    public void The_JSON_starts_with_the_readMe_and_carries_exportedAt_and_exportedFrom()
    {
        var json = JsonNode.Parse(Backup(DataSnapshot.Empty).Text)!.AsObject();

        Assert.Equal(["formatVersion", "readMe"], json.Select(p => p.Key).Take(2));
        Assert.Equal("fitness-app 1.2", (string?)json["exportedFrom"]);
        Assert.Equal(ExchangeTestData.Now, (DateTimeOffset)json["exportedAt"]!);
    }
}
