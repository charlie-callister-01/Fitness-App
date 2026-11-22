using Core.Model;

namespace Core.Rules.Tracks;

/// <summary>Looks up a Plan's main Track, the Track a Session belongs to, and the Sessions on a Track.</summary>
public static class TrackRules
{
    /// <summary>The Plan's main Track (the first Track in the list).</summary>
    public static Track Main(Plan plan) => plan.Tracks[0];

    /// <summary>The Track that <paramref name="session"/> belongs to, or null if its TrackId is not in the Plan.</summary>
    public static Track? TrackOf(Plan plan, Session session) =>
        plan.Tracks.FirstOrDefault(t => t.Id == session.TrackId);

    /// <summary>The Sessions belonging to <paramref name="track"/>, in Plan order.</summary>
    public static IReadOnlyList<Session> SessionsOf(Plan plan, Track track) =>
        [.. plan.Sessions.Where(s => s.TrackId == track.Id)];
}
