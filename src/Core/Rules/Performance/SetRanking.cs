namespace Core.Rules.Performance;

/// <summary>
/// Compares ranking keys so that "better" always means a larger key. The per-kind keys themselves come from
/// <see cref="LogKinds.LogKindRules.Key"/>.
/// </summary>
internal static class SetRanking
{
    /// <summary>Epley: kg x (1 + reps / 30).</summary>
    public static double EstimatedOneRepMax(double weightKg, int reps) => weightKg * (1 + reps / 30.0);

    public static int Compare(double[] a, double[] b)
    {
        for (var i = 0; i < Math.Min(a.Length, b.Length); i++)
        {
            var c = a[i].CompareTo(b[i]);
            if (c != 0) return c;
        }
        return 0;
    }
}
