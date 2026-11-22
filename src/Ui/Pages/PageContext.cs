using Core.Model;
using Core.Storage;
using Core.Time;

namespace Ui.Pages;

/// <summary>
/// What every page asks of the app before it draws or saves: whether this window can write, the plain reason it cannot, today's date
/// (in the owner's time zone) and the active Plan. Pages read these here instead of working them out themselves.
/// </summary>
public sealed class PageContext(AppData data, IClock clock)
{
    public bool CanWrite => data.Health.CanWrite;

    /// <summary>Why this window cannot save, in plain words, or null when it can.</summary>
    public string? ReadOnlyMessage => Saving.ReadOnlyWording.For(data);

    public DateTimeOffset LocalNow => clock.UtcNow.ToLocalTime();

    public DateOnly Today => DateOnly.FromDateTime(LocalNow.DateTime);

    public Plan? ActivePlan => Core.Rules.PlanEditing.ActivePlan.Of(data.Snapshot.Plans);
}
