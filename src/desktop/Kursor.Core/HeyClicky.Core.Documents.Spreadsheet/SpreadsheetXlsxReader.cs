using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace HeyClicky.Core.Documents.Spreadsheet;

public static class SpreadsheetXlsxReader
{
	public static SpreadsheetWorkbook Read(string path)
	{
		return Read(new SpreadsheetZipArchive(path));
	}

	public static SpreadsheetWorkbook Read(SpreadsheetZipArchive archive)
	{
		byte[] array = archive.Contents("xl/workbook.xml");
		SpreadsheetXmlNode spreadsheetXmlNode = ((array == null) ? null : SpreadsheetXmlNode.Parse(array));
		if (spreadsheetXmlNode == null)
		{
			throw new SpreadsheetReadException(SpreadsheetReadErrorKind.MissingPart, "xl/workbook.xml");
		}
		Dictionary<string, string> dictionary = Relationships(archive, "xl/workbook.xml");
		byte[] array2 = archive.Contents("xl/theme/theme1.xml");
		SpreadsheetThemeColors theme = new SpreadsheetThemeColors((array2 == null) ? null : SpreadsheetXmlNode.Parse(array2));
		byte[] array3 = archive.Contents("xl/styles.xml");
		List<SpreadsheetCellStyle> styles = SpreadsheetStylesReader.Styles((array3 == null) ? null : SpreadsheetXmlNode.Parse(array3), theme);
		byte[] array4 = archive.Contents("xl/sharedStrings.xml");
		List<string> sharedStrings = ((array4 == null) ? new List<string>() : SpreadsheetSharedStringsParser.Strings(array4));
		string text = spreadsheetXmlNode.FirstChild("workbookPr")?.Attribute("date1904") ?? "0";
		bool flag = ((text == "1" || text == "true") ? true : false);
		bool usesDate = flag;
		List<SpreadsheetSheet> list = new List<SpreadsheetSheet>();
		Dictionary<int, int> dictionary2 = new Dictionary<int, int>();
		List<SpreadsheetXmlNode> list2 = spreadsheetXmlNode.FirstChild("sheets")?.Children("sheet") ?? new List<SpreadsheetXmlNode>();
		for (int i = 0; i < list2.Count; i++)
		{
			SpreadsheetXmlNode spreadsheetXmlNode2 = list2[i];
			string text2 = spreadsheetXmlNode2.Attribute("state");
			if (text2 != null && text2 != "visible")
			{
				continue;
			}
			string text3 = spreadsheetXmlNode2.Attribute("r:id") ?? spreadsheetXmlNode2.Attribute("id");
			if (text3 != null && dictionary.TryGetValue(text3, out var value))
			{
				string text4 = ResolvedPartName(value, "xl");
				byte[] array5 = archive.Contents(text4);
				if (array5 != null)
				{
					Dictionary<string, string> relationships = Relationships(archive, text4);
					SpreadsheetSheetParser.Result result = new SpreadsheetSheetParser(sharedStrings, styles, usesDate, relationships).Parse(array5);
					dictionary2[i] = list.Count;
					list.Add(new SpreadsheetSheet(spreadsheetXmlNode2.Attribute("name") ?? $"Sheet{list.Count + 1}", result.CellsByRow, result.ColumnWidths, result.DefaultColumnWidth, result.RowHeights, result.DefaultRowHeight, result.HiddenRows, result.HiddenColumns, result.MergedRanges, result.FrozenRowCount, result.FrozenColumnCount, result.ShowsGridLines, result.Hyperlinks, styles));
				}
			}
		}
		if (list.Count == 0)
		{
			throw new SpreadsheetReadException(SpreadsheetReadErrorKind.MissingPart, "worksheets");
		}
		int valueOrDefault = SwiftText.ParseInt(spreadsheetXmlNode.FirstChild("bookViews")?.FirstChild("workbookView")?.Attribute("activeTab")).GetValueOrDefault();
		int value2;
		return new SpreadsheetWorkbook(list, dictionary2.TryGetValue(valueOrDefault, out value2) ? value2 : 0);
	}

	private static Dictionary<string, string> Relationships(SpreadsheetZipArchive archive, string partName)
	{
		int num = partName.LastIndexOf('/');
		string text = ((num >= 0) ? partName.Substring(0, num) : "");
		string text2;
		if (num < 0)
		{
			text2 = partName;
		}
		else
		{
			int num2 = num + 1;
			text2 = partName.Substring(num2, partName.Length - num2);
		}
		string text3 = text2;
		string entryName = text + "/_rels/" + text3 + ".rels";
		byte[] array = archive.Contents(entryName);
		SpreadsheetXmlNode spreadsheetXmlNode = ((array == null) ? null : SpreadsheetXmlNode.Parse(array));
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		if (spreadsheetXmlNode == null)
		{
			return dictionary;
		}
		foreach (SpreadsheetXmlNode item in spreadsheetXmlNode.Children("Relationship"))
		{
			string text4 = item.Attribute("Id");
			if (text4 != null)
			{
				string text5 = item.Attribute("Target");
				if (text5 != null)
				{
					dictionary[text4] = ((item.Attribute("TargetMode") == "External") ? text5 : ResolvedPartName(text5, text));
				}
			}
		}
		return dictionary;
	}

	public static string ResolvedPartName(string target, string folder)
	{
		if (target.StartsWith('/'))
		{
			return target.Substring(1, target.Length - 1);
		}
		if (target.StartsWith("xl/", StringComparison.Ordinal))
		{
			return target;
		}
		List<string> list = folder.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();
		string[] array = target.Split('/', StringSplitOptions.RemoveEmptyEntries);
		foreach (string text in array)
		{
			if (text == "..")
			{
				if (list.Count > 0)
				{
					list.RemoveAt(list.Count - 1);
				}
			}
			else if (text != ".")
			{
				list.Add(text);
			}
		}
		return string.Join('/', list);
	}

	internal static XmlReader CreateReader(byte[] data)
	{
		return XmlReader.Create(new MemoryStream(data), new XmlReaderSettings
		{
			DtdProcessing = DtdProcessing.Ignore,
			XmlResolver = null,
			IgnoreWhitespace = false,
			IgnoreComments = true,
			IgnoreProcessingInstructions = true,
			CheckCharacters = false
		});
	}

	internal static string LocalName(string qualifiedName)
	{
		int num = qualifiedName.LastIndexOf(':');
		if (num >= 0)
		{
			int num2 = num + 1;
			return qualifiedName.Substring(num2, qualifiedName.Length - num2);
		}
		return qualifiedName;
	}

	internal static Dictionary<string, string> Attributes(XmlReader reader)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		if (reader.HasAttributes)
		{
			while (reader.MoveToNextAttribute())
			{
				dictionary[reader.Name] = reader.Value;
			}
			reader.MoveToElement();
		}
		return dictionary;
	}
}
