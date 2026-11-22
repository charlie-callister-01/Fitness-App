using Core.Rules.Schedule;

namespace Core.Rules.Home;

/// <summary>The date, Phase label and greeting shown at the top of Home.</summary>
public sealed record HomeHeader(DateOnly Date, string PhaseLabel, string Greeting);

/// <summary>Everything Home shows, composed once. The page only reads it.</summary>
public sealed record HomeView(
    HomeHeader Header,
    TodayCard Today,
    IReadOnlyList<ScheduledSession> UndoneSessions,
    IReadOnlyList<WeekTile> Strip,
    WeekLine? Line,
    HomeOrbs? Orbs,
    IReadOnlyList<HomeNudge> Nudges,
    IReadOnlyList<string> NudgeKeys,
    IReadOnlyList<RecentPr> RecentPrs,
    WeightPanel Weight,
    bool ShowAlternatives,
    bool ShowLogPastWorkout,
    bool ShowNudges,
    IReadOnlyList<DailyTrackCard> DailyTracks,
    IReadOnlyList<ExtraWeekLine> ExtraWeekLines);
