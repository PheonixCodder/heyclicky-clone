using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Core.Documents.Spreadsheet;

public sealed class SpreadsheetThemeColors
{
	private readonly List<SpreadsheetColor> schemeColors;

	public SpreadsheetThemeColors(SpreadsheetXmlNode? root)
	{
		string[] source = new string[12]
		{
			"000000", "FFFFFF", "44546A", "E7E6E6", "4472C4", "ED7D31", "A5A5A5", "FFC000", "5B9BD5", "70AD47",
			"0563C1", "954F72"
		};
		schemeColors = source.Select((string h) => SpreadsheetColors.FromHex(h).Value).ToList();
		SpreadsheetXmlNode spreadsheetXmlNode = FindColorScheme(root);
		if (spreadsheetXmlNode == null)
		{
			return;
		}
		string[] array = new string[12]
		{
			"dk1", "lt1", "dk2", "lt2", "accent1", "accent2", "accent3", "accent4", "accent5", "accent6",
			"hlink", "folHlink"
		};
		for (int i = 0; i < array.Length; i++)
		{
			SpreadsheetXmlNode spreadsheetXmlNode2 = spreadsheetXmlNode.FirstChild(array[i]);
			if (spreadsheetXmlNode2 == null || spreadsheetXmlNode2.ChildNodes.Count == 0)
			{
				continue;
			}
			SpreadsheetXmlNode spreadsheetXmlNode3 = spreadsheetXmlNode2.ChildNodes[0];
			string text = ((spreadsheetXmlNode3.Name == "srgbClr") ? spreadsheetXmlNode3.Attribute("val") : null) ?? spreadsheetXmlNode3.Attribute("lastClr");
			if (text != null)
			{
				SpreadsheetColor? spreadsheetColor = SpreadsheetColors.FromHex(text);
				if (spreadsheetColor.HasValue)
				{
					SpreadsheetColor valueOrDefault = spreadsheetColor.GetValueOrDefault();
					schemeColors[i] = valueOrDefault;
				}
			}
		}
	}

	private static SpreadsheetXmlNode? FindColorScheme(SpreadsheetXmlNode? node)
	{
		if (node == null)
		{
			return null;
		}
		if (node.Name == "clrScheme")
		{
			return node;
		}
		foreach (SpreadsheetXmlNode childNode in node.ChildNodes)
		{
			SpreadsheetXmlNode spreadsheetXmlNode = FindColorScheme(childNode);
			if (spreadsheetXmlNode != null)
			{
				return spreadsheetXmlNode;
			}
		}
		return null;
	}

	public SpreadsheetColor? Color(int themeIndex)
	{
		int num = themeIndex switch
		{
			0 => 1, 
			1 => 0, 
			2 => 3, 
			3 => 2, 
			_ => themeIndex, 
		};
		if (num < 0 || num >= schemeColors.Count)
		{
			return null;
		}
		return schemeColors[num];
	}
}
