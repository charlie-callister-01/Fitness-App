using System.Globalization;
using Core.Rules.Insight;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Ui.Components;

/// <summary>
/// C-17 LineChart. Draws a prepared <see cref="ExerciseSeries"/> as inline SVG (viewBox 358 x 220): the line breaks where the series says
/// so, PR points are larger, Phase bands sit behind the plot, a tap shows a tooltip and a Table toggle shows the same data as a list.
/// </summary>
public sealed partial class LineChart
{
    private const double Width = 358;
    private const double Height = 220;
    private const double Left = 40;
    private const double Right = 12;
    private const double Top = 30;
    private const double Bottom = 40;
    private const int GridLines = 4;

    private int? _selected;
    private bool _table;

    /// <summary>The series to draw. A chart never receives Workouts.</summary>
    [Parameter, EditorRequired]
    public ExerciseSeries Series { get; set; } = default!;

    internal sealed record Band(double X, double Width, int Index, string Focus);

    internal sealed record Segment(string Points);

    private double PlotRight => Width - Right;

    private double PlotBottom => Height - Bottom;

    private double MinValue => Series.Points.Min(p => p.Value);

    private double MaxValue => Series.Points.Max(p => p.Value);

    private double Pad => (MaxValue - MinValue) is var span and > 0 ? span * 0.1 : 1;

    private double Lo => MinValue - Pad;

    private double Hi => MaxValue + Pad;

    private int FirstDay => Series.Points[0].Date.DayNumber;

    private int Days => Math.Max(1, Series.Points[^1].Date.DayNumber - FirstDay);

    private double X(int i) => Left + (Series.Points[i].Date.DayNumber - FirstDay) / (double)Days * (PlotRight - Left);

    private double Y(double value) => PlotBottom - (value - Lo) / (Hi - Lo) * (PlotBottom - Top);

    private static string N(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    private static string Value(double v) => v.ToString("0.#", CultureInfo.InvariantCulture);

    private static string DateText(DateOnly d) => d.ToString("ddd d MMM", CultureInfo.InvariantCulture);

    private string ValueText(int i) => $"{Value(Series.Points[i].Value)} {Series.UnitLabel}";

    private string PointLabel(int i) =>
        $"{DateText(Series.Points[i].Date)}: {ValueText(i)}{(Series.Points[i].IsPr ? ", PR" : "")}";

    private IEnumerable<double> GridValues() =>
        Enumerable.Range(0, GridLines + 1).Select(i => Lo + (Hi - Lo) * i / GridLines);

    /// <summary>The line as one polyline per run of points with no break between them. A lone point is only a dot.</summary>
    private IEnumerable<Segment> Segments()
    {
        var run = new List<int>();
        for (var i = 0; i < Series.Points.Count; i++)
        {
            if (Series.Points[i].BreaksBefore && run.Count > 0)
            {
                yield return ToSegment(run);
                run = [];
            }
            run.Add(i);
        }
        yield return ToSegment(run);
    }

    private Segment ToSegment(List<int> run) => new(string.Join(' ', run.Select(i => $"{N(X(i))},{N(Y(Series.Points[i].Value))}")));

    /// <summary>A band per run of points in the same Phase, reaching halfway to each neighbour. Points outside every Phase get none.</summary>
    private IEnumerable<Band> Bands()
    {
        var focuses = Series.Points.Select(p => p.PhaseFocus).Where(f => f is not null).Distinct().ToList();
        var i = 0;
        while (i < Series.Points.Count)
        {
            var focus = Series.Points[i].PhaseFocus;
            var j = i;
            while (j + 1 < Series.Points.Count && Series.Points[j + 1].PhaseFocus == focus) j++;
            if (focus is not null)
            {
                var from = i == 0 ? Left : (X(i - 1) + X(i)) / 2;
                var to = j == Series.Points.Count - 1 ? PlotRight : (X(j) + X(j + 1)) / 2;
                yield return new Band(from, to - from, focuses.IndexOf(focus), focus);
            }
            i = j + 1;
        }
    }

    private void Select(int index) => _selected = _selected == index ? null : index;

    private void OnKey(KeyboardEventArgs e, int index)
    {
        if (e.Key is "Enter" or " ") Select(index);
    }

    private void ToggleTable() => _table = !_table;
}
