using System;
using System.Collections.Generic;
using System.Globalization;
using Windows.UI;

namespace HeyClicky.Drawing.Engine;

internal static class CssColor
{
	public readonly record struct Rgba(double Red, double Green, double Blue, double Alpha)
	{
		public Color ToColor(double extraOpacity = 1.0)
		{
			return Color.FromArgb(Channel(Alpha * Math.Clamp(extraOpacity, 0.0, 1.0)), Channel(Red), Channel(Green), Channel(Blue));
			static byte Channel(double value)
			{
				return (byte)Math.Round(Math.Clamp(value, 0.0, 1.0) * 255.0);
			}
		}
	}

	private static readonly Dictionary<string, Rgba?> Cache = new Dictionary<string, Rgba?>(StringComparer.Ordinal);

	private static readonly Dictionary<string, string> NamedColors = new Dictionary<string, string>
	{
		["aliceblue"] = "f0f8ff",
		["antiquewhite"] = "faebd7",
		["aqua"] = "00ffff",
		["aquamarine"] = "7fffd4",
		["azure"] = "f0ffff",
		["beige"] = "f5f5dc",
		["bisque"] = "ffe4c4",
		["black"] = "000000",
		["blanchedalmond"] = "ffebcd",
		["blue"] = "0000ff",
		["blueviolet"] = "8a2be2",
		["brown"] = "a52a2a",
		["burlywood"] = "deb887",
		["cadetblue"] = "5f9ea0",
		["chartreuse"] = "7fff00",
		["chocolate"] = "d2691e",
		["coral"] = "ff7f50",
		["cornflowerblue"] = "6495ed",
		["cornsilk"] = "fff8dc",
		["crimson"] = "dc143c",
		["cyan"] = "00ffff",
		["darkblue"] = "00008b",
		["darkcyan"] = "008b8b",
		["darkgoldenrod"] = "b8860b",
		["darkgray"] = "a9a9a9",
		["darkgreen"] = "006400",
		["darkgrey"] = "a9a9a9",
		["darkkhaki"] = "bdb76b",
		["darkmagenta"] = "8b008b",
		["darkolivegreen"] = "556b2f",
		["darkorange"] = "ff8c00",
		["darkorchid"] = "9932cc",
		["darkred"] = "8b0000",
		["darksalmon"] = "e9967a",
		["darkseagreen"] = "8fbc8f",
		["darkslateblue"] = "483d8b",
		["darkslategray"] = "2f4f4f",
		["darkslategrey"] = "2f4f4f",
		["darkturquoise"] = "00ced1",
		["darkviolet"] = "9400d3",
		["deeppink"] = "ff1493",
		["deepskyblue"] = "00bfff",
		["dimgray"] = "696969",
		["dimgrey"] = "696969",
		["dodgerblue"] = "1e90ff",
		["firebrick"] = "b22222",
		["floralwhite"] = "fffaf0",
		["forestgreen"] = "228b22",
		["fuchsia"] = "ff00ff",
		["gainsboro"] = "dcdcdc",
		["ghostwhite"] = "f8f8ff",
		["gold"] = "ffd700",
		["goldenrod"] = "daa520",
		["gray"] = "808080",
		["green"] = "008000",
		["greenyellow"] = "adff2f",
		["grey"] = "808080",
		["honeydew"] = "f0fff0",
		["hotpink"] = "ff69b4",
		["indianred"] = "cd5c5c",
		["indigo"] = "4b0082",
		["ivory"] = "fffff0",
		["khaki"] = "f0e68c",
		["lavender"] = "e6e6fa",
		["lavenderblush"] = "fff0f5",
		["lawngreen"] = "7cfc00",
		["lemonchiffon"] = "fffacd",
		["lightblue"] = "add8e6",
		["lightcoral"] = "f08080",
		["lightcyan"] = "e0ffff",
		["lightgoldenrodyellow"] = "fafad2",
		["lightgray"] = "d3d3d3",
		["lightgreen"] = "90ee90",
		["lightgrey"] = "d3d3d3",
		["lightpink"] = "ffb6c1",
		["lightsalmon"] = "ffa07a",
		["lightseagreen"] = "20b2aa",
		["lightskyblue"] = "87cefa",
		["lightslategray"] = "778899",
		["lightslategrey"] = "778899",
		["lightsteelblue"] = "b0c4de",
		["lightyellow"] = "ffffe0",
		["lime"] = "00ff00",
		["limegreen"] = "32cd32",
		["linen"] = "faf0e6",
		["magenta"] = "ff00ff",
		["maroon"] = "800000",
		["mediumaquamarine"] = "66cdaa",
		["mediumblue"] = "0000cd",
		["mediumorchid"] = "ba55d3",
		["mediumpurple"] = "9370db",
		["mediumseagreen"] = "3cb371",
		["mediumslateblue"] = "7b68ee",
		["mediumspringgreen"] = "00fa9a",
		["mediumturquoise"] = "48d1cc",
		["mediumvioletred"] = "c71585",
		["midnightblue"] = "191970",
		["mintcream"] = "f5fffa",
		["mistyrose"] = "ffe4e1",
		["moccasin"] = "ffe4b5",
		["navajowhite"] = "ffdead",
		["navy"] = "000080",
		["oldlace"] = "fdf5e6",
		["olive"] = "808000",
		["olivedrab"] = "6b8e23",
		["orange"] = "ffa500",
		["orangered"] = "ff4500",
		["orchid"] = "da70d6",
		["palegoldenrod"] = "eee8aa",
		["palegreen"] = "98fb98",
		["paleturquoise"] = "afeeee",
		["palevioletred"] = "db7093",
		["papayawhip"] = "ffefd5",
		["peachpuff"] = "ffdab9",
		["peru"] = "cd853f",
		["pink"] = "ffc0cb",
		["plum"] = "dda0dd",
		["powderblue"] = "b0e0e6",
		["purple"] = "800080",
		["rebeccapurple"] = "663399",
		["red"] = "ff0000",
		["rosybrown"] = "bc8f8f",
		["royalblue"] = "4169e1",
		["saddlebrown"] = "8b4513",
		["salmon"] = "fa8072",
		["sandybrown"] = "f4a460",
		["seagreen"] = "2e8b57",
		["seashell"] = "fff5ee",
		["sienna"] = "a0522d",
		["silver"] = "c0c0c0",
		["skyblue"] = "87ceeb",
		["slateblue"] = "6a5acd",
		["slategray"] = "708090",
		["slategrey"] = "708090",
		["snow"] = "fffafa",
		["springgreen"] = "00ff7f",
		["steelblue"] = "4682b4",
		["tan"] = "d2b48c",
		["teal"] = "008080",
		["thistle"] = "d8bfd8",
		["tomato"] = "ff6347",
		["turquoise"] = "40e0d0",
		["violet"] = "ee82ee",
		["wheat"] = "f5deb3",
		["white"] = "ffffff",
		["whitesmoke"] = "f5f5f5",
		["yellow"] = "ffff00",
		["yellowgreen"] = "9acd32"
	};

