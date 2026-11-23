using Core.Model;
using Core.Serialisation;

namespace Web.BrowserTests.App;

/// <summary>The committed fixture Backup is a valid current-format document with everything the whole-app tests need to see.</summary>
public sealed class FixtureBackupTests
{
    [Fact]
    public void The_fixture_is_a_valid_current_format_Backup_without_warnings()
    {
        var result = ExchangeFormat.Read(FixtureBackup.Json);

        Assert.True(result.IsSuccess, string.Join("\n", result.Errors.Select(e => $"{e.Path}: {e.Message}")));
        Assert.Empty(result.Warnings);
        Assert.Equal(ExchangeFormat.CurrentVersion, result.Document!.FormatVersion);
    }

    [Fact]
    public void The_fixture_has_one_active_Plan_with_two_Tracks_and_a_week_of_Sessions_Workouts_Weigh_ins_a_Badge_and_a_Guide()
    {
        var document = ExchangeFormat.Read(FixtureBackup.Json).Document!;

        var plan = Assert.Single(document.Plans!);
        Assert.True(plan.IsActive);
        Assert.Equal(2, plan.Tracks.Count);
        Assert.True(plan.Sessions.Count >= 3);
        Assert.True(document.Workouts!.Count >= 3);
        Assert.NotEmpty(document.WeighIns!);
        Assert.NotEmpty(document.Badges!);
        Assert.NotEmpty(document.Guides!);
    }

    [Fact]
    public void The_fixture_is_a_complete_Backup_so_Restore_accepts_it()
    {
        var document = ExchangeFormat.Read(FixtureBackup.Json).Document!;

        // Restore refuses a file that lacks any collection or Settings; an empty collection still counts as present.
        Assert.NotNull(document.Settings);
        Assert.NotNull(document.Exercises);
        Assert.NotNull(document.Breaks);
        Assert.NotNull(document.WeekSchedules);
    }
}
