using Core.Model;

namespace Core.Serialisation;

/// <summary>
/// The status rules of a Pain check: a reported one needs an area; a "none" one can't carry an area or a severity.
/// Run only on a Pain check whose fields all read cleanly, so one bad field doesn't cause a second error.
/// </summary>
internal static class PainCheckRules
{
    public static void Check(FieldReader reader, PainCheck pain)
    {
        if (pain.Status == PainStatus.Reported)
        {
            if (pain.Areas.Count == 0)
            {
                reader.Report("areas", "missing_field", $"{reader.ChildPath("areas")} must name at least one body area when pain is reported.");
            }

            return;
        }

        if (pain.Areas.Count > 0 || pain.Severity is not null)
        {
            var field = pain.Areas.Count > 0 ? "areas" : "severity";
            reader.Report(field, "invalid_value", $"{reader.ChildPath(field)} can't be given when status is \"none\". Use status \"reported\" to record pain.");
        }
    }
}
