using System.Globalization;

namespace Core.Rules.Parsing;

/// <summary>Typed decimal numbers: a comma or a dot is the decimal mark, so "86,5" and "86.5" both read as 86.5 whatever the device's locale.</summary>
public static class DecimalText
{
    /// <summary>True when <paramref name="text"/> (trimmed) is a number; <paramref name="value"/> is 0 otherwise.</summary>
    public static bool TryParse(string? text, out double value)
    {
        if (double.TryParse((text ?? "").Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        value = 0;
        return false;
    }
}
