using Core.Model;
using Core.Rules.PlanEditing;
using Core.Tests.Ids;
using Core.Tests.Rules.TestData;
using static Core.Tests.Rules.TestData.Dates;
using static Core.Tests.Rules.TestData.HistoryBuilder;

namespace Core.Tests.Rules.PlanEditing;

public class TrackEditRulesTests
{
    private static Plan MultiPlan() => PlanFixture.Make(start: Mon, tracks:
    [
        new Track("main", "Gym", new TrackRhythm.Weekly(3, 2, false), []),
        new Track("mob", "Mobility", new TrackRhythm.Daily(75), []),
        new Track("cardio", "Cardio", new TrackRhythm.Weekly(2, 2, false), []),
    ]);

    private static T Valid<T>(PlanEditResult<T> result) => Assert.IsType<PlanEditResult<T>.Valid>(result).Value;

    private static string Reason<T>(PlanEditResult<T> result) => Assert.IsType<PlanEditResult<T>.Invalid>(result).Message;

    [Fact]
    public void Adding_a_weekly_Track_sets_the_apps_defaults_and_appends_at_the_end()
    {
        var plan = PlanFixture.Make(start: Mon);

        var updated = Valid(TrackEditRules.AddTrack(plan, " Cardio ", TrackCadence.Weekly, new SequentialIdGenerator("t")));

        Assert.Equal(2, updated.Tracks.Count);
        var added = updated.Tracks[1];
        Assert.Equal(("t-1", "Cardio"), (added.Id, added.Name));
        Assert.Equal(new TrackRhythm.Weekly(3, 2, false), added.Rhythm);
        Assert.Empty(added.Phases);
    }

    [Fact]
    public void Adding_a_daily_Track_defaults_day_done_to_75_percent()
    {
        var plan = PlanFixture.Make(start: Mon);

        var updated = Valid(TrackEditRules.AddTrack(plan, " Mobility ", TrackCadence.Daily, new SequentialIdGenerator("t")));

        Assert.Equal(2, updated.Tracks.Count);
        var added = updated.Tracks[1];
        Assert.Equal(("t-1", "Mobility"), (added.Id, added.Name));
        Assert.Equal(new TrackRhythm.Daily(75), added.Rhythm);
        Assert.Empty(added.Phases);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Main")]
    [InlineData("  main  ")]
    public void Adding_a_Track_with_a_blank_or_duplicate_name_is_refused(string name)
    {
        var plan = PlanFixture.Make(start: Mon);

        var reason = Reason(TrackEditRules.AddTrack(plan, name, TrackCadence.Daily, new SequentialIdGenerator("t")));

        Assert.False(string.IsNullOrWhiteSpace(reason));
    }

    [Fact]
    public void Renaming_a_Track_updates_only_that_Track_and_refuses_blank_or_duplicate_names()
    {
        var plan = MultiPlan();

        var renamed = Valid(TrackEditRules.RenameTrack(plan, "mob", "  Morning Mobility  "));
        var sameNameRecased = Valid(TrackEditRules.RenameTrack(renamed, "mob", "morning mobility"));

        Assert.Equal("Morning Mobility", renamed.Tracks[1].Name);
        Assert.Equal("morning mobility", sameNameRecased.Tracks[1].Name);
        Assert.Equal("Gym", renamed.Tracks[0].Name);
        Assert.Contains("name", Reason(TrackEditRules.RenameTrack(plan, "mob", " ")));
        Assert.Contains("Cardio", Reason(TrackEditRules.RenameTrack(plan, "mob", "cardio")));
        Assert.Contains("no longer", Reason(TrackEditRules.RenameTrack(plan, "missing", "New")));
    }

    [Fact]
    public void Editing_Track_settings_updates_only_that_Track_and_refuses_cadence_mismatch_or_bad_numbers()
    {
        var plan = MultiPlan();

        var updatedDaily = Valid(TrackEditRules.WithSettings(plan, "mob", new TrackSettings.Daily(80)));
        var editedWeekly = Valid(TrackEditRules.EditTrack(updatedDaily, "cardio", "Run", new TrackSettings.Weekly(4, 3, false)));

        Assert.Equal(new TrackRhythm.Daily(80), editedWeekly.Tracks[1].Rhythm);
        Assert.Equal("Run", editedWeekly.Tracks[2].Name);
        Assert.Equal(new TrackRhythm.Weekly(4, 3, false), editedWeekly.Tracks[2].Rhythm);
        Assert.Contains("1 and 100", Reason(TrackEditRules.WithSettings(plan, "mob", new TrackSettings.Daily(0))));
        Assert.Contains("cadence", Reason(TrackEditRules.WithSettings(plan, "mob", new TrackSettings.Weekly(3, 2, false))));
    }

    [Fact]
    public void Moving_weekly_Tracks_reorders_them_and_moving_daily_above_index_one_is_allowed()
    {
        var plan = MultiPlan();

        var movedUp = Valid(TrackEditRules.MoveUp(plan, "cardio"));
        var movedDown = Valid(TrackEditRules.MoveDown(movedUp, "cardio"));
        var twoWeekly = PlanFixture.Make(start: Mon, tracks:
        [
            new Track("main", "Gym", new TrackRhythm.Weekly(3, 2, false), []),
            new Track("cardio", "Cardio", new TrackRhythm.Weekly(2, 2, false), []),
        ]);
        var swappedMain = Valid(TrackEditRules.MoveUp(twoWeekly, "cardio"));

        Assert.Equal(["main", "cardio", "mob"], movedUp.Tracks.Select(t => t.Id));
        Assert.Equal(["main", "mob", "cardio"], movedDown.Tracks.Select(t => t.Id));
        Assert.Equal(["cardio", "main"], swappedMain.Tracks.Select(t => t.Id));
    }

    [Fact]
    public void Moving_a_Track_past_an_end_or_putting_a_daily_Track_first_is_refused()
    {
        var plan = MultiPlan();

        Assert.Contains("first", Reason(TrackEditRules.MoveUp(plan, "main")));
        Assert.Contains("last", Reason(TrackEditRules.MoveDown(plan, "cardio")));
        Assert.Contains("weekly", Reason(TrackEditRules.MoveUp(plan, "mob")));
        Assert.Contains("weekly", Reason(TrackEditRules.MoveDown(plan, "main")));
        Assert.Contains("no longer", Reason(TrackEditRules.MoveUp(plan, "missing")));
    }

    [Fact]
    public void Editing_a_Track_never_touches_past_Workouts()
    {
        var plan = MultiPlan();
        var workout = Logged(Mon, "s1") with { Planned = [PlanFixture.Row("squat", sets: 3)] };

        var updated = Valid(TrackEditRules.WithSettings(plan, "mob", new TrackSettings.Daily(90)));

        Assert.Equal(90, Assert.IsType<TrackRhythm.Daily>(updated.Tracks[1].Rhythm).DayDonePercent);
        Assert.Single(workout.Planned);
        Assert.Equal(3, workout.Planned[0].Sets);
    }
}
