using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HeyClicky.Core.Ink;

public sealed class InkActionsSnapshot
{
	[JsonPropertyName("version")]
	public int Version { get; set; } = 1;

	[JsonPropertyName("actionsByThingID")]
	public Dictionary<string, List<InkSentActionFile>>? ActionsByThingId { get; set; }
}
