using Core.Model;
using Core.Rules.Insight;
using Core.Rules.Units;
using Core.Storage;
using Core.Time;
using Microsoft.AspNetCore.Components;

namespace Ui.Pages;

public sealed partial class Progress : IDisposable
{
    private static readonly string[] Tabs = ["Progress", "Benchmarks", "Calendar"];

    private string _tab = "Progress";
    private ExerciseGroup? _group;
    private ProgressListView _list = new([]);
    private RangeChoice _range = default!;

    [Inject] private AppData Data { get; set; } = default!;

    [Inject] private IClock Clock { get; set; } = default!;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private ProgressListView List => _list;

    private DateOnly Today => new PageContext(Data, Clock).Today;

    private DateOnly? PlanStart => new PageContext(Data, Clock).ActivePlan?.StartDate;

    private bool HasHistory => Data.Snapshot.Workouts.Count > 0;

    private IEnumerable<ExerciseGroup> GroupChips => Enum.GetValues<ExerciseGroup>();

    private IReadOnlyList<ProgressGroupView> Shown => [.. _list.Groups.Where(g => _group is null || g.Group == _group)];

    protected override void OnInitialized()
    {
        _tab = TabQuery.Pick(Navigation.Uri, Tabs, "Progress");
        _range = new RangeChoice(RangePreset.Last12Weeks, RangeResolver.Resolve(RangePreset.Last12Weeks, Today, PlanStart).Range!);
        Data.Changed += OnDataChanged;
        Rebuild();
    }

    public void Dispose() => Data.Changed -= OnDataChanged;

    private void OnRangeChanged(RangeChoice choice)
    {
        _range = choice;
        Rebuild();
    }

    private void OnDataChanged() => _ = InvokeAsync(() =>
    {
        Rebuild();
        StateHasChanged();
    });

    private void Rebuild()
    {
        var snapshot = Data.Snapshot;
        _list = ProgressList.Build(snapshot.Exercises, new PageContext(Data, Clock).ActivePlan, [.. snapshot.Workouts.Where(w => _range.Range.Contains(w.Date))], new UnitConverter(snapshot.Settings));
    }
}
