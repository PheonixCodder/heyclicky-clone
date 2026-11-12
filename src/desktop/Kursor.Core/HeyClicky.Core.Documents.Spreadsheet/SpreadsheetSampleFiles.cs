using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security;
using System.Text;

namespace HeyClicky.Core.Documents.Spreadsheet;

public static class SpreadsheetSampleFiles
{
	public static class Styles
	{
		public const int Default = 0;

		public const int Header = 1;

		public const int Currency = 2;

		public const int Percent = 3;

		public const int Date = 4;

		public const int Wrapped = 5;

		public const int Scientific = 6;

		public const int Elapsed = 7;

		public const int Time = 8;

		public const int Title = 9;

		public const int Hyperlink = 10;

		public const int Boxed = 11;

		public const int Italic = 12;

		public const int RedStrike = 13;

		public const int LongDate = 14;

		public const int Indented = 15;

		public const int Accounting = 16;
	}

	public static string WriteCsv(string path)
	{
		File.WriteAllText(path, "Item,Price,Discount,Quantity,Note\n\"Espresso machine, steel\",$499,15%,\"1,234.5\",\"She said \"\"wow\"\"\"\nGrinder,$129.99,5%,2,\"Two lines:\nburr and blade\"\nMilk jug,12,0%,-3,plain\nTamper,1e2,,7,\nTotal,=SUM(B2:B5),,,\"left text that runs long enough to spill into the empty cells beside it\"\n", new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
		return path;
	}

	public static string WriteSemicolonCsv(string path)
	{
		File.WriteAllText(path, "Stadt;Einwohner;Fläche\nBerlin;3.645.000;891,8\nMünchen;1.488.000;310,7\nKöln;1.084.000;405,0\n", Encoding.UTF8);
		return path;
	}

	public static string WriteTsv(string path)
	{
		File.WriteAllText(path, "Name\tTeam\tScore\tActive\nAda\tBlue\t98.5\tTRUE\nGrace\tRed\t87\tFALSE\nLinus\tBlue\t-4\tTRUE\n", Encoding.UTF8);
		return path;
	}

	public static string WriteWorkbook(string path)
	{
		if (File.Exists(path))
		{
			File.Delete(path);
		}
		ZipArchive zip = ZipFile.Open(path, ZipArchiveMode.Create);
		try
		{
			Add("[Content_Types].xml", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">\n  <Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>\n  <Default Extension=\"xml\" ContentType=\"application/xml\"/>\n  <Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>\n</Types>");
			Add("_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">\n  <Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>\n</Relationships>");
			Add("xl/workbook.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">\n  <workbookPr date1904=\"false\"/>\n  <bookViews><workbookView activeTab=\"0\"/></bookViews>\n  <sheets>\n    <sheet name=\"Revenue\" sheetId=\"1\" r:id=\"rId1\"/>\n    <sheet name=\"Secret\" sheetId=\"2\" state=\"hidden\" r:id=\"rId2\"/>\n    <sheet name=\"Summary\" sheetId=\"3\" r:id=\"rId3\"/>\n    <sheet name=\"Wide\" sheetId=\"4\" r:id=\"rId4\"/>\n  </sheets>\n</workbook>");
			Add("xl/_rels/workbook.xml.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">\n  <Relationship Id=\"rId1\" Type=\"worksheet\" Target=\"worksheets/sheet1.xml\"/>\n  <Relationship Id=\"rId2\" Type=\"worksheet\" Target=\"worksheets/sheet2.xml\"/>\n  <Relationship Id=\"rId3\" Type=\"worksheet\" Target=\"/xl/worksheets/sheet3.xml\"/>\n  <Relationship Id=\"rId4\" Type=\"worksheet\" Target=\"./worksheets/sheet4.xml\"/>\n  <Relationship Id=\"rId5\" Type=\"styles\" Target=\"styles.xml\"/>\n  <Relationship Id=\"rId6\" Type=\"sharedStrings\" Target=\"sharedStrings.xml\"/>\n  <Relationship Id=\"rId7\" Type=\"theme\" Target=\"theme/theme1.xml\"/>\n</Relationships>");
			Add("xl/theme/theme1.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n<a:theme xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" name=\"Sample\">\n  <a:themeElements>\n    <a:clrScheme name=\"Sample\">\n      <a:dk1><a:sysClr val=\"windowText\" lastClr=\"000000\"/></a:dk1>\n      <a:lt1><a:sysClr val=\"window\" lastClr=\"FFFFFF\"/></a:lt1>\n      <a:dk2><a:srgbClr val=\"1F2937\"/></a:dk2>\n      <a:lt2><a:srgbClr val=\"E7E6E6\"/></a:lt2>\n      <a:accent1><a:srgbClr val=\"1F6FEB\"/></a:accent1>\n      <a:accent2><a:srgbClr val=\"ED7D31\"/></a:accent2>\n      <a:accent3><a:srgbClr val=\"A5A5A5\"/></a:accent3>\n      <a:accent4><a:srgbClr val=\"FFC000\"/></a:accent4>\n      <a:accent5><a:srgbClr val=\"5B9BD5\"/></a:accent5>\n      <a:accent6><a:srgbClr val=\"70AD47\"/></a:accent6>\n      <a:hlink><a:srgbClr val=\"0563C1\"/></a:hlink>\n      <a:folHlink><a:srgbClr val=\"954F72\"/></a:folHlink>\n    </a:clrScheme>\n  </a:themeElements>\n</a:theme>");
			Add("xl/styles.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">\n  <numFmts count=\"5\">\n    <numFmt numFmtId=\"164\" formatCode=\"&quot;$&quot;#,##0.00;[Red]\\(&quot;$&quot;#,##0.00\\)\"/>\n    <numFmt numFmtId=\"165\" formatCode=\"[h]:mm:ss\"/>\n    <numFmt numFmtId=\"166\" formatCode=\"0.0%\"/>\n    <numFmt numFmtId=\"167\" formatCode=\"dddd, mmmm d, yyyy\"/>\n    <numFmt numFmtId=\"168\" formatCode=\"_(&quot;$&quot;* #,##0.00_);_(&quot;$&quot;* \\(#,##0.00\\);_(&quot;$&quot;* &quot;-&quot;??_);_(@_)\"/>\n  </numFmts>\n  <fonts count=\"6\">\n    <font><sz val=\"11\"/><color theme=\"1\"/><name val=\"Calibri\"/></font>\n    <font><b/><sz val=\"11\"/><color rgb=\"FFFFFFFF\"/><name val=\"Calibri\"/></font>\n    <font><i/><sz val=\"11\"/><color theme=\"4\" tint=\"-0.249977111117893\"/><name val=\"Calibri\"/></font>\n    <font><b/><sz val=\"16\"/><color theme=\"3\"/><name val=\"Calibri\"/></font>\n    <font><u/><sz val=\"11\"/><color theme=\"10\"/><name val=\"Calibri\"/></font>\n    <font><strike/><sz val=\"11\"/><color indexed=\"10\"/><name val=\"Calibri\"/></font>\n  </fonts>\n  <fills count=\"4\">\n    <fill><patternFill patternType=\"none\"/></fill>\n    <fill><patternFill patternType=\"gray125\"/></fill>\n    <fill><patternFill patternType=\"solid\"><fgColor theme=\"4\"/><bgColor indexed=\"64\"/></patternFill></fill>\n    <fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFFFF2CC\"/></patternFill></fill>\n  </fills>\n  <borders count=\"3\">\n    <border><left/><right/><top/><bottom/><diagonal/></border>\n    <border><left/><right/><top/><bottom style=\"medium\"><color rgb=\"FF1F2937\"/></bottom><diagonal/></border>\n    <border><left style=\"dashed\"><color rgb=\"FFB45309\"/></left><right style=\"dashed\"><color rgb=\"FFB45309\"/></right><top style=\"thin\"><color rgb=\"FFB45309\"/></top><bottom style=\"thick\"><color rgb=\"FFB45309\"/></bottom><diagonal/></border>\n  </borders>\n  <cellXfs count=\"17\">\n    <xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/>\n    <xf numFmtId=\"0\" fontId=\"1\" fillId=\"2\" borderId=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\"/></xf>\n    <xf numFmtId=\"164\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/>\n    <xf numFmtId=\"166\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/>\n    <xf numFmtId=\"14\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/>\n    <xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"><alignment wrapText=\"1\" vertical=\"top\"/></xf>\n    <xf numFmtId=\"11\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/>\n    <xf numFmtId=\"165\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/>\n    <xf numFmtId=\"18\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/>\n    <xf numFmtId=\"0\" fontId=\"3\" fillId=\"0\" borderId=\"0\"><alignment horizontal=\"center\" vertical=\"center\"/></xf>\n    <xf numFmtId=\"0\" fontId=\"4\" fillId=\"0\" borderId=\"0\"/>\n    <xf numFmtId=\"0\" fontId=\"0\" fillId=\"3\" borderId=\"2\"/>\n    <xf numFmtId=\"0\" fontId=\"2\" fillId=\"0\" borderId=\"0\"/>\n    <xf numFmtId=\"0\" fontId=\"5\" fillId=\"0\" borderId=\"0\"/>\n    <xf numFmtId=\"167\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/>\n    <xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"><alignment indent=\"2\"/></xf>\n    <xf numFmtId=\"168\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/>\n  </cellXfs>\n</styleSheet>");
			string[] array = new string[29]
			{
				"Region", "Revenue", "Growth", "Updated", "Notes", "Margin", "Ratio", "Time on site", "Opens at", "North America",
				"Europe", "Asia Pacific", "Latin America", "Total", "Strong quarter; two big renewals closed early and the pipeline for next quarter looks healthy.", "Currency headwinds.", "New office in Singapore.", "Pilot only.", "Quarterly report", "Owner",
				"Status", "Due", "Report link", "Open the dashboard", "On track", "Maya", "Hidden row", "Dropped", "Line one_x000D_\nline two"
			};
			StringBuilder stringBuilder = new StringBuilder($"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><sst xmlns=\"{"http://schemas.openxmlformats.org/spreadsheetml/2006/main"}\" count=\"{array.Length}\" uniqueCount=\"{array.Length}\">");
			string[] array2 = array;
			foreach (string text in array2)
			{
				if (text == "North America")
				{
					stringBuilder.Append("<si><r><rPr><b/></rPr><t>North</t></r><r><t xml:space=\"preserve\"> America</t></r><rPh sb=\"0\" eb=\"1\"><t>ノース</t></rPh></si>");
				}
				else
				{
					stringBuilder.Append("<si><t xml:space=\"preserve\">").Append(SecurityElement.Escape(text)).Append("</t></si>");
				}
			}
			stringBuilder.Append("</sst>");
			Add("xl/sharedStrings.xml", stringBuilder.ToString());
			Add("xl/worksheets/sheet1.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">\n  <sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>\n  <sheetFormatPr defaultRowHeight=\"15\"/>\n  <cols>\n    <col min=\"1\" max=\"1\" width=\"18\" customWidth=\"1\"/>\n    <col min=\"2\" max=\"2\" width=\"14\" customWidth=\"1\"/>\n    <col min=\"4\" max=\"4\" width=\"12\" customWidth=\"1\"/>\n    <col min=\"5\" max=\"5\" width=\"34\" customWidth=\"1\"/>\n    <col min=\"6\" max=\"6\" width=\"15\" customWidth=\"1\"/>\n    <col min=\"8\" max=\"9\" width=\"12\" customWidth=\"1\"/>\n  </cols>\n  <sheetData>\n    <row r=\"1\" ht=\"22\" customHeight=\"1\">\n      <c r=\"A1\" t=\"s\" s=\"1\"><v>0</v></c><c r=\"B1\" t=\"s\" s=\"1\"><v>1</v></c><c r=\"C1\" t=\"s\" s=\"1\"><v>2</v></c><c r=\"D1\" t=\"s\" s=\"1\"><v>3</v></c>\n      <c r=\"E1\" t=\"s\" s=\"1\"><v>4</v></c><c r=\"F1\" t=\"s\" s=\"1\"><v>5</v></c><c r=\"G1\" t=\"s\" s=\"1\"><v>6</v></c><c r=\"H1\" t=\"s\" s=\"1\"><v>7</v></c><c r=\"I1\" t=\"s\" s=\"1\"><v>8</v></c>\n    </row>\n    <row r=\"2\">\n      <c r=\"A2\" t=\"s\"><v>9</v></c><c r=\"B2\" s=\"2\"><v>1284500.5</v></c><c r=\"C2\" s=\"3\"><v>0.124</v></c><c r=\"D2\" s=\"4\"><v>45580</v></c>\n      <c r=\"E2\" t=\"s\" s=\"5\"><v>14</v></c><c r=\"F2\" s=\"2\"><v>-1520.25</v></c><c r=\"G2\" s=\"6\"><v>0.000123456</v></c><c r=\"H2\" s=\"7\"><v>1.5208333333</v></c><c r=\"I2\" s=\"8\"><v>0.375</v></c>\n    </row>\n    <row r=\"3\">\n      <c r=\"A3\" t=\"s\"><v>10</v></c><c r=\"B3\" s=\"2\"><v>948200</v></c><c r=\"C3\" s=\"3\"><v>-0.031</v></c><c r=\"D3\" s=\"4\"><v>45575</v></c>\n      <c r=\"E3\" t=\"s\" s=\"5\"><v>15</v></c><c r=\"F3\" s=\"2\"><v>23810</v></c><c r=\"G3\" s=\"6\"><v>6.02E+23</v></c><c r=\"H3\" s=\"7\"><v>0.0208333333</v></c><c r=\"I3\" s=\"8\"><v>0.8125</v></c>\n    </row>\n    <row r=\"4\">\n      <c r=\"A4\" t=\"s\"><v>11</v></c><c r=\"B4\" s=\"2\"><v>731044.75</v></c><c r=\"C4\" s=\"3\"><v>0.4</v></c><c r=\"D4\" s=\"4\"><v>45590</v></c>\n      <c r=\"E4\" t=\"s\" s=\"5\"><v>16</v></c><c r=\"F4\" s=\"2\"><v>0</v></c><c r=\"G4\" s=\"6\"><v>12345</v></c><c r=\"H4\" s=\"7\"><v>2.75</v></c><c r=\"I4\" s=\"8\"><v>0.5</v></c>\n    </row>\n    <row r=\"5\">\n      <c r=\"A5\" t=\"s\" s=\"15\"><v>12</v></c><c r=\"B5\" s=\"2\"><v>98000</v></c><c r=\"C5\" s=\"3\"><v>1.25</v></c><c r=\"D5\" s=\"4\"><v>45601</v></c>\n      <c r=\"E5\" t=\"s\" s=\"5\"><v>17</v></c><c r=\"F5\" s=\"16\"><v>-42.5</v></c><c r=\"G5\" s=\"6\"><v>-0.5</v></c><c r=\"H5\" s=\"7\"><v>0</v></c><c r=\"I5\" s=\"8\"><v>0</v></c>\n    </row>\n    <row r=\"6\">\n      <c r=\"A6\" t=\"s\" s=\"1\"><v>13</v></c><c r=\"B6\" s=\"2\"><f>SUM(B2:B5)</f><v>3061745.25</v></c><c r=\"C6\" s=\"3\"><f>AVERAGE(C2:C5)</f><v>0.43325</v></c>\n      <c r=\"D6\" s=\"14\"><f>MAX(D2:D5)</f><v>45601</v></c><c r=\"F6\" s=\"16\"><f>SUM(F2:F5)</f><v>22247.25</v></c>\n    </row>\n  </sheetData>\n</worksheet>");
			Add("xl/worksheets/sheet2.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData><row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>never shown</t></is></c></row></sheetData></worksheet>");
			Add("xl/worksheets/sheet3.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">\n  <sheetViews><sheetView workbookViewId=\"0\" showGridLines=\"1\"/></sheetViews>\n  <cols><col min=\"1\" max=\"1\" width=\"22\" customWidth=\"1\"/><col min=\"2\" max=\"2\" width=\"12\" customWidth=\"1\"/><col min=\"3\" max=\"3\" width=\"10\" hidden=\"1\"/><col min=\"4\" max=\"4\" width=\"16\" customWidth=\"1\"/></cols>\n  <sheetData>\n    <row r=\"1\" ht=\"30\" customHeight=\"1\"><c r=\"A1\" t=\"s\" s=\"9\"><v>18</v></c></row>\n    <row r=\"2\"><c r=\"A2\" t=\"s\" s=\"1\"><v>19</v></c><c r=\"B2\" t=\"s\" s=\"1\"><v>20</v></c><c r=\"C2\" t=\"s\" s=\"1\"><v>27</v></c><c r=\"D2\" t=\"s\" s=\"1\"><v>21</v></c></row>\n    <row r=\"3\"><c r=\"A3\" t=\"s\" s=\"12\"><v>25</v></c><c r=\"B3\" t=\"s\"><v>24</v></c><c r=\"C3\" t=\"b\"><v>1</v></c><c r=\"D3\" s=\"4\"><v>45610</v></c></row>\n    <row r=\"4\" hidden=\"1\"><c r=\"A4\" t=\"s\"><v>26</v></c></row>\n    <row r=\"5\"><c r=\"A5\" t=\"s\" s=\"10\"><v>23</v></c><c r=\"B5\" t=\"b\"><v>0</v></c><c r=\"D5\" t=\"e\"><f>1/0</f><v>#DIV/0!</v></c></row>\n    <row r=\"6\"><c r=\"A6\" t=\"inlineStr\" s=\"11\"><is><t>Boxed and filled</t></is></c><c r=\"B6\" t=\"str\"><f>\"o\"&amp;\"k\"</f><v>ok</v></c><c r=\"D6\" t=\"s\" s=\"13\"><v>27</v></c></row>\n    <row r=\"7\"><c r=\"A7\" t=\"s\" s=\"5\"><v>28</v></c><c r=\"B7\" s=\"11\"/></row>\n  </sheetData>\n  <mergeCells count=\"1\"><mergeCell ref=\"A1:D1\"/></mergeCells>\n  <hyperlinks><hyperlink ref=\"A5\" r:id=\"rId1\"/><hyperlink ref=\"B3\" location=\"Revenue!A1\"/></hyperlinks>\n</worksheet>");
			Add("xl/worksheets/_rels/sheet3.xml.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">\n  <Relationship Id=\"rId1\" Type=\"hyperlink\" Target=\"https://example.com/dashboard\" TargetMode=\"External\"/>\n</Relationships>");
			StringBuilder stringBuilder2 = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetViews><sheetView workbookViewId=\"0\" showGridLines=\"0\"/></sheetViews><cols><col min=\"2\" max=\"3\" width=\"60\" customWidth=\"1\"/></cols><sheetData>");
			for (int j = 1; j <= 40; j++)
			{
				StringBuilder stringBuilder3 = stringBuilder2;
				StringBuilder stringBuilder4 = stringBuilder3;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(115, 3, stringBuilder3);
				handler.AppendLiteral("<row r=\"");
				handler.AppendFormatted(j);
				handler.AppendLiteral("\"><c r=\"A");
				handler.AppendFormatted(j);
				handler.AppendLiteral("\" t=\"inlineStr\"><is><t>Row ");
				handler.AppendFormatted(j);
				handler.AppendLiteral(" with text that spills across the empty cells to its right</t></is></c>");
				stringBuilder4.Append(ref handler);
				if (j % 3 == 0)
				{
					stringBuilder3 = stringBuilder2;
					StringBuilder stringBuilder5 = stringBuilder3;
					handler = new StringBuilder.AppendInterpolatedStringHandler(20, 2, stringBuilder3);
					handler.AppendLiteral("<c r=\"C");
					handler.AppendFormatted(j);
					handler.AppendLiteral("\"><v>");
					handler.AppendFormatted(((double)j * 1000.5).ToString(CultureInfo.InvariantCulture));
					handler.AppendLiteral("</v></c>");
					stringBuilder5.Append(ref handler);
				}
				stringBuilder3 = stringBuilder2;
				StringBuilder stringBuilder6 = stringBuilder3;
				handler = new StringBuilder.AppendInterpolatedStringHandler(27, 2, stringBuilder3);
				handler.AppendLiteral("<c r=\"AB");
				handler.AppendFormatted(j);
				handler.AppendLiteral("\"><v>");
				handler.AppendFormatted(j);
				handler.AppendLiteral("</v></c></row>");
				stringBuilder6.Append(ref handler);
			}
			stringBuilder2.Append("</sheetData></worksheet>");
			Add("xl/worksheets/sheet4.xml", stringBuilder2.ToString());
			return path;
		}
		finally
		{
			if (zip != null)
			{
				((IDisposable)zip).Dispose();
			}
		}
		void Add(string name, string xml)
		{
			using StreamWriter streamWriter = new StreamWriter(zip.CreateEntry(name, CompressionLevel.Optimal).Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			streamWriter.Write(xml);
		}
	}
}
