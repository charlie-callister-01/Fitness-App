namespace Core.Tests.Rules.TestData;

/// <summary>Date helpers shared by the Rules tests.</summary>
internal static class Dates
{
    /// <summary>The Monday that starts the week most Rules tests are written around.</summary>
    public static readonly DateOnly Mon = D(2026, 11, 16);

    public static DateOnly D(int year, int month, int day) => new(year, month, day);
}
