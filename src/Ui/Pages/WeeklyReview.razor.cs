using System.Globalization;
using Core.Rules.Home;
using Core.Rules.Insight;
using Core.Rules.LogKinds;
using Core.Rules.Units;
using Core.Storage;
using Core.Time;
using Microsoft.AspNetCore.Components;

namespace Ui.Pages;

public sealed partial class WeeklyReview : IDisposable
{
    private WeeklyReviewView _view = default!;
    private PageContext Page = default!;
    private DateOnly? _selected;

    [Inject] private AppData Data { get; set; } = default!;

    [Inject] private IClock Clock { get; set; } = default!;

    private UnitConverter Converter => new(Data.Snapshot.Settings);

    private string WeekText => _view.IsCurrentWeek
        ? $"This week, from {Day(_view.Monday)}"
        : $"Week of {Day(_view.Monday)}";

    private string VerdictTitle => _view.Verdict switch
    {
        WeeklyVerdict.OnTrack => "On track",
        WeeklyVerdict.Behind => "Behind",
        WeeklyVerdict.TargetOutOfReach => "Target out of reach",
        WeeklyVerdict.TargetHit => "Target hit",
        _ => "On a Break",
    };

    private string LastWeekText => _view.LastWeekAverage is { } last
        ? $"last week {Num(last)} {_view.UnitLabel}"
        : "no Weigh-ins last week";

    protected override void OnInitialized()
    {
        Page = new PageContext(Data, Clock);
        Data.Changed += OnDataChanged;
        Rebuild();
    }

    public void Dispose() => Data.Changed -= OnDataChanged;

    private void OnDataChanged() => _ = InvokeAsync(() =>
    {
        Rebuild();
        StateHasChanged();
    });

    private void Rebuild() => _view = WeeklyReviewBuilder.Build(Data.Snapshot, _selected ?? Page.Today, Page.Today);

    private void Select(DateOnly? monday)
    {
        if (monday is null) return;
        _selected = monday;
        Rebuild();
    }

    private static string Day(DateOnly date) => InsightFormat.WeekdayDay(date);

    private static string Num(double value) => InsightFormat.OneDecimal(value);

    private string ResultText(RecentPr pr) =>
        Data.Snapshot.Exercises.FirstOrDefault(e => e.Id == pr.ExerciseId) is { } exercise
            ? SetText.Result(exercise.LogKind, pr.Set, Converter)
            : "";
}
