using System.Text;

namespace LegacyConverter;

internal static class LegacyIds
{
    /// <summary>
    /// Converts a legacy Exercise/Badge/Phase name or front-matter key into its canonical lowercase-hyphen ID.
    /// </summary>
    public static string ToId(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var sb = new StringBuilder(name.Length);
        var pendingHyphen = false;

        foreach (var ch in name.Trim().ToLowerInvariant())
        {
            if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9'))
            {
                if (pendingHyphen && sb.Length > 0)
                {
                    sb.Append('-');
                }

                sb.Append(ch);
                pendingHyphen = false;
            }
            else
            {
                pendingHyphen = true;
            }
        }

        if (sb.Length == 0)
        {
            throw new FormatException($"Cannot derive a valid ID from '{name}'.");
        }

        return sb.ToString();
    }
}
