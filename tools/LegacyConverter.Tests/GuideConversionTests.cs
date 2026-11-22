namespace LegacyConverter.Tests;

public sealed class GuideConversionTests
{
    private const string MinimalConfig = """
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
        | A | Lower + core | Mon | strength |

        ## Exercises
        | Session | Exercise | Sets × reps | Log as | Cue |
        | --- | --- | --- | --- | --- |
        | A | Goblet squat | 3 × 8–10 | load | Chest up |
        | M | Cat-cow | × 8 slow | - | Spine |
        | M | Bird dog | × 5 per side, 3 s hold | - | Back |

        ## Badges
        | Badge | Exercise | Target |
        | --- | --- | --- |
        | First rep | sessions | 1 |
        """;

    [Fact]
    public void Convert_ParsesCompleteGuideAndMultiExerciseGuideAndMyNotes()
    {
        var gobletGuide = """
            ---
            type: guide
            exercises:
              - "Goblet squat"
            ---

            # Goblet squat

            ```dataviewjs
            await dv.view("Fitness/_scripts/fitness", {section: "guide"})
            ```

            **Works:** quads, glutes, core.

            **Why it's in your plan:** builds squat depth.

            ## Setup

            Hold dumbbell at chest.

            ## How to do it

            1. Brace.
            2. Squat.

            ## Cues

            - Chest proud
            - Knees out

            ## Common mistakes

            - Heels lifting

            ## Easier / harder

            - **Easier:** box squat
            - **Harder:** pause at bottom

            ## Watch-outs for you

            - Knees: stay pain-free.

            ## My notes

            - 
            """;

        var mobilityGuide = """
            ---
            type: guide
            exercises:
              - "Cat-cow"
              - "Bird dog"
            ---

            # Morning mobility routine

            **Why it's in your plan:** daily spine prep.

            ## My notes

            - Felt looser after week 2
            """;

        var guides = new Dictionary<string, string>
        {
            ["Goblet squat.md"] = gobletGuide,
            ["Morning mobility routine.md"] = mobilityGuide,
        };

        var result = LegacyConverter.Convert(new LegacyFolder(
            MinimalConfig,
            guides,
            new Dictionary<string, string>()));

        Assert.Equal(3, result.Document.Guides!.Count);

        var goblet = result.Document.Guides.Single(g => g.ExerciseId == "goblet-squat");
        Assert.Equal("quads, glutes, core.", goblet.Works);
        Assert.Equal("builds squat depth.", goblet.WhyInPlan);
        Assert.Equal("Hold dumbbell at chest.", goblet.Setup);
        Assert.Equal("1. Brace.\n2. Squat.", goblet.HowTo);
        Assert.Equal(["Chest proud", "Knees out"], goblet.Cues);
        Assert.Equal(["Heels lifting"], goblet.CommonMistakes);
        Assert.Equal("box squat", goblet.Easier);
        Assert.Equal("pause at bottom", goblet.Harder);
        Assert.Equal(["Knees: stay pain-free."], goblet.WatchOuts);
        Assert.Null(goblet.VideoUrl);
        Assert.Null(goblet.PersonalNotes);

        var catCow = result.Document.Guides.Single(g => g.ExerciseId == "cat-cow");
        var birdDog = result.Document.Guides.Single(g => g.ExerciseId == "bird-dog");
        Assert.Equal("daily spine prep.", catCow.WhyInPlan);
        Assert.Equal("Felt looser after week 2", catCow.PersonalNotes);
        Assert.Equal(catCow with { ExerciseId = "bird-dog" }, birdDog);

        var squatEx = result.Document.Exercises!.Single(e => e.Id == "goblet-squat");
        Assert.NotNull(squatEx.Tags);
        Assert.Contains("quads", squatEx.Tags.MuscleGroups);
    }

    [Fact]
    public void Convert_NotesUnknownGuideSections()
    {
        var guide = """
            ---
            type: guide
            exercises:
              - "Goblet squat"
            ---

            ## Custom section

            Some extra text.
            """;

        var result = LegacyConverter.Convert(new LegacyFolder(
            MinimalConfig,
            new Dictionary<string, string> { ["Custom.md"] = guide },
            new Dictionary<string, string>()));

        Assert.Contains(result.Notes, n => n.Kind == ConversionNoteKind.Warning && n.Subject.Contains("Custom section"));
    }
}
