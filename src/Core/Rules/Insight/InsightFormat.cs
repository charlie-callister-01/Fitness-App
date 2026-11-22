using System.Globalization;

namespace Core.Rules.Insight;

/// <summary>
/// The date and number formats the Insight screens share, so the same kind of value reads the same everywhere: "Mon 5 Jan", "5 Jan",
/// "5 Jan 2026", the ISO date a native date input needs, "86.3" and "86.25" (trailing zeros dropped).
/// </summary>
public static class InsightFormat
{
    /// <summary>"Mon 5 Jan".</summary>
    public static string WeekdayDay(DateOnly date) => date.ToString("ddd d MMM", CultureInfo.InvariantCulture);

    /// <summary>"5 Jan".</summary>
    public static string DayMonth(DateOnly date) => date.ToString("d MMM", CultureInfo.InvariantCulture);

    /// <summary>"5 Jan 2026".</summary>
    public static string DayMonthYear(DateOnly date) => date.ToString("d MMM yyyy", CultureInfo.InvariantCulture);

    /// <summary>"2026-01-05", the value of a native date input.</summary>
    public static string Iso(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>One decimal place: "86.3", "86.0".</summary>
    public static string OneDecimal(double value) => value.ToString("0.0", CultureInfo.InvariantCulture);

    /// <summary>Up to two decimals with trailing zeros dropped: "86", "86.25".</summary>
    public static string Trimmed(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
