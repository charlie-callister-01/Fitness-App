using Core.Model;

namespace LegacyConverter;

internal static class ExerciseTagsTable
{
    private static readonly Dictionary<string, ExerciseTags> TagsByExerciseId = new(StringComparer.Ordinal)
    {
        ["arnold-press"] = Tags(["shoulders", "triceps", "upper-back"], ["dumbbell"], ["push"]),
        ["back-extension"] = Tags(["glutes", "hamstrings", "lower-back"], ["bodyweight"], ["hinge"]),
        ["band-pull-apart-wall-slide"] = Tags(["rear-delts", "mid-back", "traps"], ["band"], ["pull"]),
        ["bird-dog"] = Tags(["core", "lower-back"], ["bodyweight"], ["stability"]),
        ["cat-cow"] = Tags(["spine"], ["bodyweight"], ["mobility"]),
        ["chest-supported-row"] = Tags(["lats", "mid-back", "rear-delts", "biceps"], ["machine"], ["pull"]),
        ["cool-down"] = Tags(["mobility"], ["bodyweight"], ["mobility"]),
        ["db-romanian-deadlift"] = Tags(["hamstrings", "glutes", "back", "grip"], ["dumbbell"], ["hinge"]),
        ["dead-bug"] = Tags(["core"], ["bodyweight"], ["stability"]),
        ["deep-squat-hold"] = Tags(["hips", "ankles"], ["bodyweight"], ["mobility"]),
        ["desk-resets"] = Tags(["mobility"], ["bodyweight"], ["mobility"]),
        ["doorway-chest-stretch"] = Tags(["chest", "shoulders"], ["bodyweight"], ["mobility"]),
        ["easy-cardio"] = Tags(["cardio"], ["bodyweight"], ["cardio"]),
        ["external-rotation"] = Tags(["rotator-cuff"], ["cable"], ["rotation"]),
        ["face-pull"] = Tags(["rear-delts", "traps", "rotator-cuff"], ["cable"], ["pull"]),
        ["farmers-carry"] = Tags(["grip", "traps", "core"], ["dumbbell"], ["carry"]),
        ["finisher"] = Tags(["cardio", "full-body"], ["erg"], ["conditioning"]),
        ["glute-bridge-bird-dog"] = Tags(["glutes", "core", "lower-back"], ["bodyweight"], ["activation"]),
        ["glute-bridge-pull-apart"] = Tags(["glutes", "rear-delts", "mid-back"], ["band"], ["activation"]),
        ["goblet-squat"] = Tags(["quads", "glutes", "core", "upper-back"], ["dumbbell"], ["squat"]),
        ["half-kneeling-hip-flexor-stretch"] = Tags(["hip-flexors"], ["bodyweight"], ["mobility"]),
        ["incline-db-press"] = Tags(["chest", "shoulders", "triceps"], ["dumbbell"], ["push"]),
        ["kb-or-trap-bar-deadlift"] = Tags(["glutes", "hamstrings", "quads", "back", "grip"], ["kettlebell", "trap-bar"], ["hinge"]),
        ["lat-pulldown"] = Tags(["lats", "upper-back", "biceps"], ["cable"], ["pull"]),
        ["long-mobility"] = Tags(["mobility"], ["bodyweight"], ["mobility"]),
        ["longest-continuous-run"] = Tags(["cardio", "legs"], ["running"], ["cardio"]),
        ["lunch-walk"] = Tags(["cardio"], ["bodyweight"], ["cardio"]),
        ["machine-chest-press"] = Tags(["chest", "shoulders", "triceps"], ["machine"], ["push"]),
        ["mcgill-big-3"] = Tags(["core"], ["bodyweight"], ["stability"]),
        ["pallof-press"] = Tags(["core"], ["cable"], ["anti-rotation"]),
        ["plank"] = Tags(["core", "obliques", "glutes"], ["bodyweight"], ["stability"]),
        ["plank-max"] = Tags(["core", "obliques", "glutes"], ["bodyweight"], ["stability"]),
        ["push-up"] = Tags(["chest", "shoulders", "triceps", "core"], ["bodyweight"], ["push"]),
        ["push-up-max"] = Tags(["chest", "shoulders", "triceps", "core"], ["bodyweight"], ["push"]),
        ["push-up-on-handles"] = Tags(["chest", "shoulders", "triceps", "core"], ["parallettes"], ["push"]),
        ["reverse-lunge"] = Tags(["quads", "glutes"], ["dumbbell"], ["lunge"]),
        ["run-walk-intervals"] = Tags(["cardio", "legs"], ["running"], ["cardio"]),
        ["running-distance"] = Tags(["cardio", "legs"], ["running"], ["cardio"]),
        ["seated-cable-row"] = Tags(["lats", "mid-back", "rear-delts", "biceps"], ["cable"], ["pull"]),
        ["side-plank"] = Tags(["obliques", "core", "glutes"], ["bodyweight"], ["stability"]),
        ["single-arm-cable-row"] = Tags(["lats", "mid-back", "rear-delts", "core"], ["cable"], ["pull"]),
        ["skierg-1000-m"] = Tags(["lats", "core", "triceps", "legs", "cardio"], ["skierg"], ["cardio"]),
        ["skierg-intervals"] = Tags(["lats", "core", "triceps", "legs", "cardio"], ["skierg"], ["cardio"]),
        ["step-up"] = Tags(["quads", "glutes"], ["dumbbell"], ["lunge"]),
        ["suitcase-carry"] = Tags(["obliques", "core", "grip"], ["dumbbell"], ["carry"]),
        ["thoracic-open-book"] = Tags(["upper-back"], ["bodyweight"], ["mobility"]),
        ["waist"] = Tags(["measurements"], ["tape"], ["measurement"]),
        ["wall-slides"] = Tags(["shoulders", "upper-back"], ["bodyweight"], ["mobility"]),
        ["warm-up"] = Tags(["mobility"], ["bodyweight"], ["warm-up"]),
        ["warm-up-walk"] = Tags(["cardio"], ["treadmill"], ["warm-up"]),
        ["wrist-curls"] = Tags(["forearms"], ["dumbbell", "band"], ["isolation"]),
        ["wrist-stretches"] = Tags(["wrists", "forearms"], ["bodyweight"], ["mobility"]),
    };

    public static IReadOnlyList<Exercise> ApplyTags(
        IReadOnlyList<Exercise> exercises,
        List<ConversionNote> notes)
    {
        var result = new List<Exercise>(exercises.Count);
        foreach (var ex in exercises)
        {
            if (TagsByExerciseId.TryGetValue(ex.Id, out var tags))
            {
                result.Add(ex with { Tags = tags });
            }
            else
            {
                notes.Add(new ConversionNote(
                    ConversionNoteKind.Warning,
                    $"Tags: {ex.Name}",
                    "No tag entry in ExerciseTagsTable; left empty."));
                result.Add(ex);
            }
        }

        return result;
    }

    private static ExerciseTags Tags(string[] muscles, string[] equipment, string[] patterns) =>
        new(muscles, equipment, patterns);
}
