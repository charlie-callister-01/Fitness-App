using Core.Model;
using Core.Rules.Home;
using Core.Rules.Nudges;
using Core.Tests.Rules.TestData;

namespace Core.Tests.Rules.Home;

public class HomeNudgesTests
{
    private static readonly DateOnly Today = Dates.Mon; // 2026-11-16 (Monday)

    [Fact]
    public void Empty_input_gives_empty_list()
    {
        var result = HomeNudges.Select([], new HashSet<string>(), Today);
        Assert.Empty(result);

        var keys = HomeNudges.KeysFor([], Today);
        Assert.Empty(keys);
    }

    [Fact]
    public void Select_caps_at_two_and_preserves_order()
    {
        var n1 = new Nudge(NudgeKind.RepeatedPain, "Knee pain", Area: BodyArea.Knee);
        var n2 = new Nudge(NudgeKind.MissedSession, "Missed Lower", SessionId: "s-mon");
        var n3 = new Nudge(NudgeKind.OneShortOfTarget, "One short");
        var n4 = new Nudge(NudgeKind.NoRecentBackup, "No backup");

        var selected = HomeNudges.Select([n1, n2, n3, n4], new HashSet<string>(), Today);

        Assert.Equal(2, selected.Count);
        Assert.Same(n1, selected[0].Nudge);
        Assert.Same(n2, selected[1].Nudge);
    }

    [Fact]
    public void Dismissed_key_is_skipped_and_subsequent_nudge_takes_its_place()
    {
        var n1 = new Nudge(NudgeKind.RepeatedPain, "Knee pain", Area: BodyArea.Knee);
        var n2 = new Nudge(NudgeKind.MissedSession, "Missed Lower", SessionId: "s-mon");
        var n3 = new Nudge(NudgeKind.OneShortOfTarget, "One short");

        var keys = HomeNudges.KeysFor([n1, n2, n3], Today);
        var dismissed = new HashSet<string> { keys[0] };

        var selected = HomeNudges.Select([n1, n2, n3], dismissed, Today);

        Assert.Equal(2, selected.Count);
        Assert.Same(n2, selected[0].Nudge);
        Assert.Same(n3, selected[1].Nudge);
    }

    [Fact]
    public void Key_differs_between_weeks()
    {
        var nudge = new Nudge(NudgeKind.NoRecentBackup, "No backup");
        var thisWeek = Today;
        var nextWeek = Today.AddDays(7);

        var keyThisWeek = Assert.Single(HomeNudges.KeysFor([nudge], thisWeek));
        var keyNextWeek = Assert.Single(HomeNudges.KeysFor([nudge], nextWeek));

        Assert.Equal("2026-11-16|NoRecentBackup|", keyThisWeek);
        Assert.Equal("2026-11-23|NoRecentBackup|", keyNextWeek);
        Assert.NotEqual(keyThisWeek, keyNextWeek);
    }

    [Fact]
    public void Key_differs_between_subjects_and_handles_session_id_area_and_empty()
    {
        var nSession = new Nudge(NudgeKind.MissedSession, "Missed Lower", SessionId: "s-mon");
        var nArea = new Nudge(NudgeKind.RepeatedPain, "Shoulder pain", Area: BodyArea.Shoulder);
        var nNone = new Nudge(NudgeKind.OneShortOfTarget, "One short");

        var keys = HomeNudges.KeysFor([nSession, nArea, nNone], Today);

        Assert.Equal("2026-11-16|MissedSession|s-mon", keys[0]);
        Assert.Equal("2026-11-16|RepeatedPain|Shoulder", keys[1]);
        Assert.Equal("2026-11-16|OneShortOfTarget|", keys[2]);
    }

    [Fact]
    public void Action_mapping_for_all_four_kinds()
    {
        var nMissed = new Nudge(NudgeKind.MissedSession, "Missed", SessionId: "s-mon");
        var nShort = new Nudge(NudgeKind.OneShortOfTarget, "Short");
        var nBackup = new Nudge(NudgeKind.NoRecentBackup, "Backup");
        var nPain = new Nudge(NudgeKind.RepeatedPain, "Pain", Area: BodyArea.Knee);

        var selected = HomeNudges.Select([nMissed, nShort, nBackup, nPain], new HashSet<string>(), Today);

        // Select first 2: Missed, Short
        Assert.Equal(NudgeAction.MakeUp, selected[0].Action);
        Assert.Equal(NudgeAction.DifferentSession, selected[1].Action);

        // Select remaining 2 by dismissing first 2
        var dismissed = selected.Select(s => s.Key).ToHashSet();
        var selected2 = HomeNudges.Select([nMissed, nShort, nBackup, nPain], dismissed, Today);

        Assert.Equal(NudgeAction.BackUp, selected2[0].Action);
        Assert.Equal(NudgeAction.None, selected2[1].Action);
    }

    [Fact]
    public void KeysFor_returns_key_for_each_nudge_in_order()
    {
        var n1 = new Nudge(NudgeKind.MissedSession, "Missed", SessionId: "s1");
        var n2 = new Nudge(NudgeKind.NoRecentBackup, "Backup");

        var keys = HomeNudges.KeysFor([n1, n2], Today);

        Assert.Equal(2, keys.Count);
        Assert.Equal("2026-11-16|MissedSession|s1", keys[0]);
        Assert.Equal("2026-11-16|NoRecentBackup|", keys[1]);
    }
}
