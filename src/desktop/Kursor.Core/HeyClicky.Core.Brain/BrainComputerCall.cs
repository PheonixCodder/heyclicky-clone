using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HeyClicky.Core.Brain;

public sealed record BrainComputerCall(string CallId, IReadOnlyList<JsonObject> Actions, IReadOnlyList<JsonObject> PendingSafetyChecks)
{
	public static BrainComputerCall? FromEvent(JsonElement call)
	{
		if (call.ValueKind != JsonValueKind.Object || !call.TryGetProperty("call_id", out var value) || value.ValueKind != JsonValueKind.String)
		{
			return null;
		}
		return new BrainComputerCall(value.GetString(), Objects(call, "actions"), Objects(call, "pending_safety_checks"));
		static List<JsonObject> Objects(JsonElement call, string name)
		{
			if (!call.TryGetProperty(name, out var value2) || value2.ValueKind != JsonValueKind.Array)
			{
				return new List<JsonObject>();
			}
			return (from e in value2.EnumerateArray()
				where e.ValueKind == JsonValueKind.Object
				select (JsonObject)JsonNode.Parse(e.GetRawText(), null)).ToList();
		}
	}
}
