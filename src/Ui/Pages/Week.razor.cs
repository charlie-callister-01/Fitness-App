using System.Globalization;
using Core.Ids;
using Core.Model;
using Core.Rules.PlanEditing;
using Core.Rules.Schedule;
using Core.Rules.Weeks;
using Core.Storage;
using Core.Time;
using Microsoft.AspNetCore.Components;
using Ui.Components;
using Ui.Saving;

namespace Ui.Pages;

public sealed partial class Week : IDisposable
{
    private const string DateFormat = "yyyy-MM-dd";

    private PageContext Page = default!;
    private GuardedSave _save = default!;
    private Plan? _plan;
    private IReadOnlyList<WeekRow> _rows = [];
    private IReadOnlyList<DateOnly> _days = [];

    // The suggested week is a proposal held here until Apply; Cancel and Apply both put it away until a Break is added.
    private WeekDraft? _draft;
    private string? _draftBasis;

    // Cancel and Apply put the draft away until the Week schedule or a Break changes. Held per AppData (one per session), never stored.
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<AppData, System.Runtime.CompilerServices.StrongBox<string?>> PutAway = [];

    private WeekRow? _moveRow;
    private DateOnly _moveDate;
    private bool _makeDefault;
    private string? _moveMessage;

    private bool _breakOpen;
    private string? _from;
    private string? _to;
    private string? _reason;
    private string? _breakMessage;

    [Inject] private AppData Data { get; set; } = default!;

    [Inject] private IClock Clock { get; set; } = default!;

    [Inject] private IIdGenerator Ids { get; set; } = default!;

    [Inject] private SaveFailureNotice Notice { get; set; } = default!;

    private string RangeText => _days.Count == 0
        ? ""
        : $"{_days[0].ToString("d MMM", CultureInfo.InvariantCulture)} to {_days[^1].ToString("d MMM", CultureInfo.InvariantCulture)}";

    private string MoveTitle => _moveRow is { } row ? $"Move {row.Name}" : "Move";

    protected override void OnInitialized()
    {
        Page = new PageContext(Data, Clock);
        _save = new GuardedSave(Data, Notice);
        _save.Started += OnSaveStarted;
        Data.Changed += OnDataChanged;
        Rebuild();
    }

    public void Dispose()
    {
        Data.Changed -= OnDataChanged;
        _save.Started -= OnSaveStarted;
    }

    private void OnSaveStarted() => _ = InvokeAsync(StateHasChanged);

    private void OnDataChanged() => _ = InvokeAsync(() =>
    {
        Rebuild();
        StateHasChanged();
    });

    private void Rebuild()
    {
        var snapshot = Data.Snapshot;
        _plan = Page.ActivePlan;
        _days = [.. WeekCalendar.DaysOf(WeekCalendar.MondayOf(Page.Today))];
        if (_plan is null)
        {
            _rows = [];
            _draft = null;
            return;
        }

        _rows = WeekRows.Build(_plan, snapshot.WeekSchedules, snapshot.Breaks, snapshot.Workouts, Page.Today);
        var basis = Fingerprint(snapshot);
        if (basis == _draftBasis)
        {
            return;
        }

        _draftBasis = basis;
        _draft = basis != PutAwayBasis && WeekRows.BreakTouchesWeek(snapshot.Breaks, Page.Today)
            ? WeekEditRules.SuggestDraft(_plan, snapshot.Exercises, snapshot.WeekSchedules, snapshot.Breaks, snapshot.Workouts, Page.Today)
            : null;
    }

    private string? PutAwayBasis
    {
        get => PutAway.TryGetValue(Data, out var box) ? box.Value : null;
        set => PutAway.GetOrCreateValue(Data).Value = value;
    }

    private string Fingerprint(DataSnapshot snapshot) => string.Join(";", snapshot.Breaks.OrderBy(b => b.Id, StringComparer.Ordinal).Select(b => $"{b.Id}|{b.StartDate:yyyy-MM-dd}|{b.EndDate:yyyy-MM-dd}"))
        + "#" + string.Join(";", snapshot.WeekSchedules.OrderBy(s => s.WeekStart).Select(s => $"{s.WeekStart:yyyy-MM-dd}:" + string.Join(",", s.Moves.Select(m => $"{m.SessionId}>{m.Date:yyyy-MM-dd}"))))
        + "#" + Page.Today.ToString(DateFormat, CultureInfo.InvariantCulture);

    private static string DayText(DateOnly date) => date.ToString("ddd d MMM", CultureInfo.InvariantCulture);

    private static StateChipKind? ChipOf(WeekRow row) => row.Chip switch
    {
        WeekRowChip.Done => StateChipKind.Done,
        WeekRowChip.Missed => StateChipKind.Missed,
        WeekRowChip.Today => StateChipKind.Today,
        _ => null,
    };

    private string UnplacedNames(WeekDraft draft) =>
        string.Join(", ", draft.Unplaced.Select(id => _plan?.Sessions.FirstOrDefault(s => s.Id == id)?.Name ?? id));

    // Move.

    private void OnMove(WeekRow row)
    {
        _moveRow = row;
        _moveDate = row.Date;
        _makeDefault = false;
        _moveMessage = null;
    }

    private void CloseMove() => _moveRow = null;

    private async Task OnSaveMoveAsync()
    {
        if (_plan is not { } plan || _moveRow is not { } row)
        {
            return;
        }

        var monday = WeekCalendar.MondayOf(Page.Today);
        var result = WeekEditRules.MoveSession(plan, Data.Snapshot.WeekSchedules, row.SessionId, monday, _moveDate, _makeDefault);
        if (!result.TryGet(out var move, out var reason))
        {
            _moveMessage = reason;
            return;
        }

        _moveMessage = null;
        var saved = await _save.CommitAsync(move.ToChanges());
        if (saved.IsSaved)
        {
            _moveRow = null;
        }
    }

    // Add a Break.

    private void OnAddBreak()
    {
        _from = Page.Today.ToString(DateFormat, CultureInfo.InvariantCulture);
        _to = _from;
        _reason = null;
        _breakMessage = null;
        _breakOpen = true;
    }

    private void CloseBreak() => _breakOpen = false;

    private async Task OnStartBreakAsync()
    {
        if (!TryParseDate(_from, out var from) || !TryParseDate(_to, out var to))
        {
            _breakMessage = "Pick both dates.";
            return;
        }

        var result = WeekEditRules.CreateBreak(from, to, _reason, Data.Snapshot.Breaks, Ids);
        if (!result.TryGet(out var created, out var reason))
        {
            _breakMessage = reason;
            return;
        }

        _breakMessage = null;
        var saved = await _save.CommitAsync(Change.Put(created));
        if (saved.IsSaved)
        {
            _breakOpen = false;
        }
    }

    private static bool TryParseDate(string? text, out DateOnly date) =>
        DateOnly.TryParseExact(text, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    // The suggested week.

    private void OnUndo(string sessionId) => _draft = _draft?.Undo(sessionId);

    private void OnCancelDraft()
    {
        PutAwayBasis = _draftBasis;
        _draft = null;
    }

    private async Task OnApplyAsync()
    {
        if (_plan is not { } plan || _draft is not { } draft)
        {
            return;
        }

        var schedule = draft.Apply(plan, Data.Snapshot.WeekSchedules);
        var saved = await _save.CommitAsync(Change.Put(schedule));
        if (saved.IsSaved)
        {
            // Only a saved Apply puts the draft away; a failure leaves the draft and its Undos as they were.
            PutAwayBasis = Fingerprint(Data.Snapshot);
            _draftBasis = PutAwayBasis;
            _draft = null;
        }
    }
}
