using Core.Model;
using Core.Storage;

namespace Core.Exchange;

/// <summary>
/// The three ready-made prompts (analyse my training, write me a Plan, write Guides). Pure text: the owner copies the result into a chat tool.
/// The wording is a first draft for the owner to review; the facts in it (enum lists, worked examples, the Exercise library) come from the code.
/// Every prompt uses <c>\n</c> line endings, except that an Export is embedded exactly as it was produced.
/// </summary>
public static partial class PromptPack
{
    /// <summary>
    /// "Analyse my training": instructions, the Exercise library (so the IDs in the Export line up with names), then the Export text unchanged at the end.
    /// <paramref name="range"/> is the range the Export covers (null for all time); it is stated in the prompt so the LLM knows the period.
    /// </summary>
    public static string Analyse(DataSnapshot snapshot, ExportResult export, DateRange? range) => Lines(
        "You are a strength and conditioning coach. Below is an export of my training, weight and pain data from my fitness app.",
        "Analyse it and tell me what is going well, what is not, and what I should change next.",
        "",
        "My goal: [goal]",
        "Injuries or limits to respect: [injuries]",
        "",
        $"Period covered: {Describe(range)}.",
        "Units: weights are in kg, distances in km or m (the field name or label says which) and durations in seconds (JSON) or m:ss (Markdown).",
        "RPE is how hard an Exercise felt, 1 to 10. A Pain check is my answer to whether anything hurt in that Workout.",
        "Only use what is in the export. If something you need is missing, say so instead of guessing.",
        "",
        "Reply in plain text. Look at: progress on the main lifts, how consistent I was, my weight trend, and any pain that keeps coming back.",
        "",
        "My Exercise library, one Exercise per line (id, name, logKind and tags):",
        LibraryJson(snapshot.Exercises),
        "",
        "Export:",
        "") + export.Text;

    /// <summary>"Write me a Plan": the Plan format, a worked example and the current Exercise library, so the reply imports as a new Plan.</summary>
    public static string WritePlan(DataSnapshot snapshot) => Lines(
        "You are a strength and conditioning coach. Write me a training Plan for my fitness app, as one JSON file the app can import.",
        "",
        "About me and what I want:",
        "- Goal: [goal]",
        "- Days a week I can train: [days a week]",
        "- Equipment I have: [equipment]",
        "- Injuries or limits: [injuries]",
        "- Start date (yyyy-MM-dd, the Monday of week 1): [start date]",
        "",
        "Reply rules:",
        "- Please reply with one JSON object and nothing else: no text before or after it.",
        "- Please use these Exercise IDs for every Exercise that is in my library below, and the same name and logKind.",
        "- To add an Exercise that is not in my library, put it in `exercises` with a new lowercase-hyphen ID (like `kb-swing`).",
        "- If the file has an `exercises` list, it must list every Exercise the Plan uses: mine and the new ones.",
        "- Write exactly one Plan, with `isActive` set to true.",
        "- Do not write workouts, weighIns, settings, breaks, badges or guides.",
        "- Weights are in kg, distances in km or m, durations in seconds.",
        "",
        "Format:",
        PlanFormatReference(),
        "",
        "Worked example (a made-up Plan; yours will differ):",
        PlanExample,
        "",
        "My Exercise library, one Exercise per line (id, name, logKind and tags):",
        LibraryJson(snapshot.Exercises),
        "");

    /// <summary>
    /// "Write Guides" for the given Exercises. <paramref name="exerciseIds"/> null means the Exercises that have no Guide content yet;
    /// IDs that are not in the library are ignored.
    /// </summary>
    public static string WriteGuides(DataSnapshot snapshot, IEnumerable<string>? exerciseIds = null)
    {
        var chosen = exerciseIds is null
            ? snapshot.Exercises.Where(e => !HasGuideContent(snapshot, e.Id))
            : snapshot.Exercises.Where(e => exerciseIds.Contains(e.Id));

        return Lines(
            "You are a strength and conditioning coach. Write a Guide for each Exercise listed below, for my fitness app, as one JSON file the app can import.",
            "A Guide is the reference page for one Exercise: what it works, why it is in my Plan, how to do it, and what to watch out for.",
            "",
            "About me: [goal], [equipment], [injuries]",
            "",
            "Reply rules:",
            "- Please reply with one JSON object and nothing else: no text before or after it.",
            "- Please write one Guide for each Exercise below, using its exact ID as `exerciseId`. Do not add Guides for any other Exercise.",
            "- Do not add an `exercises` list: the Exercises are already in my app.",
            "- Do not invent personal notes: leave out `personalNotes`.",
            "- Keep each text short and plain. Use `videoUrl` only if you are sure of a real address that starts with http:// or https://.",
            "",
            "Format:",
            GuideFormatReference(),
            "",
            "Worked example (made-up content):",
            GuidesExample,
            "",
            "Write a Guide for each of these Exercises, one per line (id, name, logKind and tags):",
            LibraryJson(chosen),
            "");
    }

    private static bool HasGuideContent(DataSnapshot snapshot, string exerciseId) =>
        snapshot.Guides.Any(g => g.ExerciseId == exerciseId && GuidesImport.HasContent(g));

    private static string Describe(DateRange? range) =>
        range is null ? "all time" : $"{range.From:yyyy-MM-dd} to {range.To:yyyy-MM-dd} (both days included)";

    private static string Lines(params string[] lines) => string.Join('\n', lines);
}
