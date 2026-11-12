using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace HeyClicky.Core.Documents.Spreadsheet;

public static class SpreadsheetStylesReader
{
	private sealed class FontInfo
	{
		public bool IsBold;

		public bool IsItalic;

		public bool IsUnderlined;

		public bool IsStruckThrough;

		public double Size = 11.0;

		public SpreadsheetColor? Color;
	}

	private readonly record struct BorderInfo(SpreadsheetBorderEdge? Left, SpreadsheetBorderEdge? Right, SpreadsheetBorderEdge? Top, SpreadsheetBorderEdge? Bottom);

	public static List<SpreadsheetCellStyle> Styles(SpreadsheetXmlNode? root, SpreadsheetThemeColors theme)
	{
		if (root == null)
		{
			int num = 1;
			List<SpreadsheetCellStyle> list = new List<SpreadsheetCellStyle>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<SpreadsheetCellStyle> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = new SpreadsheetCellStyle();
			return list;
		}
		IReadOnlyList<SpreadsheetColor> indexedPalette = SpreadsheetColors.DefaultIndexedPalette;
		List<SpreadsheetXmlNode> list2 = root.FirstChild("colors")?.FirstChild("indexedColors")?.Children("rgbColor");
		if (list2 != null && list2.Count > 0)
		{
			indexedPalette = (from c in list2.Select(delegate(SpreadsheetXmlNode c)
				{
					string text = c.Attribute("rgb");
					return (text == null) ? ((SpreadsheetColor?)null) : SpreadsheetColors.FromHex(text);
				})
				where c.HasValue
				select c.Value).ToList();
		}
		Dictionary<int, string> numberFormats = new Dictionary<int, string>();
		foreach (SpreadsheetXmlNode item in root.FirstChild("numFmts")?.Children("numFmt") ?? new List<SpreadsheetXmlNode>())
		{
			int? num2 = SwiftText.ParseInt(item.Attribute("numFmtId"));
			if (num2.HasValue)
			{
				int valueOrDefault = num2.GetValueOrDefault();
				string text2 = item.Attribute("formatCode");
				if (text2 != null)
				{
					numberFormats[valueOrDefault] = text2;
				}
			}
		}
		List<FontInfo> fonts = (root.FirstChild("fonts")?.Children("font") ?? new List<SpreadsheetXmlNode>()).Select(delegate(SpreadsheetXmlNode font)
		{
			FontInfo obj = new FontInfo
			{
				IsBold = Flag(font.FirstChild("b")),
				IsItalic = Flag(font.FirstChild("i"))
			};
			SpreadsheetXmlNode spreadsheetXmlNode = font.FirstChild("u");
			obj.IsUnderlined = spreadsheetXmlNode != null && spreadsheetXmlNode.Attribute("val") != "none";
			obj.IsStruckThrough = Flag(font.FirstChild("strike"));
			FontInfo fontInfo = obj;
			double? num3 = SwiftText.ParseDouble(font.FirstChild("sz")?.Attribute("val"));
			if (num3.HasValue)
			{
				double valueOrDefault2 = num3.GetValueOrDefault();
				fontInfo.Size = valueOrDefault2;
			}
			fontInfo.Color = SpreadsheetColors.FromNode(font.FirstChild("color"), theme, indexedPalette);
			return fontInfo;
		}).ToList();
		List<SpreadsheetColor?> fills = (root.FirstChild("fills")?.Children("fill") ?? new List<SpreadsheetXmlNode>()).Select(delegate(SpreadsheetXmlNode fill)
		{
			SpreadsheetXmlNode spreadsheetXmlNode2 = fill.FirstChild("patternFill");
			if (spreadsheetXmlNode2 == null)
			{
				return SpreadsheetColors.FromNode((fill.FirstChild("gradientFill")?.Children("stop").FirstOrDefault())?.FirstChild("color"), theme, indexedPalette);
			}
			bool flag;
			switch (spreadsheetXmlNode2.Attribute("patternType") ?? "none")
			{
			case "none":
			case "gray125":
			case "gray0625":
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			return flag ? ((SpreadsheetColor?)null) : (SpreadsheetColors.FromNode(spreadsheetXmlNode2.FirstChild("fgColor"), theme, indexedPalette) ?? SpreadsheetColors.FromNode(spreadsheetXmlNode2.FirstChild("bgColor"), theme, indexedPalette));
		}).ToList();
		List<BorderInfo> borders = (root.FirstChild("borders")?.Children("border") ?? new List<SpreadsheetXmlNode>()).Select((SpreadsheetXmlNode border) => new BorderInfo(Edge("left") ?? Edge("start"), Edge("right") ?? Edge("end"), Edge("top"), Edge("bottom"))).ToList();
		List<SpreadsheetXmlNode> list3 = root.FirstChild("cellXfs")?.Children("xf") ?? new List<SpreadsheetXmlNode>();
		if (list3.Count == 0)
		{
			int index = 1;
			List<SpreadsheetCellStyle> list4 = new List<SpreadsheetCellStyle>(index);
			CollectionsMarshal.SetCount(list4, index);
			Span<SpreadsheetCellStyle> span = CollectionsMarshal.AsSpan(list4);
			int num = 0;
			span[num] = new SpreadsheetCellStyle();
			return list4;
		}
		return list3.Select(delegate(SpreadsheetXmlNode format)
		{
			SpreadsheetCellStyle result = new SpreadsheetCellStyle();
			int? num4 = SwiftText.ParseInt(format.Attribute("fontId"));
			if (num4.HasValue)
			{
				int valueOrDefault3 = num4.GetValueOrDefault();
				if (valueOrDefault3 >= 0 && valueOrDefault3 < fonts.Count)
				{
					FontInfo fontInfo2 = fonts[valueOrDefault3];
					result.IsBold = fontInfo2.IsBold;
					result.IsItalic = fontInfo2.IsItalic;
					result.IsUnderlined = fontInfo2.IsUnderlined;
					result.IsStruckThrough = fontInfo2.IsStruckThrough;
					result.FontSize = Math.Round(fontInfo2.Size * 1.17, MidpointRounding.ToEven);
					result.TextColor = fontInfo2.Color;
				}
			}
			num4 = SwiftText.ParseInt(format.Attribute("fillId"));
			if (num4.HasValue)
			{
				int valueOrDefault4 = num4.GetValueOrDefault();
				if (valueOrDefault4 >= 0 && valueOrDefault4 < fills.Count)
				{
					result.FillColor = fills[valueOrDefault4];
				}
			}
			num4 = SwiftText.ParseInt(format.Attribute("borderId"));
			if (num4.HasValue)
			{
				int valueOrDefault5 = num4.GetValueOrDefault();
				if (valueOrDefault5 >= 0 && valueOrDefault5 < borders.Count)
				{
					BorderInfo borderInfo = borders[valueOrDefault5];
					result.LeftBorder = borderInfo.Left;
					result.RightBorder = borderInfo.Right;
					result.TopBorder = borderInfo.Top;
					result.BottomBorder = borderInfo.Bottom;
				}
			}
			int valueOrDefault6 = SwiftText.ParseInt(format.Attribute("numFmtId")).GetValueOrDefault();
			result.NumberFormatCode = (numberFormats.TryGetValue(valueOrDefault6, out string value) ? value : (SpreadsheetNumberFormat.BuiltInFormatCode(valueOrDefault6) ?? "General"));
			SpreadsheetXmlNode spreadsheetXmlNode3 = format.FirstChild("alignment");
			if (spreadsheetXmlNode3 != null)
			{
				SpreadsheetHorizontalAlignment horizontalAlignment;
				switch (spreadsheetXmlNode3.Attribute("horizontal"))
				{
				case "fill":
				case "left":
				case "justify":
				case "distributed":
					horizontalAlignment = SpreadsheetHorizontalAlignment.Left;
					break;
				case "center":
				case "centerContinuous":
					horizontalAlignment = SpreadsheetHorizontalAlignment.Center;
					break;
				case "right":
					horizontalAlignment = SpreadsheetHorizontalAlignment.Right;
					break;
				default:
					horizontalAlignment = SpreadsheetHorizontalAlignment.General;
					break;
				}
				result.HorizontalAlignment = horizontalAlignment;
				SpreadsheetVerticalAlignment verticalAlignment;
				switch (spreadsheetXmlNode3.Attribute("vertical"))
				{
				case "top":
					verticalAlignment = SpreadsheetVerticalAlignment.Top;
					break;
				case "center":
				case "justify":
				case "distributed":
					verticalAlignment = SpreadsheetVerticalAlignment.Center;
					break;
				default:
					verticalAlignment = SpreadsheetVerticalAlignment.Bottom;
					break;
				}
				result.VerticalAlignment = verticalAlignment;
				string text3 = spreadsheetXmlNode3.Attribute("wrapText");
				bool wrapsText = ((text3 == "1" || text3 == "true") ? true : false);
				result.WrapsText = wrapsText;
				result.IndentLevel = SwiftText.ParseInt(spreadsheetXmlNode3.Attribute("indent")).GetValueOrDefault();
			}
			return result;
		}).ToList();
		SpreadsheetBorderEdge? Edge(string name)
		{
			SpreadsheetXmlNode spreadsheetXmlNode4 = P_1.border.FirstChild(name);
			if (spreadsheetXmlNode4 != null)
			{
				string text4 = spreadsheetXmlNode4.Attribute("style");
				if (text4 != null && !(text4 == "none"))
				{
					int num5;
					switch (text4)
					{
					case "medium":
					case "mediumDashed":
					case "slantDashDot":
					case "mediumDashDot":
					case "mediumDashDotDot":
						num5 = 2;
						break;
					case "double":
					case "thick":
						num5 = 3;
						break;
					default:
						num5 = 1;
						break;
					}
					double width = num5;
					SpreadsheetColor color = SpreadsheetColors.FromNode(spreadsheetXmlNode4.FirstChild("color"), theme, indexedPalette) ?? SpreadsheetColors.Black;
					return new SpreadsheetBorderEdge(width, color, text4.ToLowerInvariant().Contains("dash") || text4 == "dotted" || text4 == "hair");
				}
			}
			return null;
		}
	}

	private static bool Flag(SpreadsheetXmlNode? node)
	{
		if (node == null)
		{
			return false;
		}
		string text = node.Attribute("val");
		if (text == null)
		{
			return true;
		}
		if (text == "1" || text == "true")
		{
			return true;
		}
		return false;
	}

	public static bool IsDateCode(string code)
	{
		string text = code.ToLowerInvariant();
		if (text == "general")
		{
			return false;
		}
		string text2 = Regex.Replace(Regex.Replace(text, "\"[^\"]*\"", ""), "\\[[^\\]]*\\]", "");
		if (!text2.Contains('y') && !text2.Contains('d') && !text2.Contains('h'))
		{
			if (text2.Contains('m') && !text2.Contains('0'))
			{
				return !text2.Contains('#');
			}
			return false;
		}
		return true;
	}
}