	public static Rgba? Parse(string? rawValue, bool strict = false)
	{
		if (rawValue == null)
		{
			return null;
		}
		if (!strict && Cache.TryGetValue(rawValue, out var value))
		{
			return value;
		}
		Rgba? rgba = ParseUncached(rawValue, strict);
		if (!strict && Cache.Count < 4096)
		{
			Cache[rawValue] = rgba;
		}
		return rgba;
	}

	public static Color? Paint(string? value, string? opacity)
	{
		Rgba? rgba = Parse(value ?? "#000000");
		if (rgba.HasValue)
		{
			Rgba valueOrDefault = rgba.GetValueOrDefault();
			double extraOpacity = 1.0;
			if (opacity != null && double.TryParse(opacity, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
			{
				extraOpacity = result;
			}
			return valueOrDefault.ToColor(extraOpacity);
		}
		return null;
	}

	private static Rgba? ParseUncached(string rawValue, bool strict)
	{
		string text = rawValue.Trim().ToLowerInvariant();
		if (text.Length == 0 || text == "none")
		{
			return null;
		}
		if (text == "transparent")
		{
			return new Rgba(0.0, 0.0, 0.0, 0.0);
		}
		if (text.StartsWith('#'))
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
		if (NamedColors.TryGetValue(text, out string value))
		{
			return Hex(value);
		}
		if (strict)
		{
			return null;
		}
		return new Rgba(0.0, 0.0, 0.0, 1.0);
	}

	private static Rgba? Hex(ReadOnlySpan<char> digits)
	{
		Span<char> span = stackalloc char[2];
		switch (digits.Length)
		{
		case 3:
		case 4:
		{
			Span<double> span3 = stackalloc double[4];
			for (int j = 0; j < digits.Length; j++)
			{
				span[0] = (span[1] = digits[j]);
				if (!Component(span, out span3[j]))
				{
					return null;
				}
			}
			return new Rgba(span3[0], span3[1], span3[2], (digits.Length == 4) ? span3[3] : 1.0);
		}
		case 6:
		case 8:
		{
			Span<double> span2 = stackalloc double[4];
			for (int i = 0; i < digits.Length / 2; i++)
			{
				if (!Component(digits.Slice(i * 2, 2), out span2[i]))
				{
					return null;
				}
			}
			return new Rgba(span2[0], span2[1], span2[2], (digits.Length == 8) ? span2[3] : 1.0);
		}
		default:
			return null;
		}
		static bool Component(ReadOnlySpan<char> text, out double value)
		{
			byte result;
			bool result2 = byte.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
			value = (double)(int)result / 255.0;
			return result2;
		}
	}

	private static Rgba? Functional(string value, bool isHsl)
	{
		int num = value.IndexOf('(');
		int num2 = value.LastIndexOf(')');
		if (num < 0 || num2 < num)
		{
			return null;
		}
		int num3 = num + 1;
		string[] array = value.Substring(num3, num2 - num3).Replace('/', ' ').Replace(',', ' ')
			.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		if (array.Length < 3)
		{
			return null;
		}
		double alpha = ((array.Length >= 4) ? Math.Clamp(Number(array[3], 1.0) ?? 1.0, 0.0, 1.0) : 1.0);
		double q;
		double p;
		double? num4;
		if (isHsl)
		{
			num4 = Number(array[0], 360.0);
			if (num4.HasValue)
			{
				double valueOrDefault = num4.GetValueOrDefault();
				num4 = Number(array[1], 1.0);
				if (num4.HasValue)
				{
					double valueOrDefault2 = num4.GetValueOrDefault();
					num4 = Number(array[2], 1.0);
					if (num4.HasValue)
					{
						double valueOrDefault3 = num4.GetValueOrDefault();
						double num5 = (valueOrDefault % 360.0 + 360.0) % 360.0 / 360.0;
						double num6 = Math.Clamp(valueOrDefault2, 0.0, 1.0);
						double num7 = Math.Clamp(valueOrDefault3, 0.0, 1.0);
						q = ((num7 < 0.5) ? (num7 * (1.0 + num6)) : (num7 + num6 - num7 * num6));
						p = 2.0 * num7 - q;
						return new Rgba(Channel(num5 + 1.0 / 3.0), Channel(num5), Channel(num5 - 1.0 / 3.0), alpha);
					}
				}
			}
			return null;
		}
		num4 = Number(array[0], 255.0);
		if (num4.HasValue)
		{
			double valueOrDefault4 = num4.GetValueOrDefault();
			num4 = Number(array[1], 255.0);
			if (num4.HasValue)
			{
				double valueOrDefault5 = num4.GetValueOrDefault();
				num4 = Number(array[2], 255.0);
				if (num4.HasValue)
				{
					double valueOrDefault6 = num4.GetValueOrDefault();
					return new Rgba(Math.Clamp(valueOrDefault4 / 255.0, 0.0, 1.0), Math.Clamp(valueOrDefault5 / 255.0, 0.0, 1.0), Math.Clamp(valueOrDefault6 / 255.0, 0.0, 1.0), alpha);
				}
			}
		}
		return null;
		double Channel(double t)
		{
			if (t < 0.0)
			{
				t += 1.0;
			}
			if (t > 1.0)
			{
				t -= 1.0;
			}
			if (t < 1.0 / 6.0)
			{
				return p + (q - p) * 6.0 * t;
			}
			if (t < 0.5)
			{
				return q;
			}
			if (t < 2.0 / 3.0)
			{
				return p + (q - p) * (2.0 / 3.0 - t) * 6.0;
			}
			return p;
		}
		static double? Number(string text, double percentScale)
		{
			if (text.EndsWith('%'))
			{
				string text2 = text;
				if (!double.TryParse(text2.Substring(0, text2.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
				{
					return null;
				}
				return result / 100.0 * percentScale;
			}
			if (text.EndsWith("deg", StringComparison.Ordinal))
			{
				string text2 = text;
				if (!double.TryParse(text2.Substring(0, text2.Length - 3), NumberStyles.Float, CultureInfo.InvariantCulture, out var result2))
				{
					return null;
				}
				return result2;
			}
			if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var result3))
			{
				return null;
			}
			return result3;
		}
	}
}
