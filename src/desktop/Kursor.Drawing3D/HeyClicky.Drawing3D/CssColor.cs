using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace HeyClicky.Drawing3D;

internal static class CssColor
{
	private static readonly Dictionary<string, string> Named = BuildNamed();

	public static Vector4? Parse(string raw, bool strict = false)
	{
		string text = raw.Trim().ToLowerInvariant();
		if (text.Length == 0 || text == "none")
		{
			return null;
		}
		if (text == "transparent")
		{
			return Vector4.Zero;
		}
		if (text[0] == '#')
		{
			return Hex(text.AsSpan(1));
		}
		if (text.StartsWith("rgb", StringComparison.Ordinal))
		{
			return Functional(text, isHsl: false);
		}
		if (text.StartsWith("hsl", StringComparison.Ordinal))
		{
			return Functional(text, isHsl: true);
		}
		if (Named.TryGetValue(text, out string value))
		{
			return Hex(value);
		}
		if (!strict)
		{
			return new Vector4(0f, 0f, 0f, 1f);
		}
		return null;
	}

	private static Vector4? Hex(ReadOnlySpan<char> digits)
	{
		switch (digits.Length)
		{
		case 3:
		case 4:
		{
			Span<float> span2 = stackalloc float[4];
			Span<char> span3 = stackalloc char[2];
			for (int j = 0; j < digits.Length; j++)
			{
				span3[0] = digits[j];
				span3[1] = digits[j];
				if (!Component(span3, out span2[j]))
				{
					return null;
				}
			}
			return new Vector4(span2[0], span2[1], span2[2], (digits.Length == 4) ? span2[3] : 1f);
		}
		case 6:
		case 8:
		{
			Span<float> span = stackalloc float[4];
			for (int i = 0; i < digits.Length / 2; i++)
			{
				if (!Component(digits.Slice(i * 2, 2), out span[i]))
				{
					return null;
				}
			}
			return new Vector4(span[0], span[1], span[2], (digits.Length == 8) ? span[3] : 1f);
		}
		default:
			return null;
		}
		static bool Component(ReadOnlySpan<char> text, out float value)
		{
			byte result;
			bool result2 = byte.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
			value = (float)(int)result / 255f;
			return result2;
		}
	}

