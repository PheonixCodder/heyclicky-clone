using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;

namespace HeyClicky.Drawing.Engine;

internal static class SvgValues
{
	private static readonly Dictionary<string, Matrix3x2?> TransformCache = new Dictionary<string, Matrix3x2?>(StringComparer.Ordinal);

	public static string? UrlReference(string? value)
	{
		if (value == null || !value.StartsWith("url(#", StringComparison.Ordinal) || !value.EndsWith(')'))
		{
			return null;
		}
		return value.Substring(5, value.Length - 1 - 5);
	}

	public static double Number(string? value)
	{
		if (value == null)
		{
			return 0.0;
		}
		ReadOnlySpan<char> readOnlySpan = value.AsSpan().Trim();
		if (readOnlySpan.EndsWith("px"))
		{
			readOnlySpan = readOnlySpan.Slice(0, readOnlySpan.Length - 2);
		}
		if (!double.TryParse(readOnlySpan, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
		{
			return 0.0;
		}
		return result;
	}

	public static bool TryNumber(string? value, out double number)
	{
		number = 0.0;
		if (value != null)
		{
			return double.TryParse(value.AsSpan().Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out number);
		}
		return false;
	}

	public static Vector2? TransformOrigin(string? value)
	{
		if (value == null)
		{
			return null;
		}
		string[] array = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		List<float> list = new List<float>();
		string[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			if (double.TryParse(array2[i].Replace("px", ""), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
			{
				list.Add((float)result);
			}
		}
		if (list.Count < 2)
		{
			return null;
		}
		return new Vector2(list[0], list[1]);
	}

	// [GeneratedRegex("(matrix|translate|scale|rotate|skewX|skewY)\\s*\\(([^)]*)\\)")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex TransformPattern()
	{
		return _003CRegexGenerator_g_003EF7574F012CF8662BB4428E0569B8990148C8CEFCEB6920EBA9F953742ED426801__TransformPattern_0.Instance;
	}

	public static Matrix3x2? Transform(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}
		if (TransformCache.TryGetValue(value, out var value2))
		{
			return value2;
		}
		Matrix3x2 matrix3x = Matrix3x2.Identity;
		foreach (Match item in TransformPattern().Matches(value))
		{
			string value3 = item.Groups[1].Value;
			List<float> list = new List<float>();
			string[] array = item.Groups[2].Value.Split(new char[4] { ' ', ',', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < array.Length; i++)
			{
				if (double.TryParse(array[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
				{
					list.Add((float)result);
				}
			}
			Matrix3x2 matrix3x2;
			switch (value3)
			{
			case "matrix":
				if (list.Count != 6)
				{
					continue;
				}
				matrix3x2 = new Matrix3x2(list[0], list[1], list[2], list[3], list[4], list[5]);
				break;
			case "translate":
				if (list.Count <= 0)
				{
					continue;
				}
				matrix3x2 = Matrix3x2.CreateTranslation(list[0], (list.Count > 1) ? list[1] : 0f);
				break;
			case "scale":
				if (list.Count <= 0)
				{
					continue;
				}
				matrix3x2 = Matrix3x2.CreateScale(list[0], (list.Count > 1) ? list[1] : list[0]);
				break;
			case "rotate":
			{
				if (list.Count <= 0)
				{
					continue;
				}
				float radians = list[0] * (float)Math.PI / 180f;
				matrix3x2 = ((list.Count >= 3) ? Matrix3x2.CreateRotation(radians, new Vector2(list[1], list[2])) : Matrix3x2.CreateRotation(radians));
				break;
			}
			case "skewX":
				if (list.Count <= 0)
				{
					continue;
				}
				matrix3x2 = new Matrix3x2(1f, 0f, MathF.Tan(list[0] * (float)Math.PI / 180f), 1f, 0f, 0f);
				break;
			case "skewY":
				if (list.Count <= 0)
				{
					continue;
				}
				matrix3x2 = new Matrix3x2(1f, MathF.Tan(list[0] * (float)Math.PI / 180f), 0f, 1f, 0f, 0f);
				break;
			default:
				continue;
			}
			matrix3x = matrix3x2 * matrix3x;
		}
		Matrix3x2? matrix3x3 = (matrix3x.IsIdentity ? ((Matrix3x2?)null) : new Matrix3x2?(matrix3x));
		if (TransformCache.Count < 4096)
		{
			TransformCache[value] = matrix3x3;
		}
		return matrix3x3;
	}

	public static Matrix4x4 To4x4(Matrix3x2 m)
	{
		return new Matrix4x4(m.M11, m.M12, 0f, 0f, m.M21, m.M22, 0f, 0f, 0f, 0f, 1f, 0f, m.M31, m.M32, 0f, 1f);
	}
}
