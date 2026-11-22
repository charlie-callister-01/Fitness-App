using System.Globalization;
using Core.Ids;
using Core.Model;
using Core.Rules.Home;
using Core.Rules.Insight;
using Core.Rules.Units;
using Core.Storage;
using Core.Time;
using Microsoft.AspNetCore.Components;
using Ui.Saving;

namespace Ui.Pages;

public sealed partial class Weight : IDisposable
{
    private WeightView _view = default!;
    private PageContext Page = default!;
    private GuardedSave _save = default!;
    private bool _weightOpen;
    private string? _weightMessage;

    [Inject] private AppData Data { get; set; } = default!;

    [Inject] private IClock Clock { get; set; } = default!;

    [Inject] private IIdGenerator Ids { get; set; } = default!;

    [Inject] private SaveFailureNotice Notice { get; set; } = default!;

    private UnitConverter Converter => new(Data.Snapshot.Settings);

    private string LatestWeightText => _view.Now is { } now ? Num(now) : "";

    private string LastWeightText =>
        _view.Now is { } now && _view.NowDate is { } date
            ? $"Last: {Num(now)} {_view.UnitLabel} on {date.ToString("ddd d MMM", CultureInfo.InvariantCulture)}"
            : "";

    private string ChangeText => _view.ChangeSinceStart is not { } change
        ? "-"
        : change == 0
            ? "no change"
            : $"{(change > 0 ? "+" : change < 0 ? "-" : "")}{Num(Math.Abs(change))} {_view.UnitLabel}";

    protected override void OnInitialized()
    {
        Page = new PageContext(Data, Clock);
        _save = new GuardedSave(Data, Notice);
        Data.Changed += OnDataChanged;
        Rebuild();
    }

    public void Dispose() => Data.Changed -= OnDataChanged;

    private void OnDataChanged() => _ = InvokeAsync(() =>
    {
        Rebuild();
        StateHasChanged();
    });

    private void Rebuild() =>
        _view = WeightViewBuilder.Build(Data.Snapshot.WeighIns, Data.Snapshot.Badges, Page.Today, Converter);

    private static string Num(double value) => value.ToString("0.0", CultureInfo.InvariantCulture);

    private string Show(double? value) => value is { } v ? $"{Num(v)} {_view.UnitLabel}" : "-";

    private void OnLogWeight()
    {
        _weightMessage = null;
        _weightOpen = true;
    }

    private void CloseWeight() => _weightOpen = false;

    private async Task<bool> SaveWeightAsync(string text)
    {
        switch (WeighInRules.Create(text, Converter, Page.Today, Ids))
        {
            case WeighInResult.Invalid invalid:
                _weightMessage = invalid.Message;
                return false;
            case WeighInResult.Valid valid:
                _weightMessage = null;
                var result = await _save.CommitAsync(Change.Put(valid.WeighIn));
                _weightOpen = _weightOpen && !result.IsSaved;
                return result.IsSaved;
            default:
                return false;
        }
    }
}
