using Core.Rules.Insight;
using Microsoft.AspNetCore.Components.Web;

namespace Ui.Components;

/// <summary>The weight variant of <see cref="LineChart"/>: a prepared <see cref="WeightView"/> drawn as dots, a rolling line, a ringed lowest and a goal line.</summary>
public sealed partial class LineChart
{
    private int? _wSelected;
    private bool _wTable;

    private IEnumerable<double> WeightValues =>
        Weight!.Points.SelectMany(p => new[] { p.Value, p.Rolling }).Concat(Weight.Goal is { } g ? [g] : []);

    private double WeightPad => (WeightValues.Max() - WeightValues.Min()) is var span and > 0 ? span * 0.1 : 1;

    private double WeightLo => WeightValues.Min() - WeightPad;

    private double WeightHi => WeightValues.Max() + WeightPad;

    private int WeightFirstDay => Weight!.Points[0].Date.DayNumber;

    private int WeightDays => Math.Max(1, Weight!.Points[^1].Date.DayNumber - WeightFirstDay);

    private double WX(int i) => Left + (Weight!.Points[i].Date.DayNumber - WeightFirstDay) / (double)WeightDays * (PlotRight - Left);

    private double WY(double value) => PlotBottom - (value - WeightLo) / (WeightHi - WeightLo) * (PlotBottom - Top);

    private string WeightLine => string.Join(' ', Weight!.Points.Select((p, i) => $"{N(WX(i))},{N(WY(p.Rolling))}"));

    private string WeightBest => $"Lowest {Value(Weight!.Points.First(p => p.IsLowest).Value)} {Weight.UnitLabel}";

    private string WeightPointLabel(int i) =>
        $"{DateText(Weight!.Points[i].Date)}: {Value(Weight.Points[i].Value)} {Weight.UnitLabel}, average {Value(Weight.Points[i].Rolling)}{(Weight.Points[i].IsLowest ? ", lowest" : "")}";

    private IEnumerable<double> WeightGrid() =>
        Enumerable.Range(0, GridLines + 1).Select(i => WeightLo + (WeightHi - WeightLo) * i / GridLines);

    private void SelectWeight(int index) => _wSelected = _wSelected == index ? null : index;

    private void OnWeightKey(KeyboardEventArgs e, int index)
    {
        if (e.Key is "Enter" or " ") SelectWeight(index);
    }

    private void ToggleWeightTable() => _wTable = !_wTable;
}
