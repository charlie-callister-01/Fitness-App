using Core.Model;

namespace Ui.Components;

/// <summary>
/// What the ExercisePicker offers: Exercises similar to the one in hand first, then the rest, both already without the Exercises in
/// the Workout. <see cref="ShowSearch"/> is true when the library is big enough to need a search box.
/// </summary>
public sealed record ExercisePickerChoices(IReadOnlyList<Exercise> Similar, IReadOnlyList<Exercise> Others, bool ShowSearch);
