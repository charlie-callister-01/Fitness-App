using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Ui.Components;

/// <summary>
/// Reads the design's icon files, which ship as embedded resources of this assembly
/// (the same files in wwwroot/icons), and strips the provenance metadata they carry.
/// </summary>
public static partial class IconCatalogue
{
    private const string ResourcePrefix = "icons/";

    private static readonly Assembly Source = typeof(IconCatalogue).Assembly;

    /// <summary>File names of every shipped icon, e.g. "icon-plus.svg".</summary>
    public static IReadOnlyCollection<string> ShippedFileNames { get; } =
        Source.GetManifestResourceNames()
            .Where(n => n.StartsWith(ResourcePrefix, StringComparison.Ordinal))
            .Select(n => n[ResourcePrefix.Length..])
            .ToArray();

    /// <summary>"AddExercise" becomes "icon-add-exercise.svg".</summary>
    public static string FileNameOf(IconName name)
    {
        var kebab = new StringBuilder();
        foreach (var c in name.ToString())
        {
            if (char.IsUpper(c) && kebab.Length > 0)
            {
                kebab.Append('-');
            }
            kebab.Append(char.ToLowerInvariant(c));
        }
        return $"icon-{kebab}.svg";
    }

    /// <summary>The icon's inline SVG markup, ready to render.</summary>
    public static string SvgOf(IconName name) => Stripped.GetOrAdd(name, ReadStripped);

    // Each icon is read and stripped once, however often it renders.
    private static readonly ConcurrentDictionary<IconName, string> Stripped = new();

    private static string ReadStripped(IconName name)
    {
        var fileName = FileNameOf(name);
        using var stream = Source.GetManifestResourceStream(ResourcePrefix + fileName)
            ?? throw new InvalidOperationException($"Icon file {fileName} is not shipped for {name}.");
        using var reader = new StreamReader(stream);
        var svg = reader.ReadToEnd();
        svg = MetadataElement().Replace(svg, "");
        return ProvenanceNamespace().Replace(svg, "");
    }

    [GeneratedRegex(@"<metadata>.*?</metadata>", RegexOptions.Singleline)]
    private static partial Regex MetadataElement();

    [GeneratedRegex(@"\s+xmlns:c2pa=""[^""]*""")]
    private static partial Regex ProvenanceNamespace();
}
