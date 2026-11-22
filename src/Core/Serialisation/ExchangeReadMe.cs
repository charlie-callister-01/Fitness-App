using System.Text.Json.Nodes;

namespace Core.Serialisation;

/// <summary>
/// The "how to read this" block written at the top of every exported document, so an LLM (or a person) handed the bare file understands it.
/// The reader accepts it and ignores it. Defined in <c>docs/data-spec.md</c> (Root document).
/// </summary>
internal static class ExchangeReadMe
{
    public const string Field = "readMe";

    public static JsonObject Build() => new()
    {
        ["about"] = "Fitness app data in the app's exchange format. Fields and rules: the data spec for the formatVersion given at the top of this document.",
        ["formatVersion"] = "The integer at the top of this document names the version of the format. Newer apps still read older versions.",
        ["units"] = "Weights in kg (fields ending Kg), cardio distance in km (distanceKm), carry distance in metres (distanceM), body measurements in cm (measurementCm), durations in seconds. Dates are yyyy-MM-dd. Timestamps are ISO 8601 with a time zone offset.",
        ["ids"] = "Every record has an ID that is unique among records of its kind and never changes: letters, digits, hyphens and underscores only. Names are labels and may change.",
        ["tracks"] = "Each Plan has one or more Tracks (cadence weekly or daily), with the main Track first and each Track holding its own rules and Phases; every Session names its trackId.",
        ["absentFields"] = "A missing optional field and an explicit null mean the same thing. A missing collection means that kind of data is not part of this document.",
    };
}
