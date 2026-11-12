using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace HeyClicky.Core.Documents.Spreadsheet;

internal static class SpreadsheetSharedStringsParser
{
	public static List<string> Strings(byte[] data)
	{
		List<string> strings = new List<string>();
		StringBuilder currentString = new StringBuilder();
		bool isInsideText = false;
		int phoneticDepth = 0;
		try
		{
			using XmlReader xmlReader = SpreadsheetXlsxReader.CreateReader(data);
			while (xmlReader.Read())
			{
				switch (xmlReader.NodeType)
				{
				case XmlNodeType.Element:
				{
					string name2 = SpreadsheetXlsxReader.LocalName(xmlReader.Name);
					bool isEmptyElement = xmlReader.IsEmptyElement;
					Start(name2);
					if (isEmptyElement)
					{
						End(name2);
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
					if (isInsideText)
					{
						currentString.Append(xmlReader.Value);
					}
					break;
				}
			}
		}
		catch (XmlException)
		{
		}
		return strings;
		void End(string name)
		{
			switch (name)
			{
			case "si":
				strings.Add(SpreadsheetSheetParser.UnescapingExcelEscapes(currentString.ToString()));
				break;
			case "t":
				isInsideText = false;
				break;
			case "rPh":
				phoneticDepth--;
				break;
			}
		}
		void Start(string name)
		{
			switch (name)
			{
			case "si":
				currentString.Clear();
				break;
			case "t":
				isInsideText = phoneticDepth == 0;
				break;
			case "rPh":
				phoneticDepth++;
				break;
			}
		}
	}
}
