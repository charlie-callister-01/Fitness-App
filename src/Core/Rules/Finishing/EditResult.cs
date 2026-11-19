using Core.Model;

namespace Core.Rules.Finishing;

/// <summary>The result of a Workout change that can be refused: the changed Workout, or a plain-language reason it was not allowed.</summary>
public sealed record EditResult(Workout? Workout, string? Refusal)
{
    public bool IsRefused => Refusal is not null;

    public static EditResult Changed(Workout workout) => new(workout, null);

    public static EditResult Refused(string reason) => new(null, reason);
}
