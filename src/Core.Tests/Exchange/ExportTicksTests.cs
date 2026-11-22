using Core.Exchange;
using Core.Model;
using Core.Storage;

namespace Core.Tests.Exchange;

/// <summary>Each tick includes exactly its data, and every non-Backup Export reads back through the real reader.</summary>
public class ExportTicksTests
{
    private static readonly DateRange NovemberToDecember = new(new DateOnly(2026, 11, 1), new DateOnly(2026, 12, 31));

    private static ExportRequest Everything() => ExportRequest.Default(ExchangeTestData.Today) with { Range = NovemberToDecember, Guides = true };

    private static ExchangeDocument Export(ExportRequest request, DataSnapshot? snapshot = null) =>
        ExchangeTestData.ReadBack(Exporter.Export(snapshot ?? ExchangeTestData.Snapshot(), request, "test", ExchangeTestData.Now).Text);

    [Fact]
    public void A_sliced_Export_carries_the_records_it_includes_unchanged()
    {
        var snapshot = ExchangeTestData.Snapshot();
        var document = Export(Everything() with { PainChecks = true, Guides = true }, snapshot);

        AssertSame(snapshot.Exercises, document.Exercises!);
        AssertSame(snapshot.WeighIns.Where(w => NovemberToDecember.Contains(w.Date)), document.WeighIns!);
        // The Session link is dropped when its Plan is not exported (documented in the spec), so compare without it.
        AssertSame(
            snapshot.Workouts.Where(w => NovemberToDecember.Contains(w.Date)).Select(w => w with { SessionId = null }),
            document.Workouts!.Select(w => w with { SessionId = null }));
        AssertSame(snapshot.Plans.Where(p => p.IsActive), document.Plans!);
        AssertSame(snapshot.Guides.Select(g => g with { PersonalNotes = null }), document.Guides!);
    }

    private static void AssertSame<T>(IEnumerable<T> source, IEnumerable<T> exported)
        where T : class
    {
        var expected = source.ToDictionary(RecordKeys.For);
        Assert.NotEmpty(exported);
        Assert.All(exported, record => Assert.True(StoredJson.Same(record, expected[RecordKeys.For(record)]), RecordKeys.For(record)));
    }

    [Fact]
    public void Unticked_kinds_are_absent_from_the_document()
    {
        var document = Export(Everything() with { Workouts = false, Plan = false, WeighIns = false, PainChecks = false, Guides = false });

        Assert.Null(document.Workouts);
        Assert.Null(document.Plans);
        Assert.Null(document.Breaks);
        Assert.Null(document.WeekSchedules);
        Assert.Null(document.WeighIns);
        Assert.Null(document.Guides);
    }

    [Fact]
    public void Workouts_keep_their_planned_snapshot_Sets_skips_and_swaps()
    {
        var document = Export(Everything());

        var w1 = document.Workouts!.Single(w => w.Id == "w-1");
        Assert.Equal(3, w1.Planned.Count);
        Assert.Equal([EntryStatus.Done, EntryStatus.Skipped, EntryStatus.Swapped], w1.Entries.Select(e => e.Status));
        Assert.Equal("rdl", w1.Entries[2].SwappedFromExerciseId);
        Assert.Equal(["s-1", "s-2"], w1.Entries[0].Sets.Select(s => s.Id));
    }

    [Fact]
    public void Unticking_Pain_checks_removes_them_from_every_Workout()
    {
        var document = Export(Everything() with { PainChecks = false });

        Assert.NotEmpty(document.Workouts!);
        Assert.All(document.Workouts!, w => Assert.Null(w.PainCheck));
    }

    [Fact]
    public void Pain_checks_alone_give_Workouts_reduced_to_date_Session_and_Pain_check()
    {
        var document = Export(Everything() with { Workouts = false, Plan = false });

        Assert.Equal(["w-1", "w-2", "w-status-0", "w-status-1", "w-status-2"], document.Workouts!.Select(w => w.Id).Order());
        Assert.All(document.Workouts!, w =>
        {
            Assert.Empty(w.Entries);
            Assert.Empty(w.Planned);
            Assert.NotNull(w.PainCheck);
        });
        Assert.Equal("a-lower", document.Workouts!.Single(w => w.Id == "w-1").SessionId);
        Assert.Equal(new DateOnly(2026, 11, 16), document.Workouts!.Single(w => w.Id == "w-1").Date);
    }

