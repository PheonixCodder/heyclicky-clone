using System;
using System.Globalization;
using System.Text.Json.Nodes;

namespace HeyClicky.Core.Telemetry;

public sealed record AnalyticsEvent(string Event, string DistinctId, JsonObject Properties, DateTimeOffset Timestamp, Guid Uuid)
{
	public JsonObject ToPostHogJson()
	{
		return new JsonObject((JsonNodeOptions?)null)
		{
			["event"] = Event,
			["distinct_id"] = DistinctId,
			["properties"] = Properties.DeepClone(),
			["timestamp"] = Timestamp.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture),
			["uuid"] = Uuid.ToString()
		};
	}
}
