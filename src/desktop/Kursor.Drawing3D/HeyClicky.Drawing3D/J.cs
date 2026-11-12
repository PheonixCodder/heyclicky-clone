using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text.Json;

namespace HeyClicky.Drawing3D;

internal static class J
{
	public static readonly JsonElement Empty = JsonDocument.Parse("{}").RootElement.Clone();

	public static bool Has(this JsonElement e, string name, out JsonElement value)
	{
		if (e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out value))
		{
			JsonValueKind valueKind = value.ValueKind;
			if (valueKind != JsonValueKind.Undefined && valueKind != JsonValueKind.Null)
			{
				return true;
			}
		}
		value = default(JsonElement);
		return false;
	}

	public static float? Number(this JsonElement e, string name)
	{
		if (!e.Has(name, out var value))
		{
			return null;
		}
		return Number(value);
	}

	public static float? Number(JsonElement v)
	{
		if (v.ValueKind == JsonValueKind.Number)
		{
			return (float)v.GetDouble();
		}
		if (v.ValueKind == JsonValueKind.True)
		{
			return 1f;
		}
		if (v.ValueKind == JsonValueKind.False)
		{
			return 0f;
		}
		double? num = NumericString(v);
		if (num.HasValue)
		{
			double valueOrDefault = num.GetValueOrDefault();
			return (float)valueOrDefault;
		}
		return null;
	}

	public static double? Double(this JsonElement e, string name)
	{
		if (!e.Has(name, out var value))
		{
			return null;
		}
		if (value.ValueKind != JsonValueKind.Number)
		{
			return NumericString(value);
		}
		return value.GetDouble();
	}

	private static double? NumericString(JsonElement v)
	{
		if (v.ValueKind != JsonValueKind.String || !double.TryParse(v.GetString()?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var result) || !double.IsFinite(result))
		{
			return null;
		}
		return result;
	}

	public static string? Str(this JsonElement e, string name)
	{
		if (!e.Has(name, out var value) || value.ValueKind != JsonValueKind.String)
		{
			return null;
		}
		return value.GetString();
	}

	public static bool? Bool(this JsonElement e, string name)
	{
		if (!e.Has(name, out var value))
		{
			return null;
		}
		if (value.ValueKind != JsonValueKind.True)
		{
			if (value.ValueKind != JsonValueKind.False)
			{
				return null;
			}
			return false;
		}
		return true;
	}

	public static bool IsTrue(this JsonElement e, string name)
	{
		return e.Bool(name) == true;
	}

	public static Vector3? Vec3(this JsonElement e, string name)
	{
		if (!e.Has(name, out var value))
		{
			return null;
		}
		return Vec3(value);
	}

	public static Vector3? Vec3(JsonElement v)
	{
		if (v.ValueKind != JsonValueKind.Array || v.GetArrayLength() < 3)
		{
			return null;
		}
		float? num = Number(v[0]);
		float? num2 = Number(v[1]);
		float? num3 = Number(v[2]);
		if (num.HasValue && num2.HasValue && num3.HasValue)
		{
			return new Vector3(num.Value, num2.Value, num3.Value);
		}
		return null;
	}

	public static Vector2? Vec2(JsonElement v)
	{
		if (v.ValueKind != JsonValueKind.Array || v.GetArrayLength() < 2)
		{
			return null;
		}
		float? num = Number(v[0]);
		float? num2 = Number(v[1]);
		if (num.HasValue && num2.HasValue)
		{
			return new Vector2(num.Value, num2.Value);
		}
		return null;
	}

	public static List<Vector3> Points3(this JsonElement e, string name)
	{
		List<Vector3> list = new List<Vector3>();
		if (e.Has(name, out var value) && value.ValueKind == JsonValueKind.Array)
		{
			foreach (JsonElement item in value.EnumerateArray())
			{
				Vector3? vector = Vec3(item);
				if (vector.HasValue)
				{
					Vector3 valueOrDefault = vector.GetValueOrDefault();
					list.Add(valueOrDefault);
				}
			}
		}
		return list;
	}

	public static List<Vector3> Points3(JsonElement v)
	{
		List<Vector3> list = new List<Vector3>();
		if (v.ValueKind == JsonValueKind.Array)
		{
			foreach (JsonElement item in v.EnumerateArray())
			{
				Vector3? vector = Vec3(item);
				if (vector.HasValue)
				{
					Vector3 valueOrDefault = vector.GetValueOrDefault();
					list.Add(valueOrDefault);
				}
			}
		}
		return list;
	}

	public static List<Vector2> Points2(this JsonElement e, string name)
	{
		List<Vector2> list = new List<Vector2>();
		if (e.Has(name, out var value) && value.ValueKind == JsonValueKind.Array)
		{
			foreach (JsonElement item in value.EnumerateArray())
			{
				Vector2? vector = Vec2(item);
				if (vector.HasValue)
				{
					Vector2 valueOrDefault = vector.GetValueOrDefault();
					list.Add(valueOrDefault);
				}
			}
		}
		return list;
	}

	public static List<string> Strings(this JsonElement e, string name)
	{
		List<string> list = new List<string>();
		if (e.Has(name, out var value) && value.ValueKind == JsonValueKind.Array)
		{
			foreach (JsonElement item in value.EnumerateArray())
			{
				if (item.ValueKind == JsonValueKind.String)
				{
					list.Add(item.GetString());
				}
			}
		}
		return list;
	}

	public static JsonElement Obj(this JsonElement e, string name)
	{
		if (!e.Has(name, out var value) || value.ValueKind != JsonValueKind.Object)
		{
			return Empty;
		}
		return value;
	}
}