    [Fact]
    public void Plan_means_the_active_Plan_whole_whatever_the_range()
    {
        var document = Export(Everything() with { Range = new DateRange(new DateOnly(2030, 1, 1), new DateOnly(2030, 1, 2)) });

        var plan = Assert.Single(document.Plans!);
        Assert.Equal("hyrox-2027", plan.Id);
        Assert.Equal(ExchangeTestData.Snapshot().Plans[0].Sessions.Select(s => s.Id), plan.Sessions.Select(s => s.Id));
        Assert.Equal(3, plan.Tracks[0].Phases.Count);
    }

    [Fact]
    public void Breaks_and_Week_schedules_are_included_only_when_they_overlap_the_range()
    {
        var inDecember = Export(Everything() with { Range = new DateRange(new DateOnly(2026, 12, 25), new DateOnly(2027, 1, 10)) });
        var inNovember = Export(Everything() with { Range = new DateRange(new DateOnly(2026, 11, 22), new DateOnly(2026, 11, 22)) });
        var overlapsWeekByItsSunday = Export(Everything() with { Range = new DateRange(new DateOnly(2026, 11, 22), new DateOnly(2026, 11, 30)) });

        Assert.Equal(["xmas"], inDecember.Breaks!.Select(b => b.Id));
        Assert.Empty(inNovember.Breaks!);
        Assert.Empty(inDecember.WeekSchedules!);
        Assert.Equal([new DateOnly(2026, 11, 16)], overlapsWeekByItsSunday.WeekSchedules!.Select(w => w.WeekStart));
    }

    [Fact]
    public void Weigh_ins_are_included_only_in_range()
    {
        var document = Export(Everything() with { Range = new DateRange(new DateOnly(2026, 11, 17), new DateOnly(2026, 11, 17)) });

        Assert.Equal(["wi-2"], document.WeighIns!.Select(w => w.Id));
    }

    [Fact]
    public void Guides_leave_out_personal_notes_but_keep_the_rest()
    {
        var document = Export(Everything());

        Assert.Equal(["goblet-squat", "rdl"], document.Guides!.Select(g => g.ExerciseId).Order());
        Assert.All(document.Guides!, g => Assert.Null(g.PersonalNotes));
        Assert.Equal(["Chest up", "Knees out"], document.Guides!.Single(g => g.ExerciseId == "goblet-squat").Cues);
    }

    [Fact]
    public void Guides_come_only_for_the_Exercises_that_are_exported()
    {
        var onlyOldWorkout = Everything() with { Range = new DateRange(new DateOnly(2025, 1, 6), new DateOnly(2025, 1, 6)), Plan = false, Exercises = false };

        var document = Export(onlyOldWorkout);

        Assert.Equal(["rdl"], document.Guides!.Select(g => g.ExerciseId));
    }

    [Fact]
    public void Ticking_Exercises_exports_the_whole_library()
    {
        var snapshot = ExchangeTestData.Snapshot();

        var document = Export(Everything(), snapshot);

        Assert.Equal(snapshot.Exercises.Select(e => e.Id), document.Exercises!.Select(e => e.Id));
    }

    [Fact]
    public void Unticking_Exercises_still_carries_every_Exercise_the_exported_data_names()
    {
        var document = Export(Everything() with { Exercises = false });

        var ids = document.Exercises!.Select(e => e.Id).ToHashSet();
        Assert.Contains("goblet-squat", ids);
        Assert.Contains("rdl", ids);
        Assert.Contains("5k-run", ids);
        Assert.Contains("ex-load", ids);
        Assert.DoesNotContain("ex-text", ids);
        Assert.All(document.Exercises!, e => Assert.False(string.IsNullOrEmpty(e.Name)));
    }

    [Fact]
    public void Exercises_named_only_by_a_Pain_check_are_carried()
    {
        var document = Export(Everything() with { Workouts = false, Plan = false, Exercises = false, Range = new DateRange(new DateOnly(2026, 11, 20), new DateOnly(2026, 11, 20)) });

        Assert.Equal(["ex-load"], document.Exercises!.Select(e => e.Id));
    }

