using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace HeyClicky.Core.Documents.Spreadsheet;

public static class SpreadsheetColors
{
	public static readonly IReadOnlyList<SpreadsheetColor> DefaultIndexedPalette = new string[64]
	{
		"000000", "FFFFFF", "FF0000", "00FF00", "0000FF", "FFFF00", "FF00FF", "00FFFF", "000000", "FFFFFF",
		"FF0000", "00FF00", "0000FF", "FFFF00", "FF00FF", "00FFFF", "800000", "008000", "000080", "808000",
		"800080", "008080", "C0C0C0", "808080", "9999FF", "993366", "FFFFCC", "CCFFFF", "660066", "FF8080",
		"0066CC", "CCCCFF", "000080", "FF00FF", "FFFF00", "00FFFF", "800080", "800000", "008080", "0000FF",
		"00CCFF", "CCFFFF", "CCFFCC", "FFFF99", "99CCFF", "FF99CC", "CC99FF", "FFCC99", "3366FF", "33CCCC",
		"99CC00", "FFCC00", "FF9900", "FF6600", "666699", "969696", "003366", "339966", "003300", "333300",
		"993300", "993366", "333399", "333333"
	}.Select((string h) => FromHex(h).Value).ToList();

	public static readonly SpreadsheetColor Black = new SpreadsheetColor(0.0, 0.0, 0.0);

	public static SpreadsheetColor? FromHex(string rawHex)
	{
		string text = SwiftText.TrimWhitespace(rawHex);
		if (text.StartsWith('#'))
		{
			string text2 = text;
			text = text2.Substring(1, text2.Length - 1);
		}
		if (text.Length == 8)
		{
			string text3 = text;
			int length = text3.Length;
			int num = length - 6;
			text = text3.Substring(num, length - num);
		}
		if (text.Length != 6 || !uint.TryParse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var result))
		{
			return null;
		}
		return new SpreadsheetColor((double)((result >> 16) & 0xFF) / 255.0, (double)((result >> 8) & 0xFF) / 255.0, (double)(result & 0xFF) / 255.0);
	}

	public static SpreadsheetColor? FromNode(SpreadsheetXmlNode? node, SpreadsheetThemeColors theme, IReadOnlyList<SpreadsheetColor> indexedPalette)
	{
		if (node == null)
		{
			return null;
		}
		if (node.Attribute("auto") == "1")
		{
			return null;
		}
		SpreadsheetColor? spreadsheetColor = null;
		string text = node.Attribute("rgb");
		if (text != null)
		{
			spreadsheetColor = FromHex(text);
		}
		else
		{
			int? num = SwiftText.ParseInt(node.Attribute("theme"));
			if (num.HasValue)
			{
				int valueOrDefault = num.GetValueOrDefault();
				spreadsheetColor = theme.Color(valueOrDefault);
			}
			else
			{
				num = SwiftText.ParseInt(node.Attribute("indexed"));
				if (num.HasValue)
				{
					int valueOrDefault2 = num.GetValueOrDefault();
					if (valueOrDefault2 == 64)
					{
						return null;
					}
					if (valueOrDefault2 == 65)
					{
						return null;
					}
					spreadsheetColor = ((valueOrDefault2 >= 0 && valueOrDefault2 < indexedPalette.Count) ? new SpreadsheetColor?(indexedPalette[valueOrDefault2]) : ((SpreadsheetColor?)null));
				}
			}
		}
		if (spreadsheetColor.HasValue)
		{
			SpreadsheetColor valueOrDefault3 = spreadsheetColor.GetValueOrDefault();
			double? num2 = SwiftText.ParseDouble(node.Attribute("tint"));
			if (num2.HasValue)
			{
				double valueOrDefault4 = num2.GetValueOrDefault();
				if (valueOrDefault4 != 0.0)
				{
					return ApplyingTint(valueOrDefault4, valueOrDefault3);
				}
			}
			return valueOrDefault3;
		}
		return null;
	}

	public static SpreadsheetColor ApplyingTint(double tint, SpreadsheetColor color)
	{
		double red = color.Red;
		double green = color.Green;
		double blue = color.Blue;
		double num = Math.Max(red, Math.Max(green, blue));
		double num2 = Math.Min(red, Math.Min(green, blue));
		double num3 = (num + num2) / 2.0;
		double num4 = 0.0;
		double num5 = 0.0;
		if (num != num2)
		{
			double num6 = num - num2;
			num5 = ((num3 > 0.5) ? (num6 / (2.0 - num - num2)) : (num6 / (num + num2)));
			num4 = ((num == red) ? ((green - blue) / num6 + (double)((green < blue) ? 6 : 0)) : ((num != green) ? ((red - green) / num6 + 4.0) : ((blue - red) / num6 + 2.0)));
			num4 /= 6.0;
		}
		num3 = ((tint < 0.0) ? (num3 * (1.0 + tint)) : (num3 * (1.0 - tint) + tint));
		if (num5 == 0.0)
		{
			return new SpreadsheetColor(num3, num3, num3);
		}
		double num7 = ((num3 < 0.5) ? (num3 * (1.0 + num5)) : (num3 + num5 - num3 * num5));
		double p2 = 2.0 * num3 - num7;
		return new SpreadsheetColor(Channel(p2, num7, num4 + 1.0 / 3.0), Channel(p2, num7, num4), Channel(p2, num7, num4 - 1.0 / 3.0));
		static double Channel(double p, double q, double t0)
		{
			double num8 = t0;
			if (num8 < 0.0)
			{
				num8 += 1.0;
			}
			if (num8 > 1.0)
			{
				num8 -= 1.0;
			}
			if (num8 < 1.0 / 6.0)
			{
				return p + (q - p) * 6.0 * num8;
			}
			if (num8 < 0.5)
			{
				return q;
			}
			if (num8 < 2.0 / 3.0)
			{
				return p + (q - p) * (2.0 / 3.0 - num8) * 6.0;
			}
			return p;
		}
	}
}
