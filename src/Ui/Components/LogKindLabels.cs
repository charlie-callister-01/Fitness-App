using Core.Model;

namespace Ui.Components;

/// <summary>Plain words for how an Exercise is logged, for the New Exercise form and the Exercise library rows.</summary>
public static class LogKindLabels
{
    public static string For(LogKind kind) => kind switch
    {
        LogKind.Load => "Weight and reps",
        LogKind.BodyweightLoad => "Bodyweight and reps",
        LogKind.Carry => "Carry",
        LogKind.Reps => "Reps only",
        LogKind.Seconds => "Seconds",
        LogKind.Minutes => "Minutes",
        LogKind.DistanceKm => "Distance in km",
        LogKind.DistanceM => "Distance in metres",
        LogKind.Time => "Time",
        LogKind.BodyMeasurementKg => "Body measurement (kg)",
        LogKind.BodyMeasurementCm => "Body measurement (cm)",
        LogKind.Text => "Note",
        LogKind.TickOnly => "Tick when done",
        _ => kind.ToString(),
    };
}
