namespace Ui.Pages;

/// <summary>Which number of an Exercise row the owner is typing.</summary>
public enum RowNumber
{
    Sets,
    Reps,
    HighestReps,
}

/// <summary>
/// The text typed into a row's number boxes that is not a whole number. Such text stays on screen exactly as typed (so the owner can fix
/// it), the row keeps its last good number, and the editor holds Save back with <see cref="Message"/> until the text is a number or
/// blank. A row is told apart by its Session and Exercise, so reordering does not lose what was typed.
/// </summary>
public sealed class RowNumberInputs
{
    public const string Message = "Enter a whole number.";

    private readonly Dictionary<(string SessionId, string ExerciseId, RowNumber Field), string> _unreadable = [];

    /// <summary>Counts every change here, so a cached reading of the editor knows it is out of date.</summary>
    public int Version { get; private set; }

    /// <summary>Some box holds text that is not a number.</summary>
    public bool Any => _unreadable.Count > 0;

    /// <summary>The text typed into this box when it is not a number, or null when the box shows the row's own number.</summary>
    public string? UnreadableText(string sessionId, string exerciseId, RowNumber field) =>
        _unreadable.GetValueOrDefault((sessionId, exerciseId, field));

    /// <summary>This row has a box holding text that is not a number.</summary>
    public bool HasUnreadable(string sessionId, string exerciseId) =>
        _unreadable.Keys.Any(k => k.SessionId == sessionId && k.ExerciseId == exerciseId);

    /// <summary>
    /// Reads what was typed. Blank is "no number" (null) and a whole number is that number; both clear any earlier unreadable text and
    /// return true. Anything else is kept as typed and returns false.
    /// </summary>
    public bool TryRead(string sessionId, string exerciseId, RowNumber field, string? text, out int? value)
    {
        var key = (sessionId, exerciseId, field);
        Version++;
        if (string.IsNullOrWhiteSpace(text))
        {
            _unreadable.Remove(key);
            value = null;
            return true;
        }

        if (int.TryParse(text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var number))
        {
            _unreadable.Remove(key);
            value = number;
            return true;
        }

        _unreadable[key] = text;
        value = null;
        return false;
    }

    /// <summary>Drops what was typed for a row (it was removed).</summary>
    public void Forget(string sessionId, string exerciseId)
    {
        foreach (var key in _unreadable.Keys.Where(k => k.SessionId == sessionId && k.ExerciseId == exerciseId).ToList())
        {
            _unreadable.Remove(key);
        }

        Version++;
    }

    public void Clear()
    {
        _unreadable.Clear();
        Version++;
    }
}
