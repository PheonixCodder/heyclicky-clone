using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml;

namespace HeyClicky.Core.Documents.Spreadsheet;

public sealed class SpreadsheetSheetParser
{
	public sealed class Result
	{
		public Dictionary<int, Dictionary<int, SpreadsheetCell>> CellsByRow { get; } = new Dictionary<int, Dictionary<int, SpreadsheetCell>>();

		public Dictionary<int, double> ColumnWidths { get; } = new Dictionary<int, double>();

		public double DefaultColumnWidth { get; set; } = 100.0;

		public Dictionary<int, double> RowHeights { get; } = new Dictionary<int, double>();

		public double DefaultRowHeight { get; set; } = 21.0;

		public HashSet<int> HiddenRows { get; } = new HashSet<int>();

		public HashSet<int> HiddenColumns { get; } = new HashSet<int>();

		public List<SpreadsheetCellRange> MergedRanges { get; } = new List<SpreadsheetCellRange>();

		public int FrozenRowCount { get; set; }

		public int FrozenColumnCount { get; set; }

		public bool ShowsGridLines { get; set; } = true;

		public Dictionary<SpreadsheetCellAddress, string> Hyperlinks { get; } = new Dictionary<SpreadsheetCellAddress, string>();
	}

	private enum TextTarget
	{
		None,
		Value,
		Formula,
		InlineText
	}

	public const int MaximumCellCount = 1500000;

	private readonly IReadOnlyList<string> sharedStrings;

	private readonly IReadOnlyList<SpreadsheetCellStyle> styles;

	private readonly bool usesDate1904;

	private readonly IReadOnlyDictionary<string, string> relationships;

	private readonly Result result = new Result();

	private int cellCount;

	private int currentRow = -1;

	private int currentColumn = -1;

	private string currentCellType = "n";

	private int currentCellStyleIndex;

	private readonly StringBuilder currentValue = new StringBuilder();

	private readonly StringBuilder currentFormula = new StringBuilder();

	private readonly StringBuilder currentInlineText = new StringBuilder();

	private TextTarget textTarget;

	private bool isInsideInlineString;

	private bool isInsidePhonetic;

	private bool didSeeFirstSheetView;

	public SpreadsheetSheetParser(IReadOnlyList<string> sharedStrings, IReadOnlyList<SpreadsheetCellStyle> styles, bool usesDate1904, IReadOnlyDictionary<string, string> relationships)
	{
		this.sharedStrings = sharedStrings;
		this.styles = styles;
		this.usesDate1904 = usesDate1904;
		this.relationships = relationships;
	}

	public Result Parse(byte[] data)
	{
		try
		{
			using XmlReader xmlReader = SpreadsheetXlsxReader.CreateReader(data);
			while (xmlReader.Read())
			{
				switch (xmlReader.NodeType)
				{
				case XmlNodeType.Element:
				{
					string name = SpreadsheetXlsxReader.LocalName(xmlReader.Name);
					bool isEmptyElement = xmlReader.IsEmptyElement;
					Start(name, SpreadsheetXlsxReader.Attributes(xmlReader));
					if (isEmptyElement)
					{
						End(name);
					}
					break;
				}
				case XmlNodeType.EndElement:
					End(SpreadsheetXlsxReader.LocalName(xmlReader.Name));
					break;
				case XmlNodeType.Text:
				case XmlNodeType.CDATA:
				case XmlNodeType.Whitespace:
				case XmlNodeType.SignificantWhitespace:
					switch (textTarget)
					{
					case TextTarget.Value:
						currentValue.Append(xmlReader.Value);
						break;
					case TextTarget.Formula:
						currentFormula.Append(xmlReader.Value);
						break;
					case TextTarget.InlineText:
						currentInlineText.Append(xmlReader.Value);
						break;
					}
					break;
				}
			}
		}
		catch (XmlException)
		{
		}
		return result;
	}

	private static string? Get(Dictionary<string, string> attributes, string key)
	{
		if (!attributes.TryGetValue(key, out string value))
		{
			return null;
		}
		return value;
	}

	private static double RowHeightPoints(double height)
	{
		return Math.Max(4.0, Math.Round(height * 1.3333333333333333, MidpointRounding.AwayFromZero));
	}

