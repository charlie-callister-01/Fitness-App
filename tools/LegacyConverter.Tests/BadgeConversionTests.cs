namespace LegacyConverter.Tests;

public sealed class BadgeConversionTests
{
    [Fact]
    public void Convert_MapsBadgesInCanonicalUnitsAndDropsBodyweightDeadlift()
    {
        var config = """
            ## Settings
            - start: 2026-10-19
            - race: 2027-04-24
            - race name: HYROX London (Doubles)
            - sessions per week: 5
            - streak min: 4
            - increment kg: 2
            - mobility done percent: 75

            ## Phases
            | Phase | From week | To week |
            | --- | --- | --- |
            | Foundation | 1 | 8 |

            ## Sessions
            | Code | Name | Day | Type |
            | --- | --- | --- | --- |
            | M | Morning mobility | Daily | mobility |
            | C | Engine + core | Wed | cardio |
            | D | Full body | Thu | strength |
            | W | Weigh-in | Sun | body |
            | T | Benchmark test | Every 4 weeks | test |

            ## Exercises
            | Session | Exercise | Sets × reps | Log as | Cue |
            | --- | --- | --- | --- | --- |
            | C, T | Longest continuous run | in one go | mins | Unbroken |
            | D | KB or trap-bar deadlift | 3 × 8 | load | Light |
            | W | Waist | monthly | cm | Belly button |
            | T | SkiErg 1000 m | 1 all-out | time | Log as m:ss |

            ## Badges
            | Badge | Exercise | Target |
            | --- | --- | --- |
            | First rep | sessions | 1 |
            | Perfect week | best week | 5 |
            | Habit formed | streak | 4 |
            | PR hunter | prs | 10 |
            | Mobility week | mobility streak | 7 |
            | 50 mornings | mobility days | 50 |
            | Gate 1: 20 min run | Longest continuous run | 20 |
            | Ski sub-4:30 | SkiErg 1000 m | 4:30 |
            | Bodyweight deadlift | KB or trap-bar deadlift | bodyweight |
            | Waist −3 cm | Waist | -3 |
            """;

        var result = LegacyConverter.Convert(new LegacyFolder(
            config,
            new Dictionary<string, string>(),
            new Dictionary<string, string>()));

        var badges = result.Document.Badges!;
        Assert.Equal(9, badges.Count);
        Assert.DoesNotContain(badges, b => b.Id == "bodyweight-deadlift");

        Assert.Contains(badges, b => b.Id == "first-rep" && b.Metric == "sessions" && b.Target == 1);
        Assert.Contains(badges, b => b.Id == "perfect-week" && b.Metric == "bestweek" && b.Target == 5);
        Assert.Contains(badges, b => b.Id == "habit-formed" && b.Metric == "streak" && b.Target == 4);
        Assert.Contains(badges, b => b.Id == "pr-hunter" && b.Metric == "prs" && b.Target == 10);
        Assert.Contains(badges, b => b.Id == "mobility-week" && b.Metric == "streak:mobility" && b.Target == 7);
        Assert.Contains(badges, b => b.Id == "50-mornings" && b.Metric == "days:mobility" && b.Target == 50);
        Assert.Contains(badges, b => b.Id == "gate-1-20-min-run" && b.Metric == "longest-continuous-run" && b.Target == 1200);
        Assert.Contains(badges, b => b.Id == "ski-sub-4-30" && b.Metric == "skierg-1000-m" && b.Target == 270);
        Assert.Contains(badges, b => b.Id == "waist-3-cm" && b.Metric == "waist" && b.Target == -3);

        Assert.Contains(result.Notes, n => n.Kind == ConversionNoteKind.Dropped && n.Subject == "Badge: Bodyweight deadlift");
    }
}