	private static Vector4? Functional(string value, bool isHsl)
	{
		int num = value.IndexOf('(');
		int num2 = value.LastIndexOf(')');
		if (num < 0 || num2 < num)
		{
			return null;
		}
		string[] array = value.Substring(num + 1, num2 - num - 1).Replace('/', ' ').Replace(',', ' ')
			.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		if (array.Length < 3)
		{
			return null;
		}
		float w = ((array.Length >= 4) ? Math.Clamp(Number(array[3], 1f) ?? 1f, 0f, 1f) : 1f);
		float q;
		float p;
		float? num3;
		if (isHsl)
		{
			num3 = Number(array[0], 360f);
			if (num3.HasValue)
			{
				float valueOrDefault = num3.GetValueOrDefault();
				num3 = Number(array[1], 1f);
				if (num3.HasValue)
				{
					float valueOrDefault2 = num3.GetValueOrDefault();
					num3 = Number(array[2], 1f);
					if (num3.HasValue)
					{
						float valueOrDefault3 = num3.GetValueOrDefault();
						float num4 = (valueOrDefault % 360f + 360f) % 360f / 360f;
						float num5 = Math.Clamp(valueOrDefault2, 0f, 1f);
						float num6 = Math.Clamp(valueOrDefault3, 0f, 1f);
						q = ((num6 < 0.5f) ? (num6 * (1f + num5)) : (num6 + num5 - num6 * num5));
						p = 2f * num6 - q;
						return new Vector4(Channel(num4 + 1f / 3f), Channel(num4), Channel(num4 - 1f / 3f), w);
					}
				}
			}
			return null;
		}
		num3 = Number(array[0], 255f);
		if (num3.HasValue)
		{
			float valueOrDefault4 = num3.GetValueOrDefault();
			num3 = Number(array[1], 255f);
			if (num3.HasValue)
			{
				float valueOrDefault5 = num3.GetValueOrDefault();
				num3 = Number(array[2], 255f);
				if (num3.HasValue)
				{
					float valueOrDefault6 = num3.GetValueOrDefault();
					return new Vector4(Math.Clamp(valueOrDefault4 / 255f, 0f, 1f), Math.Clamp(valueOrDefault5 / 255f, 0f, 1f), Math.Clamp(valueOrDefault6 / 255f, 0f, 1f), w);
				}
			}
		}
		return null;
		float Channel(float t)
		{
			if (t < 0f)
			{
				t += 1f;
			}
			if (t > 1f)
			{
				t -= 1f;
			}
			if (t < 1f / 6f)
			{
				return p + (q - p) * 6f * t;
			}
			if (t < 0.5f)
			{
				return q;
			}
			if (t < 2f / 3f)
			{
				return p + (q - p) * (2f / 3f - t) * 6f;
			}
			return p;
		}
		static float? Number(string text, float percentScale)
		{
			if (text.EndsWith('%'))
			{
				string text2 = text;
				if (!float.TryParse(text2.Substring(0, text2.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
				{
					return null;
				}
				return result / 100f * percentScale;
			}
			if (text.EndsWith("deg", StringComparison.Ordinal))
			{
				string text2 = text;
				if (!float.TryParse(text2.Substring(0, text2.Length - 3), NumberStyles.Float, CultureInfo.InvariantCulture, out var result2))
				{
					return null;
				}
				return result2;
			}
			if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var result3))
			{
				return null;
			}
			return result3;
		}
	}

	private static Dictionary<string, string> BuildNamed()
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
		string[] array = "aliceblue=f0f8ff antiquewhite=faebd7 aqua=00ffff aquamarine=7fffd4 azure=f0ffff beige=f5f5dc bisque=ffe4c4 black=000000 blanchedalmond=ffebcd blue=0000ff blueviolet=8a2be2 brown=a52a2a burlywood=deb887 cadetblue=5f9ea0 chartreuse=7fff00 chocolate=d2691e coral=ff7f50 cornflowerblue=6495ed cornsilk=fff8dc crimson=dc143c cyan=00ffff darkblue=00008b darkcyan=008b8b darkgoldenrod=b8860b darkgray=a9a9a9 darkgreen=006400 darkgrey=a9a9a9 darkkhaki=bdb76b darkmagenta=8b008b darkolivegreen=556b2f darkorange=ff8c00 darkorchid=9932cc darkred=8b0000 darksalmon=e9967a darkseagreen=8fbc8f darkslateblue=483d8b darkslategray=2f4f4f darkslategrey=2f4f4f darkturquoise=00ced1 darkviolet=9400d3 deeppink=ff1493 deepskyblue=00bfff dimgray=696969 dimgrey=696969 dodgerblue=1e90ff firebrick=b22222 floralwhite=fffaf0 forestgreen=228b22 fuchsia=ff00ff gainsboro=dcdcdc ghostwhite=f8f8ff gold=ffd700 goldenrod=daa520 gray=808080 green=008000 greenyellow=adff2f grey=808080 honeydew=f0fff0 hotpink=ff69b4 indianred=cd5c5c indigo=4b0082 ivory=fffff0 khaki=f0e68c lavender=e6e6fa lavenderblush=fff0f5 lawngreen=7cfc00 lemonchiffon=fffacd lightblue=add8e6 lightcoral=f08080 lightcyan=e0ffff lightgoldenrodyellow=fafad2 lightgray=d3d3d3 lightgreen=90ee90 lightgrey=d3d3d3 lightpink=ffb6c1 lightsalmon=ffa07a lightseagreen=20b2aa lightskyblue=87cefa lightslategray=778899 lightslategrey=778899 lightsteelblue=b0c4de lightyellow=ffffe0 lime=00ff00 limegreen=32cd32 linen=faf0e6 magenta=ff00ff maroon=800000 mediumaquamarine=66cdaa mediumblue=0000cd mediumorchid=ba55d3 mediumpurple=9370db mediumseagreen=3cb371 mediumslateblue=7b68ee mediumspringgreen=00fa9a mediumturquoise=48d1cc mediumvioletred=c71585 midnightblue=191970 mintcream=f5fffa mistyrose=ffe4e1 moccasin=ffe4b5 navajowhite=ffdead navy=000080 oldlace=fdf5e6 olive=808000 olivedrab=6b8e23 orange=ffa500 orangered=ff4500 orchid=da70d6 palegoldenrod=eee8aa palegreen=98fb98 paleturquoise=afeeee palevioletred=db7093 papayawhip=ffefd5 peachpuff=ffdab9 peru=cd853f pink=ffc0cb plum=dda0dd powderblue=b0e0e6 purple=800080 rebeccapurple=663399 red=ff0000 rosybrown=bc8f8f royalblue=4169e1 saddlebrown=8b4513 salmon=fa8072 sandybrown=f4a460 seagreen=2e8b57 seashell=fff5ee sienna=a0522d silver=c0c0c0 skyblue=87ceeb slateblue=6a5acd slategray=708090 slategrey=708090 snow=fffafa springgreen=00ff7f steelblue=4682b4 tan=d2b48c teal=008080 thistle=d8bfd8 tomato=ff6347 turquoise=40e0d0 violet=ee82ee wheat=f5deb3 white=ffffff whitesmoke=f5f5f5 yellow=ffff00 yellowgreen=9acd32".Split(' ', StringSplitOptions.RemoveEmptyEntries);
		foreach (string text in array)
		{
			int num = text.IndexOf('=');
			string key = text.Substring(0, num);
			string text2 = text;
			int num2 = num + 1;
			dictionary[key] = text2.Substring(num2, text2.Length - num2);
		}
		return dictionary;
	}
}
