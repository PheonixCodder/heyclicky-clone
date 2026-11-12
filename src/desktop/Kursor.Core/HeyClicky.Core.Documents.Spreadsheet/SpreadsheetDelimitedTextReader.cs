using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace HeyClicky.Core.Documents.Spreadsheet;

public static class SpreadsheetDelimitedTextReader
{
	private static readonly string[] CurrencySymbols = new string[5] { "$", "€", "£", "¥", "₹" };

	public static SpreadsheetWorkbook Read(string path, bool isTabSeparated)
	{
		byte[] array;
		using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
		{
			if (fileStream.Length > 83886080)
			{
				throw new SpreadsheetReadException(SpreadsheetReadErrorKind.TooLarge);
			}
			array = new byte[fileStream.Length];
			fileStream.ReadExactly(array);
		}
		return Read(SwiftText.DecodeText(array) ?? throw new SpreadsheetReadException(SpreadsheetReadErrorKind.Unreadable), Path.GetFileNameWithoutExtension(path), isTabSeparated);
	}

	public static SpreadsheetWorkbook Read(string text, string sheetName, bool isTabSeparated)
	{
		char delimiter = (isTabSeparated ? '\t' : SniffDelimiter(text));
		List<List<string>> list = ParseRows(text, delimiter);
		Dictionary<int, Dictionary<int, SpreadsheetCell>> dictionary = new Dictionary<int, Dictionary<int, SpreadsheetCell>>();
		Dictionary<int, int> dictionary2 = new Dictionary<int, int>();
		int num = 0;
		for (int i = 0; i < list.Count; i++)
		{
			List<string> list2 = list[i];
			Dictionary<int, SpreadsheetCell> dictionary3 = new Dictionary<int, SpreadsheetCell>();
			for (int j = 0; j < list2.Count; j++)
			{
				string text2 = list2[j];
				if (text2.Length != 0)
				{
					if (num >= 1500000)
					{
						break;
					}
					double? numericValue = NumericValue(SwiftText.TrimWhitespace(text2));
					dictionary3[j] = new SpreadsheetCell(text2, text2, numericValue.HasValue ? SpreadsheetCellKind.Number : SpreadsheetCellKind.Text, numericValue, 0);
					num++;
					if (i < 200)
					{
						int val = text2.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(SwiftText.CharacterCount).DefaultIfEmpty(0)
							.Max();
						dictionary2[j] = Math.Max(dictionary2.GetValueOrDefault(j), val);
					}
				}
			}
			if (dictionary3.Count > 0)
			{
				dictionary[i] = dictionary3;
			}
		}
		Dictionary<int, double> dictionary4 = new Dictionary<int, double>();
		int value;
		int key;
		foreach (KeyValuePair<int, int> item in dictionary2)
		{
			item.Deconstruct(out key, out value);
			int key2 = key;
			int num2 = value;
			dictionary4[key2] = Math.Min(320.0, Math.Max(64.0, (double)num2 * 7.2 + 16.0));
		}
		Dictionary<int, double> customRowHeights = new Dictionary<int, double>();
		HashSet<int> hiddenRows = new HashSet<int>();
		HashSet<int> hiddenColumns = new HashSet<int>();
		List<SpreadsheetCellRange> mergedRanges = new List<SpreadsheetCellRange>();
		Dictionary<SpreadsheetCellAddress, string> hyperlinks = new Dictionary<SpreadsheetCellAddress, string>();
		value = 1;
		List<SpreadsheetCellStyle> list3 = new List<SpreadsheetCellStyle>(value);
		CollectionsMarshal.SetCount(list3, value);
		Span<SpreadsheetCellStyle> span = CollectionsMarshal.AsSpan(list3);
		key = 0;
		span[key] = new SpreadsheetCellStyle();
		return new SpreadsheetWorkbook(new _003C_003Ez__ReadOnlySingleElementList<SpreadsheetSheet>(new SpreadsheetSheet(sheetName, dictionary, dictionary4, 100.0, customRowHeights, 21.0, hiddenRows, hiddenColumns, mergedRanges, 0, 0, showsGridLines: true, hyperlinks, list3)), 0);
	}

	internal static double? NumericValue(string text)
	{
		if (text.Length == 0 || SwiftText.CharacterCount(text) >= 40)
		{
			return null;
		}
		double? num = SwiftText.ParseDouble(text);
		if (num.HasValue)
		{
			return num.GetValueOrDefault();
		}
		string text2 = text.Replace(",", "");
		double num2 = 1.0;
		if (text2.EndsWith('%'))
		{
			string text3 = text2;
			text2 = text3.Substring(0, text3.Length - 1);
			num2 = 0.01;
		}
		string[] currencySymbols = CurrencySymbols;
		foreach (string text4 in currencySymbols)
		{
			if (text2.StartsWith(text4, StringComparison.Ordinal) || text2.StartsWith("-" + text4, StringComparison.Ordinal))
			{
				text2 = text2.Replace(text4, "");
			}
		}
		num = SwiftText.ParseDouble(text2);
		if (num.HasValue)
		{
			double valueOrDefault = num.GetValueOrDefault();
			return valueOrDefault * num2;
		}
		return null;
	}

	private static char SniffDelimiter(string text)
	{
		string? source = ((text.Length > 4000) ? text.Substring(0, 4000) : text).Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
		int num = source.Count((char c) => c == ',');
		int num2 = source.Count((char c) => c == ';');
		int num3 = source.Count((char c) => c == '\t');
		if (num3 > num && num3 > num2)
		{
			return '\t';
		}
		if (num2 <= num)
		{
			return ',';
		}
		return ';';
	}

	internal static List<List<string>> ParseRows(string text, char delimiter)
	{
		List<List<string>> rows = new List<List<string>>();
		List<string> currentRow = new List<string>();
		StringBuilder currentField = new StringBuilder();
		bool flag = false;
		bool flag2 = false;
		for (int i = ((text.Length > 0 && text[0] == '\ufeff') ? 1 : 0); i < text.Length; i++)
		{
			char c = text[i];
			if (flag)
			{
				if (!flag2)
				{
					if (c == '"')
					{
						flag2 = true;
					}
					else
					{
						currentField.Append(c);
					}
					continue;
				}
				flag2 = false;
				if (c == '"')
				{
					currentField.Append('"');
					continue;
				}
				flag = false;
			}
			if (c == '"' && currentField.Length == 0)
			{
				flag = true;
				continue;
			}
			if (c == delimiter)
			{
				FinishField();
				continue;
			}
			switch (c)
			{
			case '\n':
				FinishRow();
				break;
			default:
				currentField.Append(c);
				break;
			case '\r':
				break;
			}
		}
		if (currentField.Length > 0 || currentRow.Count > 0)
		{
			FinishRow();
		}
		return rows;
		void FinishField()
		{
			currentRow.Add(currentField.ToString());
			currentField.Clear();
		}
		void FinishRow()
		{
			FinishField();
			rows.Add(currentRow);
			currentRow = new List<string>();
		}
	}
}