    [Fact]
    public void A_Workout_of_a_Plan_that_is_not_exported_loses_its_Session_link_so_the_file_stays_valid()
    {
        var snapshot = ExchangeTestData.Snapshot();
        var old = snapshot.Plans.Single(p => !p.IsActive);
        var oldPlan = old with { Sessions = [new Session("old-s", "Old", old.Tracks[0].Id, DefaultDay.Monday, SessionType.Cardio, null, [])] };
        var plans = snapshot.Plans.Select(p => p.IsActive ? p : oldPlan).ToList();
        var workouts = snapshot.Workouts.Select(w => w.Id == "w-3" ? w with { SessionId = "old-s", Planned = [] } : w).ToList();

        var document = Export(Everything(), snapshot with { Plans = plans, Workouts = workouts });

        Assert.Null(document.Workouts!.Single(w => w.Id == "w-3").SessionId);
        Assert.Equal("a-lower", document.Workouts!.Single(w => w.Id == "w-1").SessionId);
    }

    [Fact]
    public void A_non_Backup_Export_leaves_out_Settings_Badges_and_inactive_Plans()
    {
        var document = Export(Everything());

        Assert.Null(document.Settings);
        Assert.Null(document.Badges);
        Assert.DoesNotContain(document.Plans!, p => p.Id == "old-plan");
    }

    [Fact]
    public void An_empty_snapshot_still_exports_a_valid_document()
    {
        var document = Export(Everything(), DataSnapshot.Empty);

        Assert.Empty(document.Workouts!);
        Assert.Empty(document.Plans!);
        Assert.Empty(document.Exercises!);
    }

    [Fact]
    public void Export_JSON_carries_tracks_trackId_daily_and_mobility_and_keeps_sessionId_when_plan_is_unticked()
    {
        var baseSnapshot = ExchangeTestData.Snapshot();
        var active = baseSnapshot.Plans.Single(p => p.IsActive);
        var twoTrack = active with
        {
            Tracks =
            [
                active.Tracks[0],
                new Track("mobility", "Morning mobility", new TrackRhythm.Daily(75), [new Phase("mob-habit", "Habit", 1, 4, [], [])]),
            ],
            Sessions =
            [
                .. active.Sessions,
                new Session("m-mobility", "Morning mobility", "mobility", DefaultDay.Daily, SessionType.Mobility, null, [new SessionExercise("ex-tickonly", 1, null, null, null)]),
            ],
        };
        var mobWorkout = new Workout(
            "w-mob", new DateOnly(2026, 11, 16), "m-mobility", false, null, null,
            [new SessionExercise("ex-tickonly", 1, null, null, null)],
            [new WorkoutEntry("ex-tickonly", EntryStatus.Done, null, null, null, [])],
            null);
        var snapshot = baseSnapshot with
        {
            Plans = [.. baseSnapshot.Plans.Select(p => p.IsActive ? twoTrack : p)],
            Workouts = [.. baseSnapshot.Workouts, mobWorkout],
        };

        var withPlanJson = Exporter.Export(snapshot, Everything(), "test", ExchangeTestData.Now).Text;
        Assert.Contains("\"tracks\"", withPlanJson);
        Assert.Contains("\"trackId\": \"mobility\"", withPlanJson);
        Assert.Contains("\"defaultDay\": \"daily\"", withPlanJson);
        Assert.Contains("\"type\": \"mobility\"", withPlanJson);
        var withPlanDoc = ExchangeTestData.ReadBack(withPlanJson);
        Assert.Equal("m-mobility", withPlanDoc.Workouts!.Single(w => w.Id == "w-mob").SessionId);

        var withoutPlan = Export(Everything() with { Plan = false }, snapshot);
        Assert.Null(withoutPlan.Plans);
        Assert.Equal("m-mobility", withoutPlan.Workouts!.Single(w => w.Id == "w-mob").SessionId);

        var withoutExercises = Export(Everything() with { Exercises = false }, snapshot);
        Assert.Contains(withoutExercises.Exercises!, e => e.Id == "ex-tickonly");
    }
}
