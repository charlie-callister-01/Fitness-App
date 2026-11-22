using Core.Model;
using Core.Rules.Library;
using Core.Tests.Ids;

namespace Core.Tests.Rules.Library;

public class NewExerciseRulesTests
{
    private static Exercise Ex(string id, string name) => new(id, name, LogKind.Load, null, false);

    private static readonly Exercise[] Library = [Ex("squat", "Back Squat"), Ex("row", "Row")];

    private static NewExerciseResult Create(string? name, LogKind kind = LogKind.Load, ExerciseTags? tags = null, string? cue = null) =>
        NewExerciseRules.Create(name, kind, tags, cue, Library, new SequentialIdGenerator("ex"));

    private static NewExerciseResult.Valid Valid(NewExerciseResult result) => Assert.IsType<NewExerciseResult.Valid>(result);

    private static string Reason(NewExerciseResult result) => Assert.IsType<NewExerciseResult.Invalid>(result).Message;

    [Fact]
    public void A_valid_Exercise_gets_a_permanent_id_a_trimmed_name_and_is_not_a_Benchmark_exercise()
    {
        var exercise = Valid(Create("  Box Jump  ", LogKind.Reps)).Exercise;

        Assert.Equal("ex-1", exercise.Id);
        Assert.Equal("Box Jump", exercise.Name);
        Assert.Equal(LogKind.Reps, exercise.LogKind);
        Assert.False(exercise.IsBenchmark);
        Assert.Null(exercise.Tags);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void An_empty_name_is_refused(string? name) => Assert.Contains("name", Reason(Create(name)));

    [Theory]
    [InlineData("Row")]
    [InlineData("row")]
    [InlineData("  ROW ")]
    public void A_duplicate_name_is_refused_naming_the_existing_Exercise_whatever_the_case_or_spaces(string name) =>
        Assert.Contains("\"Row\"", Reason(Create(name)));

    [Fact]
    public void A_name_that_only_contains_an_existing_name_is_allowed() => Assert.Equal("Rowing", Valid(Create("Rowing")).Exercise.Name);

    [Theory]
    [InlineData(LogKind.BodyMeasurementKg)]
    [InlineData(LogKind.BodyMeasurementCm)]
    public void The_body_measurement_kinds_are_not_offered_and_are_refused(LogKind kind)
    {
        Assert.DoesNotContain(kind, NewExerciseRules.OfferedKinds);
        Assert.Contains("logged", Reason(Create("Waist", kind)));
    }

    [Fact]
    public void Every_other_kind_is_offered() =>
        Assert.Equal(Enum.GetValues<LogKind>().Length - 2, NewExerciseRules.OfferedKinds.Count);

    [Fact]
    public void Tags_are_cleaned_and_kept_and_empty_tags_become_none()
    {
        var tagged = Valid(Create("A", tags: new ExerciseTags([" legs ", "LEGS", ""], ["barbell"], []))).Exercise.Tags!;
        var none = Valid(Create("B", tags: new ExerciseTags([" "], [], []))).Exercise.Tags;

        Assert.Equal(["legs"], tagged.MuscleGroups);
        Assert.Equal(["barbell"], tagged.Equipment);
        Assert.Null(none);
    }

    [Fact]
    public void A_cue_becomes_the_only_Cue_of_a_new_Guide()
    {
        var valid = Valid(Create("Box Jump", cue: "  Land softly "));

        Assert.Equal(valid.Exercise.Id, valid.Guide!.ExerciseId);
        Assert.Equal(["Land softly"], valid.Guide.Cues);
        Assert.Null(valid.Guide.HowTo);
        Assert.Null(valid.Guide.PersonalNotes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void No_cue_means_no_Guide(string? cue) => Assert.Null(Valid(Create("Box Jump", cue: cue)).Guide);

    [Fact]
    public void A_refused_Exercise_does_not_use_up_an_id()
    {
        var ids = new SequentialIdGenerator("ex");

        NewExerciseRules.Create("", LogKind.Load, null, null, Library, ids);
        var valid = Assert.IsType<NewExerciseResult.Valid>(NewExerciseRules.Create("New", LogKind.Load, null, null, Library, ids));

        Assert.Equal("ex-1", valid.Exercise.Id);
    }
}
