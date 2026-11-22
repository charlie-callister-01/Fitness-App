using System.Globalization;
using System.Text.Json;

namespace Core.Serialisation;

internal sealed partial class FieldReader
{
    /// <summary>An optional number that, when present, must be zero or more (zero is allowed: an unweighted set).</summary>
    public double? OptionalNonNegativeDouble(string name)
    {
        if (!TryTakeField(name, out var value))
        {
            return null;
        }

        var path = ChildPath(name);
        if (ReadNumber(path, value) is not { } number)
        {
            return null;
        }

        if (number < 0)
        {
            Sink.Error(path, "out_of_range", $"{path} can't be negative (it was {number.ToString(CultureInfo.InvariantCulture)}).");
            return null;
        }

        return number;
    }

    /// <summary>A required whole number that must be between <paramref name="min"/> and <paramref name="max"/> inclusive.</summary>
    public int RequiredIntInRange(string name, int min, int max)
    {
        if (!TryTakeRequired(name, out var value))
        {
            return 0;
        }

        var path = ChildPath(name);
        if (ParseInt(path, value, int.MinValue) is not { } number)
        {
            return 0;
        }

        return CheckRange(path, number, min, max) ?? 0;
    }

    /// <summary>An optional whole number that, when present, must be between <paramref name="min"/> and <paramref name="max"/> inclusive.</summary>
    public int? OptionalIntInRange(string name, int min, int max) =>
        OptionalInt(name, int.MinValue) is { } number ? CheckRange(ChildPath(name), number, min, max) : null;

    private int? CheckRange(string path, int number, int min, int max)
    {
        if (number < min || number > max)
        {
            Sink.Error(path, "out_of_range", $"{path} must be between {min} and {max} (it was {number}).");
            return null;
        }

        return number;
    }

    /// <summary>An optional enum; null when absent or invalid (invalid is reported).</summary>
    public T? OptionalEnum<T>(string name) where T : struct, Enum
    {
        if (!TryTakeField(name, out var value))
        {
            return null;
        }

        return ParseEnum<T>(ChildPath(name), value);
    }

    /// <summary>An optional array of enum values; each bad item is reported at its own index.</summary>
    public IReadOnlyList<T> OptionalEnumList<T>(string name) where T : struct, Enum
    {
        if (!TryTakeField(name, out var value))
        {
            return [];
        }

        var path = ChildPath(name);
        if (value.ValueKind != JsonValueKind.Array)
        {
            Sink.Error(path, "wrong_type", $"{path} must be a list.");
            return [];
        }

        var items = new List<T>();
        var index = 0;
        foreach (var item in value.EnumerateArray())
        {
            if (ParseEnum<T>($"{path}[{index}]", item) is { } parsed)
            {
                items.Add(parsed);
            }

            index++;
        }

        return items;
    }
}
