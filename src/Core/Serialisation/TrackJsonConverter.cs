using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Model;

namespace Core.Serialisation;

/// <summary>
/// Writes a <see cref="Track"/> in format version 2's flat JSON shape (<c>cadence</c> beside the cadence-specific fields).
/// Reading is hand-written via <see cref="FieldReader"/> so errors report exact paths.
/// </summary>
internal sealed class TrackJsonConverter : JsonConverter<Track>
{
    public override Track Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw new NotSupportedException("Tracks are read through FieldReader.");

    public override void Write(Utf8JsonWriter writer, Track value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("id", value.Id);
        writer.WriteString("name", value.Name);
        switch (value.Rhythm)
        {
            case TrackRhythm.Weekly weekly:
                writer.WriteString("cadence", "weekly");
                writer.WriteNumber("weeklyTarget", weekly.WeeklyTarget);
                writer.WriteNumber("streakMinWorkoutsPerWeek", weekly.StreakMinWorkoutsPerWeek);
                writer.WriteBoolean("streakCountsCustomWorkouts", weekly.StreakCountsCustomWorkouts);
                break;
            case TrackRhythm.Daily daily:
                writer.WriteString("cadence", "daily");
                writer.WriteNumber("dayDonePercent", daily.DayDonePercent);
                break;
            default:
                throw new InvalidOperationException($"Unknown track rhythm: {value.Rhythm.GetType().Name}.");
        }

        writer.WritePropertyName("phases");
        JsonSerializer.Serialize(writer, value.Phases, ExchangeJsonContext.Default.IReadOnlyListPhase);
        writer.WriteEndObject();
    }
}