	private void Start(string name, Dictionary<string, string> attributes)
	{
		if (name == null)
		{
			return;
		}
		switch (name.Length)
		{
		case 9:
			switch (name[0])
			{
			case 's':
				if (name == "sheetView" && !didSeeFirstSheetView)
				{
					didSeeFirstSheetView = true;
					string text7 = Get(attributes, "showGridLines");
					if (text7 != null)
					{
						Result result = this.result;
						bool flag = ((text7 == "0" || text7 == "false") ? true : false);
						result.ShowsGridLines = !flag;
					}
				}
				break;
			case 'm':
			{
				if (!(name == "mergeCell"))
				{
					break;
				}
				string text6 = Get(attributes, "ref");
				if (text6 != null)
				{
					SpreadsheetCellRange? spreadsheetCellRange = SpreadsheetCellRange.Parse(text6);
					if (spreadsheetCellRange.HasValue)
					{
						SpreadsheetCellRange valueOrDefault10 = spreadsheetCellRange.GetValueOrDefault();
						this.result.MergedRanges.Add(valueOrDefault10);
					}
				}
				break;
			}
			case 'h':
			{
				if (!(name == "hyperlink"))
				{
					break;
				}
				string text2 = Get(attributes, "ref");
				if (text2 == null)
				{
					break;
				}
				SpreadsheetCellRange? spreadsheetCellRange = SpreadsheetCellRange.Parse(text2);
				if (!spreadsheetCellRange.HasValue)
				{
					break;
				}
				SpreadsheetCellRange valueOrDefault9 = spreadsheetCellRange.GetValueOrDefault();
				string text3 = null;
				string text4 = Get(attributes, "r:id") ?? Get(attributes, "id");
				if (text4 != null)
				{
					text3 = (relationships.TryGetValue(text4, out string value) ? value : null);
				}
				if (text3 == null)
				{
					string text5 = Get(attributes, "location");
					if (text5 != null)
					{
						text3 = "#" + text5;
					}
				}
				if (text3 != null)
				{
					this.result.Hyperlinks[new SpreadsheetCellAddress(valueOrDefault9.FirstRow, valueOrDefault9.FirstColumn)] = text3;
				}
				break;
			}
			}
			break;
		case 3:
			switch (name[2])
			{
			case 'l':
			{
				if (!(name == "col"))
				{
					break;
				}
				int? num = SwiftText.ParseInt(Get(attributes, "min"));
				if (!num.HasValue)
				{
					break;
				}
				int valueOrDefault = num.GetValueOrDefault();
				num = SwiftText.ParseInt(Get(attributes, "max"));
				if (!num.HasValue)
				{
					break;
				}
				int valueOrDefault2 = num.GetValueOrDefault();
				if (valueOrDefault < 1 || valueOrDefault2 < valueOrDefault)
				{
					break;
				}
				string text = Get(attributes, "hidden");
				bool flag = ((text == "1" || text == "true") ? true : false);
				bool flag2 = flag;
				double? num2 = SwiftText.ParseDouble(Get(attributes, "width"));
				for (int i = valueOrDefault - 1; i < Math.Min(valueOrDefault2, valueOrDefault - 1 + 2000); i++)
				{
					if (flag2)
					{
						this.result.HiddenColumns.Add(i);
					}
					if (num2.HasValue)
					{
						double valueOrDefault3 = num2.GetValueOrDefault();
						this.result.ColumnWidths[i] = SpreadsheetMetrics.Points(valueOrDefault3);
					}
				}
				break;
			}
			case 'w':
				if (name == "row")
				{
					int? num = SwiftText.ParseInt(Get(attributes, "r"));
					if (num.HasValue)
					{
						int valueOrDefault4 = num.GetValueOrDefault();
						currentRow = valueOrDefault4 - 1;
					}
					else
					{
						currentRow++;
					}
					currentColumn = -1;
					double? num3 = SwiftText.ParseDouble(Get(attributes, "ht"));
					double valueOrDefault5 = default(double);
					int num4;
					if (num3.HasValue)
					{
						valueOrDefault5 = num3.GetValueOrDefault();
						num4 = 1;
					}
					else
					{
						num4 = 0;
					}
					bool flag = (byte)num4 != 0;
					string text;
					if (flag)
					{
						text = Get(attributes, "customHeight");
						bool flag3 = ((text == "1" || text == "true") ? true : false);
						flag = flag3 || valueOrDefault5 > 0.0;
					}
					if (flag)
					{
						this.result.RowHeights[currentRow] = RowHeightPoints(valueOrDefault5);
					}
					text = Get(attributes, "hidden");
					if ((text == "1" || text == "true") ? true : false)
					{
						this.result.HiddenRows.Add(currentRow);
					}
				}
				break;
			case 'h':
				if (name == "rPh")
				{
					isInsidePhonetic = true;
				}
				break;
			}
			break;
		case 1:
			switch (name[0])
			{
			case 'c':
			{
				string text8 = Get(attributes, "r");
				if (text8 != null)
				{
					SpreadsheetCellAddress? spreadsheetCellAddress = SpreadsheetCellAddress.Parse(text8);
					if (spreadsheetCellAddress.HasValue)
					{
						SpreadsheetCellAddress valueOrDefault11 = spreadsheetCellAddress.GetValueOrDefault();
						currentRow = valueOrDefault11.Row;
						currentColumn = valueOrDefault11.Column;
						goto IL_059b;
					}
				}
				currentColumn++;
				goto IL_059b;
			}
			case 'v':
				textTarget = TextTarget.Value;
				break;
			case 'f':
				textTarget = TextTarget.Formula;
				break;
			case 't':
				{
					if (isInsideInlineString && !isInsidePhonetic)
					{
						textTarget = TextTarget.InlineText;
					}
					break;
				}
				IL_059b:
				currentCellType = Get(attributes, "t") ?? "n";
				currentCellStyleIndex = SwiftText.ParseInt(Get(attributes, "s")).GetValueOrDefault();
				currentValue.Clear();
				currentFormula.Clear();
				currentInlineText.Clear();
				break;
			}
			break;
		case 4:
		{
			if (!(name == "pane"))
			{
				break;
			}
			string text9 = Get(attributes, "state") ?? "split";
			if ((text9 == "frozen" || text9 == "frozenSplit") ? true : false)
			{
				Result obj = this.result;
				double? num3 = SwiftText.ParseDouble(Get(attributes, "xSplit"));
				int frozenColumnCount;
				if (num3.HasValue)
				{
					double valueOrDefault12 = num3.GetValueOrDefault();
					frozenColumnCount = (int)valueOrDefault12;
				}
				else
				{
					frozenColumnCount = 0;
				}
				obj.FrozenColumnCount = frozenColumnCount;
				Result obj2 = this.result;
				num3 = SwiftText.ParseDouble(Get(attributes, "ySplit"));
				int frozenRowCount;
				if (num3.HasValue)
				{
					double valueOrDefault13 = num3.GetValueOrDefault();
					frozenRowCount = (int)valueOrDefault13;
				}
				else
				{
					frozenRowCount = 0;
				}
				obj2.FrozenRowCount = frozenRowCount;
			}
			break;
		}
		case 13:
		{
			if (!(name == "sheetFormatPr"))
			{
				break;
			}
			double? num3 = SwiftText.ParseDouble(Get(attributes, "defaultRowHeight"));
			if (num3.HasValue)
			{
				double valueOrDefault6 = num3.GetValueOrDefault();
				if (valueOrDefault6 > 0.0)
				{
					this.result.DefaultRowHeight = RowHeightPoints(valueOrDefault6);
				}
			}
			num3 = SwiftText.ParseDouble(Get(attributes, "defaultColWidth"));
			if (num3.HasValue)
			{
				double valueOrDefault7 = num3.GetValueOrDefault();
				this.result.DefaultColumnWidth = SpreadsheetMetrics.Points(valueOrDefault7);
				break;
			}
			num3 = SwiftText.ParseDouble(Get(attributes, "baseColWidth"));
			if (num3.HasValue)
			{
				double valueOrDefault8 = num3.GetValueOrDefault();
				this.result.DefaultColumnWidth = SpreadsheetMetrics.Points(valueOrDefault8 + 0.71);
			}
			break;
		}
		case 2:
			if (name == "is")
			{
				isInsideInlineString = true;
			}
			break;
		}
	}

