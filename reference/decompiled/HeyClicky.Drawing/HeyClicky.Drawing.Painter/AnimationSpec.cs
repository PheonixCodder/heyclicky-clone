using System;
using System.Numerics;
using System.Text.Json;

namespace HeyClicky.Drawing.Painter;

internal readonly record struct AnimationSpec(string Name, double Duration, double BeginTime, Vector4 Timing)
{
	public double EndTime => BeginTime + Duration;

	public static readonly Vector4 CssEase = new Vector4(0.25f, 0.1f, 0.25f, 1f);

	public static AnimationSpec? From(JsonElement value, double now)
	{
		if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty("name", out var value2) || value2.ValueKind != JsonValueKind.String)
		{
			return null;
		}
		JsonElement value3;
		double val = ((value.TryGetProperty("dur", out value3) && value3.ValueKind == JsonValueKind.Number) ? value3.GetDouble() : 0.0);
		JsonElement value4;
		double num = ((value.TryGetProperty("delay", out value4) && value4.ValueKind == JsonValueKind.Number) ? value4.GetDouble() : 0.0);
		JsonElement value5;
		double num2 = ((value.TryGetProperty("at", out value5) && value5.ValueKind == JsonValueKind.Number) ? value5.GetDouble() : now);
		Vector4 timing = CssEase;
		if (value.TryGetProperty("fn", out var value6) && value6.ValueKind == JsonValueKind.Array && value6.GetArrayLength() == 4)
		{
			Span<float> span = stackalloc float[4];
			int num3 = 0;
			foreach (JsonElement item in value6.EnumerateArray())
			{
				span[num3++] = ((item.ValueKind == JsonValueKind.Number) ? ((float)item.GetDouble()) : 0f);
			}
			timing = new Vector4(span[0], span[1], span[2], span[3]);
		}
		return new AnimationSpec(value2.GetString(), Math.Max(0.001, val), Math.Max(num2 + num, now), timing);
	}
}
