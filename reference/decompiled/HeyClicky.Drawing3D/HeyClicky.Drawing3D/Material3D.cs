using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json;

namespace HeyClicky.Drawing3D;

public struct Material3D
{
	public Vector4 Color;

	public float Roughness;

	public float Metalness;

	public Vector3 Emissive;

	public float Opacity;

	public bool IsDoubleSided;

	public bool IsFlatShaded;

	public bool UsesVertexColors;

	public Vector4[]? Bands;

	public float Clearcoat;

	public float ClearcoatRoughness;

	public float Sheen;

	public bool IsGlass;

	public Vector4 CutPlane;

	public bool CutFacesViewer;

	private static readonly float DefaultGrey = Math3D.LinearFromSRGB(13f / 15f);

	public readonly bool IsTransparent => Opacity < 1f;

	public readonly bool IsCut
	{
		get
		{
			if (CutPlane.X == 0f && CutPlane.Y == 0f)
			{
				return CutPlane.Z != 0f;
			}
			return true;
		}
	}

	public readonly int BandCount
	{
		get
		{
			Vector4[]? bands = Bands;
			if (bands == null)
			{
				return 0;
			}
			return bands.Length;
		}
	}

	public static Material3D Default()
	{
		Material3D result = default(Material3D);
		result.Color = new Vector4(DefaultGrey, DefaultGrey, DefaultGrey, 1f);
		result.Roughness = 0.5f;
		result.Metalness = 0.05f;
		result.Opacity = 1f;
		result.ClearcoatRoughness = 0.12f;
		return result;
	}

