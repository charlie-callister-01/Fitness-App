using Core.Model;
using Core.Serialisation;

namespace Core.Tests.Serialisation;

public class V1ToV2UpgraderTests
{
    [Fact]
    public void Upgrades_a_v1_plan_with_phases_and_sessions_into_one_main_track()
    {
        var phases = new List<Phase>
        {
            new("foundation", "Foundation", 1, 8, [new PhaseGoal("squat", 40, 8)], []),
            new("build", "Build", 9, 16, [], [new SessionOverride("s-1", [new SessionExercise("squat", 4, 6, null, "Heavy")])]),
        };
        var sessions = new List<V1Session>
        {
            new("s-1", "A: Lower", DefaultDay.Monday, SessionType.Strength, null, [new SessionExercise("squat", 3, 8, 12, "Chest tall")]),
            new("s-2", "B: Run", DefaultDay.Wednesday, SessionType.Cardio, null, []),
            new("s-3", "Test", DefaultDay.Saturday, SessionType.Test, 6, []),
        };
        var v1 = new V1Plan(
            "hyrox", "HYROX", true, new DateOnly(2026, 10, 19), "Race", new DateOnly(2027, 4, 24),
            5, 4, true, 2.0, phases, sessions);

        var upgraded = V1ToV2Upgrader.UpgradePlan(v1);

        Assert.Equal(("hyrox", "HYROX", true, new DateOnly(2026, 10, 19), "Race", new DateOnly(2027, 4, 24), 2.0),
            (upgraded.Id, upgraded.Name, upgraded.IsActive, upgraded.StartDate, upgraded.Goal, upgraded.GoalDate, upgraded.WeightIncrementKg));

        var track = Assert.Single(upgraded.Tracks);
        Assert.Equal("hyrox-main", track.Id);
        Assert.Equal("Main", track.Name);
        var weekly = Assert.IsType<TrackRhythm.Weekly>(track.Rhythm);
        Assert.Equal((5, 4, true), (weekly.WeeklyTarget, weekly.StreakMinWorkoutsPerWeek, weekly.StreakCountsCustomWorkouts));
        Assert.Equal(phases, track.Phases);

        Assert.Equal(["s-1", "s-2", "s-3"], upgraded.Sessions.Select(s => s.Id));
        Assert.All(upgraded.Sessions, s => Assert.Equal("hyrox-main", s.TrackId));
        Assert.Equal(DefaultDay.Wednesday, upgraded.Sessions[1].DefaultDay);
        Assert.Equal(6, upgraded.Sessions[2].BenchmarkIntervalWeeks);
    }

    [Fact]
    public void Two_v1_plans_get_distinct_track_ids()
    {
        var first = V1ToV2Upgrader.UpgradePlan(new V1Plan("alpha", "Alpha", true, new DateOnly(2026, 1, 5), null, null, 3, 2, false, 2.5, [], []));
        var second = V1ToV2Upgrader.UpgradePlan(new V1Plan("beta", "Beta", false, new DateOnly(2026, 1, 5), null, null, 3, 2, false, 2.5, [], []));

        Assert.Equal("alpha-main", first.Tracks[0].Id);
        Assert.Equal("beta-main", second.Tracks[0].Id);
        Assert.NotEqual(first.Tracks[0].Id, second.Tracks[0].Id);
    }

    [Fact]
    public void V1_reader_hands_plans_through_the_upgrader()
    {
        const string json = """
        {
          "formatVersion": 1,
          "exercises": [{ "id": "squat", "name": "Squat", "logKind": "load" }],
          "plans": [
            {
              "id": "p1",
              "name": "Plan One",
              "isActive": true,
              "startDate": "2026-10-19",
              "weeklyTarget": 4,
              "streakMinWorkoutsPerWeek": 3,
              "streakCountsCustomWorkouts": true,
              "weightIncrementKg": 2.5,
              "phases": [{ "id": "ph1", "focus": "Base", "startWeek": 1, "endWeek": 4 }],
              "sessions": [
                { "id": "s1", "name": "Lower", "defaultDay": "monday", "type": "strength", "exercises": [{ "exerciseId": "squat", "sets": 3, "reps": 8 }] }
              ]
            }
          ]
        }
        """;

        var result = ExchangeFormat.Read(json);

        Assert.True(result.IsSuccess, string.Join("; ", result.Errors.Select(e => e.Message)));
        var plan = Assert.Single(result.Document!.Plans!);
        var track = Assert.Single(plan.Tracks);
        Assert.Equal(("p1-main", "Main"), (track.Id, track.Name));
        Assert.Equal(new TrackRhythm.Weekly(4, 3, true), track.Rhythm);
        Assert.Single(track.Phases);
        var session = Assert.Single(plan.Sessions);
        Assert.Equal("p1-main", session.TrackId);
    }

