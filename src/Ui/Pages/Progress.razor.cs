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

    [Inject] private AppData Data { get; set; } = default!;

    [Inject] private IClock Clock { get; set; } = default!;

    private ProgressListView List => _list;

    private IEnumerable<ExerciseGroup> GroupChips => Enum.GetValues<ExerciseGroup>();

    private IReadOnlyList<ProgressGroupView> Shown => [.. _list.Groups.Where(g => _group is null || g.Group == _group)];

    protected override void OnInitialized()
    {
        Data.Changed += OnDataChanged;
        Rebuild();
    }

    public void Dispose() => Data.Changed -= OnDataChanged;

    private void OnDataChanged() => _ = InvokeAsync(() =>
    {
        Rebuild();
        StateHasChanged();
    });

    private void Rebuild()
    {
        var snapshot = Data.Snapshot;
        _list = ProgressList.Build(snapshot.Exercises, new PageContext(Data, Clock).ActivePlan, snapshot.Workouts, new UnitConverter(snapshot.Settings));
    }
}