	public static Material3D Studio(JsonElement dictionary)
	{
		Material3D result = Default();
		string text = dictionary.Str("mat") ?? "gloss";
		Vector4? tint = Math3D.LinearColor(dictionary.Str("tint"));
		switch (text)
		{
		case "steel":
			result.Color = Tinted("#C2C8CF");
			result.Metalness = 0.95f;
			result.Roughness = 0.24f;
			break;
		case "aluminum":
		case "alu":
		case "aluminium":
			result.Color = Tinted("#D3D5D6");
			result.Metalness = 0.55f;
			result.Roughness = 0.55f;
			break;
		case "titanium":
			result.Color = Tinted("#8E99A8");
			result.Metalness = 0.85f;
			result.Roughness = 0.34f;
			break;
		case "chrome":
			result.Color = Tinted("#EEF2F6");
			result.Metalness = 1f;
			result.Roughness = 0.08f;
			break;
		case "brass":
			result.Color = Tinted("#D2A64C");
			result.Metalness = 0.95f;
			result.Roughness = 0.3f;
			break;
		case "copper":
			result.Color = Tinted("#CC7F55");
			result.Metalness = 0.95f;
			result.Roughness = 0.3f;
			break;
		case "gold":
			result.Color = Tinted("#F2C14E");
			result.Metalness = 1f;
			result.Roughness = 0.22f;
			break;
		case "iron":
			result.Color = Tinted("#5E636B");
			result.Metalness = 0.45f;
			result.Roughness = 0.7f;
			break;
		case "black":
			result.Color = Tinted("#33373D");
			result.Metalness = 0.1f;
			result.Roughness = 0.4f;
			result.Clearcoat = 0.4f;
			break;
		case "rubber":
			result.Color = Tinted("#34383D");
			result.Metalness = 0f;
			result.Roughness = 0.85f;
			result.Sheen = 0.3f;
			break;
		case "glass":
			result.Color = Tinted("#DCEBFA");
			result.Metalness = 0f;
			result.Roughness = 0.06f;
			result.Clearcoat = 1f;
			result.Opacity = 0.28f;
			result.IsGlass = true;
			break;
		case "water":
			result.Color = Tinted("#3F7390");
			result.Metalness = 0f;
			result.Roughness = 0.05f;
			result.Clearcoat = 1f;
			result.Opacity = 0.72f;
			result.IsGlass = true;
			break;
		case "wood":
			result.Color = Tinted("#C99A68");
			result.Metalness = 0f;
			result.Roughness = 0.7f;
			break;
		case "ceramic":
			result.Color = Tinted("#F5F0E6");
			result.Metalness = 0f;
			result.Roughness = 0.25f;
			result.Clearcoat = 0.6f;
			break;
		case "paper":
			result.Color = Tinted("#FAF7F0");
			result.Metalness = 0f;
			result.Roughness = 0.95f;
			break;
		case "paint":
			result.Color = Tinted("#5AA8F5");
			result.Metalness = 0.05f;
			result.Roughness = 0.4f;
			result.Clearcoat = 0.5f;
			break;
		case "plastic":
			result.Color = Tinted("#5AA8F5");
			result.Metalness = 0f;
			result.Roughness = 0.5f;
			result.Clearcoat = 0.2f;
			break;
		case "matte":
		case "soft":
			result.Color = Tinted("#5AA8F5");
			result.Metalness = 0f;
			result.Roughness = 0.85f;
			result.Sheen = 0.35f;
			break;
		case "metal":
			result.Color = Tinted("#C9D1DC");
			result.Metalness = 0.65f;
			result.Roughness = 0.3f;
			result.Clearcoat = 0.3f;
			break;
		case "glow":
			result.Color = Tinted("#FF9A3C");
			result.Metalness = 0f;
			result.Roughness = 0.4f;
			result.Emissive = new Vector3(result.Color.X, result.Color.Y, result.Color.Z) * 1.2f;
			break;
		default:
			result.Color = Tinted("#B9BEC4");
			result.Metalness = 0f;
			result.Roughness = 0.42f;
			result.Clearcoat = 0.3f;
			result.ClearcoatRoughness = 0.25f;
			break;
		}
		Vector4? vector = Math3D.LinearColor(dictionary.Str("color"));
		if (vector.HasValue)
		{
			Vector4 valueOrDefault = vector.GetValueOrDefault();
			result.Color = new Vector4(valueOrDefault.X, valueOrDefault.Y, valueOrDefault.Z, 1f);
			if (text == "glow")
			{
				result.Emissive = new Vector3(valueOrDefault.X, valueOrDefault.Y, valueOrDefault.Z) * 1.2f;
			}
		}
		float? num = dictionary.Number("roughness");
		if (num.HasValue)
		{
			float valueOrDefault2 = num.GetValueOrDefault();
			result.Roughness = Math.Clamp(valueOrDefault2, 0f, 1f);
		}
		num = dictionary.Number("metalness");
		if (num.HasValue)
		{
			float valueOrDefault3 = num.GetValueOrDefault();
			result.Metalness = Math.Clamp(valueOrDefault3, 0f, 1f);
		}
		num = dictionary.Number("opacity");
		if (num.HasValue)
		{
			float valueOrDefault4 = num.GetValueOrDefault();
			if (valueOrDefault4 < 1f)
			{
				result.Opacity = MathF.Max(0f, valueOrDefault4);
			}
		}
		bool flag = dictionary.IsTrue("glow");
		Vector4? vector2 = Math3D.LinearColor(dictionary.Str("emissive")) ?? (flag ? new Vector4?(new Vector4(result.Color.X, result.Color.Y, result.Color.Z, 1f)) : ((Vector4?)null));
		if (vector2.HasValue)
		{
			Vector4 valueOrDefault5 = vector2.GetValueOrDefault();
			float num2 = dictionary.Number("emissiveIntensity") ?? (flag ? 0.9f : 1f);
			result.Emissive = new Vector3(valueOrDefault5.X, valueOrDefault5.Y, valueOrDefault5.Z) * num2;
		}
		result.IsDoubleSided = dictionary.IsTrue("double") || result.IsGlass;
		result.IsFlatShaded = dictionary.IsTrue("flat");
		Vector4[] array = BandColors(dictionary);
		if (array != null)
		{
			result.Bands = array;
			result.Color = new Vector4(1f, 1f, 1f, result.Color.W);
		}
		if (dictionary.Has("cut", out var value2) && value2.ValueKind == JsonValueKind.Array && value2.GetArrayLength() == 4)
		{
			float[] array2 = new float[4];
			bool flag2 = true;
			for (int i = 0; i < 4; i++)
			{
				num = J.Number(value2[i]);
				if (num.HasValue)
				{
					float valueOrDefault6 = num.GetValueOrDefault();
					array2[i] = valueOrDefault6;
				}
				else
				{
					flag2 = false;
				}
			}
			if (flag2)
			{
				Vector3 value3 = new Vector3(array2[0], array2[1], array2[2]);
				if (value3.Length() > 0.001f)
				{
					result.CutPlane = new Vector4(Vector3.Normalize(value3), array2[3]);
				}
				result.CutFacesViewer = result.IsCut && dictionary.IsTrue("cutFacing");
			}
		}
		return result;
		static Vector4 Hex(string value)
		{
			return Math3D.LinearColor(value) ?? new Vector4(0.8f, 0.8f, 0.8f, 1f);
		}
		Vector4 Tinted(string fallback)
		{
			return tint ?? Hex(fallback);
		}
	}