    [Fact]
    public void V1_backup_with_plans_workouts_and_schedules_reads_and_upgrades_cleanly()
    {
        const string v1Backup = """
        {
          "formatVersion": 1,
          "exportedAt": "2026-11-16T09:30:00+00:00",
          "exportedFrom": "fitness-app 0.1",
          "exercises": [
            { "id": "goblet-squat", "name": "Goblet squat", "logKind": "load", "isBenchmark": true }
          ],
          "settings": {
            "displayWeightUnit": "kg",
            "displayDistanceUnit": "km",
            "weightIncrementKg": 2.5,
            "restTimerSeconds": 90,
            "streakMinWorkoutsPerWeek": 3,
            "streakCountsCustomWorkouts": false
          },
          "weighIns": [{ "id": "wi-1", "date": "2026-11-16", "weightKg": 84.35 }],
          "breaks": [{ "id": "xmas", "startDate": "2026-12-20", "endDate": "2027-01-02", "reason": "Holiday" }],
          "badges": [{ "id": "b1", "name": "Streak", "metric": "streak", "target": 4 }],
          "guides": [{ "exerciseId": "goblet-squat", "works": "Quads" }],
          "plans": [
            {
              "id": "hyrox-2027",
              "name": "HYROX London",
              "isActive": true,
              "startDate": "2026-10-19",
              "goal": "HYROX London Doubles",
              "goalDate": "2027-04-24",
              "weeklyTarget": 5,
              "streakMinWorkoutsPerWeek": 4,
              "streakCountsCustomWorkouts": false,
              "weightIncrementKg": 2,
              "phases": [
                { "id": "foundation", "focus": "Foundation", "startWeek": 1, "endWeek": 8, "goals": [{ "exerciseId": "goblet-squat", "weightKg": 24, "reps": 10 }] }
              ],
              "sessions": [
                { "id": "a-lower", "name": "A: Lower", "defaultDay": "monday", "type": "strength", "exercises": [{ "exerciseId": "goblet-squat", "sets": 3, "reps": 8 }] }
              ]
            }
          ],
          "workouts": [
            {
              "id": "w-1",
              "date": "2026-11-16",
              "sessionId": "a-lower",
              "entries": [{ "exerciseId": "goblet-squat", "status": "done", "sets": [{ "id": "s-1", "weightKg": 24, "reps": 8 }] }]
            }
          ],
          "weekSchedules": [
            { "weekStart": "2026-11-16", "moves": [{ "sessionId": "a-lower", "date": "2026-11-18" }] }
          ]
        }
        """;

        var result = ExchangeFormat.Read(v1Backup);

        Assert.True(result.IsSuccess, string.Join("; ", result.Errors.Select(e => $"{e.Path}: {e.Message}")));
        Assert.Empty(result.Warnings);
        var doc = result.Document!;
        Assert.Equal(2, doc.FormatVersion);
        var plan = Assert.Single(doc.Plans!);
        var main = Assert.Single(plan.Tracks);
        Assert.Equal(("hyrox-2027-main", "Main"), (main.Id, main.Name));
        Assert.Equal(new TrackRhythm.Weekly(5, 4, false), main.Rhythm);
        Assert.Single(main.Phases);
        Assert.Equal("hyrox-2027-main", Assert.Single(plan.Sessions).TrackId);
        Assert.Single(doc.Workouts!);
        Assert.Single(doc.WeekSchedules!);
    }
}
