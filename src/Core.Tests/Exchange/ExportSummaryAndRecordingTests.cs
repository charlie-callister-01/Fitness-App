using Core.Exchange;
using Core.Storage;
using Core.Tests.Storage;

namespace Core.Tests.Exchange;

public class ExportSummaryAndRecordingTests
{
    private static ExportResult Export(ExportRequest request, DateTimeOffset? now = null) =>
        Exporter.Export(ExchangeTestData.Snapshot(), request, "test", now ?? ExchangeTestData.Now);

    [Fact]
    public void The_default_Export_is_summarised_as_counts_and_the_date_range()
    {
        var snapshot = ExchangeTestData.Snapshot();

        var result = Export(ExportRequest.Default(ExchangeTestData.Today));

        Assert.Equal(
            [$"3 Workouts, 1 Plan, 2 Weigh-ins, 2 Pain checks, {snapshot.Exercises.Count} Exercises", "26 Aug to 17 Nov 2026"],
            result.Summary);
    }

    [Fact]
    public void Singular_counts_read_naturally()
    {
        var range = new DateRange(new DateOnly(2026, 11, 17), new DateOnly(2026, 11, 17));

        var result = Export(ExportRequest.Default(ExchangeTestData.Today) with { Range = range, Plan = false, Exercises = false });

        Assert.StartsWith("1 Workout, 1 Weigh-in, 0 Pain checks", result.Summary[0]);
    }

    [Fact]
    public void A_range_across_years_spells_out_both_years()
    {
        var range = new DateRange(new DateOnly(2025, 12, 30), new DateOnly(2026, 1, 2));

        var result = Export(ExportRequest.Default(ExchangeTestData.Today) with { Range = range });

        Assert.Contains("30 Dec 2025 to 2 Jan 2026", result.Summary);
    }

    [Fact]
    public void Unticking_Exercises_says_only_the_Exercises_used()
    {
        var result = Export(ExportRequest.Default(ExchangeTestData.Today) with { Exercises = false });

        Assert.Contains("Exercises: only the Exercises used", result.Summary);
    }

    [Fact]
    public void A_Backup_summary_says_it_is_everything_and_all_time()
    {
        var result = Export(ExportRequest.Backup);

        Assert.Equal("Backup: every kind of data, all time", result.Summary[0]);
        Assert.Contains("All time", result.Summary);
        Assert.Contains("2 Plans", result.Summary[1]);
    }

    [Theory]
    [InlineData(true, "fitness-backup-2026-11-17.json")]
    [InlineData(false, "fitness-export-2026-08-26-to-2026-11-17.json")]
    public void The_suggested_file_name_says_what_it_is_and_when(bool backup, string expected)
    {
        var request = backup ? ExportRequest.Backup : ExportRequest.Default(ExchangeTestData.Today);

        Assert.Equal(expected, Export(request).FileName);
    }

    [Fact]
    public void An_all_time_slice_is_named_all_time_with_the_export_date()
    {
        var request = ExportRequest.Backup with { Guides = false };

        Assert.Equal("fitness-export-all-time-2026-11-17.json", Export(request).FileName);
    }

    [Fact]
    public void The_file_name_date_is_the_date_in_the_time_given()
    {
        var lateEvening = new DateTimeOffset(2026, 11, 17, 23, 30, 0, TimeSpan.FromHours(-5));

        Assert.Equal("fitness-backup-2026-11-17.json", Export(ExportRequest.Backup, lateEvening).FileName);
    }

    [Fact]
    public async Task Recording_a_Backup_stamps_the_Backup_date()
    {
        var app = await AppData.OpenAsync(new InMemoryDataStore());

        var outcome = await BackupRecorder.RecordAsync(app, Export(ExportRequest.Backup), ExchangeTestData.Now);

        Assert.IsType<StoreResult<Unit>.Success>(outcome);
        Assert.Equal(ExchangeTestData.Now, app.LastBackupAt);
    }

    [Fact]
    public async Task Making_an_Export_does_not_stamp_anything_until_the_host_records_it()
    {
        var app = await AppData.OpenAsync(new InMemoryDataStore());

        Export(ExportRequest.Backup);

        Assert.Null(app.LastBackupAt);
    }

    [Fact]
    public async Task Recording_a_non_Backup_Export_changes_nothing()
    {
        var app = await AppData.OpenAsync(new InMemoryDataStore());
        var changes = 0;
        app.Changed += () => changes++;

        var outcome = await BackupRecorder.RecordAsync(app, Export(ExportRequest.Default(ExchangeTestData.Today)), ExchangeTestData.Now);

        Assert.IsType<StoreResult<Unit>.Success>(outcome);
        Assert.Null(app.LastBackupAt);
        Assert.Equal(0, changes);
    }

    [Fact]
    public async Task A_failed_stamp_is_reported_and_nothing_is_remembered()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        var app = await AppData.OpenAsync(store);
        store.FailAllWrites(StoreFailureKind.StorageFull);

        var outcome = await BackupRecorder.RecordAsync(app, Export(ExportRequest.Backup), ExchangeTestData.Now);

        Assert.IsType<StoreResult<Unit>.Failure>(outcome);
        Assert.Null(app.LastBackupAt);
    }
}