	public static Material3D Plain(JsonElement dictionary)
	{
		Material3D result = Default();
		Vector4? vector = Math3D.LinearColor(dictionary.Str("color"));
		if (vector.HasValue)
		{
			Vector4 valueOrDefault = vector.GetValueOrDefault();
			result.Color = new Vector4(valueOrDefault.X, valueOrDefault.Y, valueOrDefault.Z, 1f);
		}
		float? num = dictionary.Number("roughness");
		if (num.HasValue)
		{
			float valueOrDefault2 = num.GetValueOrDefault();
			result.Roughness = valueOrDefault2;
		}
		num = dictionary.Number("metalness");
		if (num.HasValue)
		{
			float valueOrDefault3 = num.GetValueOrDefault();
			result.Metalness = valueOrDefault3;
		}
		bool flag = dictionary.IsTrue("glow");
		Vector4? vector2 = Math3D.LinearColor(dictionary.Str("emissive")) ?? (flag ? Math3D.LinearColor(dictionary.Str("color") ?? "#ffffff") : ((Vector4?)null));
		if (vector2.HasValue)
		{
			Vector4 valueOrDefault4 = vector2.GetValueOrDefault();
			float num2 = dictionary.Number("emissiveIntensity") ?? (flag ? 0.8f : 1f);
			result.Emissive = new Vector3(valueOrDefault4.X, valueOrDefault4.Y, valueOrDefault4.Z) * num2;
		}
		num = dictionary.Number("opacity");
		if (num.HasValue)
		{
			float valueOrDefault5 = num.GetValueOrDefault();
			if (valueOrDefault5 < 1f)
			{
				result.Opacity = MathF.Max(0f, valueOrDefault5);
			}
		}
		result.IsDoubleSided = dictionary.IsTrue("double");
		result.IsFlatShaded = dictionary.IsTrue("flat");
		if (dictionary.Strings("colors").Count > 0)
		{
			result.UsesVertexColors = true;
			result.Color = Vector4.One;
		}
		Vector4[] array = BandColors(dictionary);
		if (array != null)
		{
			result.Bands = array;
			result.Color = Vector4.One;
		}
		return result;
	}

	private static Vector4[]? BandColors(JsonElement dictionary)
	{
		List<Vector4> list = new List<Vector4>();
		foreach (string item in dictionary.Strings("bands"))
		{
			Vector4? vector = Math3D.LinearColor(item);
			if (vector.HasValue)
			{
				Vector4 valueOrDefault = vector.GetValueOrDefault();
				list.Add(new Vector4(valueOrDefault.X, valueOrDefault.Y, valueOrDefault.Z, 1f));
				if (list.Count == 16)
				{
					break;
				}
			}
		}
		if (list.Count <= 0)
		{
			return null;
		}
		return list.ToArray();
	}
}
