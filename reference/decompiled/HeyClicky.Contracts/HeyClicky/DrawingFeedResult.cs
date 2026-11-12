using System.Collections.Generic;
using System.Text.Json;

namespace HeyClicky;

public sealed record DrawingFeedResult(IReadOnlyList<string> Lines, IReadOnlyList<DrawingAction> Actions, int MarkCount, JsonElement Raw)
{
	public int InkedMarkCount
	{
		get
		{
			if (Raw.ValueKind != JsonValueKind.Object || !Raw.TryGetProperty("inked", out var value) || value.ValueKind != JsonValueKind.Number)
			{
				return MarkCount;
			}
			return value.GetInt32();
		}
	}

	public bool DrawsPicture
	{
		get
		{
			if (Raw.ValueKind == JsonValueKind.Object && Raw.TryGetProperty("picture", out var value))
			{
				return value.ValueKind == JsonValueKind.True;
			}
			return false;
		}
	}
}
