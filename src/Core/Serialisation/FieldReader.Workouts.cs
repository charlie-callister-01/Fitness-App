using System.Globalization;
using System.Text.Json;

namespace Core.Serialisation;

internal sealed partial class FieldReader
{
    /// <summary>An optional number that, when present, must be zero or more (zero is allowed: an unweighted set).</summary>
    public double? OptionalNonNegativeDouble(string name)
    {
        if (!TryGet(name, out var value))
        {
            return null;
        }

        var path = ChildPath(name);
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out var number))
        {
            _sink.Error(path, "wrong_type", $"{path} must be a number.");
            return null;
        }

        if (number < 0)
        {
            _sink.Error(path, "out_of_range", $"{path} can't be negative (it was {number.ToString(CultureInfo.InvariantCulture)}).");
            return null;
        }

        return number;
    }

    /// <summary>An optional whole number that, when present, must be between <paramref name="min"/> and <paramref name="max"/> inclusive.</summary>
    public int? OptionalIntInRange(string name, int min, int max)
    {
        var number = OptionalInt(name, int.MinValue);
        if (number is { } n && (n < min || n > max))
        {
            _sink.Error(ChildPath(name), "out_of_range", $"{ChildPath(name)} must be between {min} and {max} (it was {n}).");
            return null;
        }

        return number;
    }

    /// <summary>An optional enum; null when absent or invalid (invalid is reported).</summary>
    public T? OptionalEnum<T>(string name) where T : struct, Enum
    {
        if (!TryGet(name, out var value))
        {
            return null;
        }

        var errorsBefore = _sink.Errors.Count;
        var parsed = ParseEnum<T>(ChildPath(name), value);
        return _sink.Errors.Count == errorsBefore ? parsed : null;
    }

    /// <summary>An optional array of enum values; each bad item is reported at its own index.</summary>
    public IReadOnlyList<T> OptionalEnumList<T>(string name) where T : struct, Enum
    {
        if (!TryGet(name, out var value))
        {
            return [];
        }

        var path = ChildPath(name);
        if (value.ValueKind != JsonValueKind.Array)
        {
            _sink.Error(path, "wrong_type", $"{path} must be a list.");
            return [];
        }

        var items = new List<T>();
        var index = 0;
        foreach (var item in value.EnumerateArray())
        {
            var errorsBefore = _sink.Errors.Count;
            var parsed = ParseEnum<T>($"{path}[{index}]", item);
            if (_sink.Errors.Count == errorsBefore)
            {
                items.Add(parsed);
            }

            index++;
        }

        return items;
    }
}
