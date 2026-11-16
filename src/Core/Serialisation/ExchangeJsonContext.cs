using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Model;

namespace Core.Serialisation;

/// <summary>Enums are written as camelCase strings and never read as numbers.</summary>
internal sealed class CamelCaseEnumConverter<T>() : JsonStringEnumConverter<T>(JsonNamingPolicy.CamelCase, allowIntegerValues: false)
    where T : struct, Enum;

/// <summary>Source-generated (trim-safe) JSON metadata used for writing. Reading is hand-written so errors can name exact paths.</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters =
    [
        typeof(CamelCaseEnumConverter<LogKind>),
        typeof(CamelCaseEnumConverter<WeightUnit>),
        typeof(CamelCaseEnumConverter<DistanceUnit>),
        typeof(CamelCaseEnumConverter<SessionType>),
        typeof(CamelCaseEnumConverter<DayOfWeek>),
    ])]
[JsonSerializable(typeof(ExchangeDocument))]
internal sealed partial class ExchangeJsonContext : JsonSerializerContext;
