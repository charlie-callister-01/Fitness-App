namespace Core.Rules.Editing;

/// <summary>The reasons offered when skipping an Exercise, in order. All are optional.</summary>
public static class SkipReasons
{
    public static readonly IReadOnlyList<string> All = ["Short on time", "Pain", "No equipment", "Other"];
}
