using System.Globalization;
using Core.Exchange;

namespace Core.Rules.Insight;

/// <summary>The choices the DateRangePicker offers.</summary>
public enum RangePreset
{
    Last4Weeks,
    Last12Weeks,
    WholePlan,
    Custom,
}

/// <summary>A resolved range, or the plain reason it cannot be applied.</summary>
public sealed record RangeResult(DateRange? Range, string? Error)
{
    public bool IsValid => Range is not null;
}

/// <summary>Turns a preset (or a custom start and end) into an inclusive date range. A screen choice: never stored.</summary>
public static class RangeResolver
{
    public const string StartAfterEnd = "The start date is after the end date.";

    public const string MissingDate = "Pick a start and an end date.";

    /// <summary>
    /// Presets end today and cover whole days back from it (12 weeks is 84 days including today), whatever the Plan start;
    /// Whole Plan runs from the Plan start (or today, if the Plan has not started or there is no Plan).
    /// </summary>
    public static RangeResult Resolve(RangePreset preset, DateOnly today, DateOnly? planStart, DateOnly? customStart = null, DateOnly? customEnd = null)
    {
        switch (preset)
        {
            case RangePreset.Custom:
                if (customStart is not { } start || customEnd is not { } end) return new RangeResult(null, MissingDate);
                return start > end ? new RangeResult(null, StartAfterEnd) : new RangeResult(new DateRange(start, end), null);
            case RangePreset.WholePlan:
                return Ok(planStart ?? today, today);
            default:
                var from = today.AddDays(1 - (preset == RangePreset.Last4Weeks ? 28 : 84));
                return Ok(from, today);
        }
    }

    public static string PresetLabel(RangePreset preset) => preset switch
    {
        RangePreset.Last4Weeks => "Last 4 weeks",
        RangePreset.Last12Weeks => "Last 12 weeks",
        RangePreset.WholePlan => "Whole Plan",
        _ => "Custom",
    };

    /// <summary>The field text: "Last 12 weeks · 24 Aug – 16 Nov".</summary>
    public static string Describe(RangePreset preset, DateRange range) =>
        $"{PresetLabel(preset)} · {range.From.ToString("d MMM", CultureInfo.InvariantCulture)} – {range.To.ToString("d MMM", CultureInfo.InvariantCulture)}";

    private static RangeResult Ok(DateOnly from, DateOnly to) => new(new DateRange(from > to ? to : from, to), null);
}

/// <summary>What the owner picked: the preset and the dates it resolved to.</summary>
public sealed record RangeChoice(RangePreset Preset, DateRange Range)
{
    public string Text => RangeResolver.Describe(Preset, Range);
}
