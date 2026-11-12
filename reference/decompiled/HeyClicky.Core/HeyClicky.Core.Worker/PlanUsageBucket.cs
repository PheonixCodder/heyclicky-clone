using System.Text.Json.Serialization;

namespace HeyClicky.Core.Worker;

public sealed record PlanUsageBucket([property: JsonConverter(typeof(SaturatingInt32Converter))] int Used, [property: JsonConverter(typeof(SaturatingInt32Converter))] int Cap);
