using Core.Model;

namespace Core.Serialisation;

/// <summary>The status rules of a Pain check: a reported one needs an area and a severity; a "none" one can't carry either.</summary>
internal static class PainCheckRules
{
    /// <param name="fieldsAreValid">False when a field already failed to read; the "no area" rule is skipped then so one bad field doesn't cause a second error.</param>
    public static void Check(FieldReader reader, PainStatus status, IReadOnlyList<BodyArea> areas, PainSeverity? severity, bool fieldsAreValid)
    {
        if (status == PainStatus.Reported)
        {
            if (areas.Count == 0 && fieldsAreValid)
            {
                reader.Report("areas", "missing_field", $"{reader.ChildPath("areas")} must name at least one body area when pain is reported.");
            }

            return;
        }

        if (areas.Count > 0 || severity is not null)
        {
            var field = areas.Count > 0 ? "areas" : "severity";
            reader.Report(field, "invalid_value", $"{reader.ChildPath(field)} can't be given when status is \"none\". Use status \"reported\" to record pain.");
        }
    }
}