	private void End(string name)
	{
		switch (name)
		{
		case "v":
		case "f":
			textTarget = TextTarget.None;
			break;
		case "t":
			if (textTarget == TextTarget.InlineText)
			{
				textTarget = TextTarget.None;
			}
			break;
		case "is":
			isInsideInlineString = false;
			break;
		case "rPh":
			isInsidePhonetic = false;
			break;
		case "c":
			FinishCell();
			break;
		}
	}

	private void FinishCell()
	{
		if (currentRow < 0 || currentColumn < 0 || cellCount >= 1500000)
		{
			return;
		}
		SpreadsheetCellStyle spreadsheetCellStyle = ((currentCellStyleIndex >= 0 && currentCellStyleIndex < styles.Count) ? styles[currentCellStyleIndex] : new SpreadsheetCellStyle());
		string text = ((currentFormula.Length == 0) ? null : ("=" + currentFormula));
		string text2 = currentValue.ToString();
		SpreadsheetCell spreadsheetCell;
		switch (currentCellType)
		{
		case "s":
		{
			int? num2 = SwiftText.ParseInt(SwiftText.TrimWhitespace(text2));
			if (num2.HasValue)
			{
				int valueOrDefault2 = num2.GetValueOrDefault();
				if (valueOrDefault2 >= 0 && valueOrDefault2 < sharedStrings.Count)
				{
					string text7 = sharedStrings[valueOrDefault2];
					spreadsheetCell = new SpreadsheetCell(text7, text ?? text7, SpreadsheetCellKind.Text, null, currentCellStyleIndex);
					break;
				}
			}
			spreadsheetCell = null;
			break;
		}
		case "inlineStr":
		{
			string text5 = UnescapingExcelEscapes(currentInlineText.ToString());
			spreadsheetCell = new SpreadsheetCell(text5, text ?? text5, SpreadsheetCellKind.Text, null, currentCellStyleIndex);
			break;
		}
		case "str":
		{
			string text6 = UnescapingExcelEscapes(text2);
			spreadsheetCell = new SpreadsheetCell(text6, text ?? text6, SpreadsheetCellKind.Text, null, currentCellStyleIndex);
			break;
		}
		case "b":
		{
			string text4 = ((text2 == "1" || text2.ToLowerInvariant() == "true") ? "TRUE" : "FALSE");
			spreadsheetCell = new SpreadsheetCell(text4, text ?? text4, SpreadsheetCellKind.Boolean, null, currentCellStyleIndex);
			break;
		}
		case "e":
			spreadsheetCell = new SpreadsheetCell(text2, text ?? text2, SpreadsheetCellKind.Error, null, currentCellStyleIndex);
			break;
		case "d":
			spreadsheetCell = new SpreadsheetCell(text2, text ?? text2, SpreadsheetCellKind.Text, null, currentCellStyleIndex);
			break;
		default:
		{
			string text3 = SwiftText.TrimWhitespaceAndNewlines(text2);
			double? num = SwiftText.ParseDouble(text3);
			if (num.HasValue)
			{
				double valueOrDefault = num.GetValueOrDefault();
				spreadsheetCell = new SpreadsheetCell(SpreadsheetNumberFormat.Format((usesDate1904 && SpreadsheetStylesReader.IsDateCode(spreadsheetCellStyle.NumberFormatCode)) ? (valueOrDefault + 1462.0) : valueOrDefault, spreadsheetCellStyle.NumberFormatCode), text ?? SpreadsheetNumberFormat.General(valueOrDefault), SpreadsheetCellKind.Number, valueOrDefault, currentCellStyleIndex);
			}
			else if (text3.Length > 0)
			{
				spreadsheetCell = new SpreadsheetCell(text3, text ?? text3, SpreadsheetCellKind.Text, null, currentCellStyleIndex);
			}
			else
			{
				SpreadsheetColor? fillColor = spreadsheetCellStyle.FillColor;
				spreadsheetCell = ((!fillColor.HasValue && !spreadsheetCellStyle.HasBorders) ? null : new SpreadsheetCell("", text ?? "", SpreadsheetCellKind.Text, null, currentCellStyleIndex));
			}
			break;
		}
		}
		if ((object)spreadsheetCell != null)
		{
			if (!result.CellsByRow.TryGetValue(currentRow, out Dictionary<int, SpreadsheetCell> value))
			{
				value = (result.CellsByRow[currentRow] = new Dictionary<int, SpreadsheetCell>());
			}
			value[currentColumn] = spreadsheetCell;
			cellCount++;
		}
	}

	public static string UnescapingExcelEscapes(string text)
	{
		if (!text.Contains("_x", StringComparison.Ordinal))
		{
			return text;
		}
		StringBuilder stringBuilder = new StringBuilder(text.Length);
		int num = 0;
		while (num < text.Length)
		{
			if (text[num] == '_' && num + 7 <= text.Length && text[num + 1] == 'x' && text[num + 6] == '_' && uint.TryParse(text.AsSpan(num + 2, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var num2) && (num2 < 55296 || num2 > 57343))
			{
				if (num2 != 13)
				{
					stringBuilder.Append((char)num2);
				}
				num += 7;
			}
			else
			{
				stringBuilder.Append(text[num]);
				num++;
			}
		}
		return stringBuilder.ToString();
	}
}
