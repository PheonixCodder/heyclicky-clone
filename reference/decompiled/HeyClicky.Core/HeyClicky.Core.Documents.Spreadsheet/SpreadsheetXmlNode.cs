using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace HeyClicky.Core.Documents.Spreadsheet;

public sealed class SpreadsheetXmlNode(string name, Dictionary<string, string> attributes)
{
	public string Name { get; } = name;

	public Dictionary<string, string> Attributes { get; } = attributes;

	public List<SpreadsheetXmlNode> ChildNodes { get; } = new List<SpreadsheetXmlNode>();

	public string Text { get; set; } = "";

	public string? Attribute(string key)
	{
		if (!Attributes.TryGetValue(key, out string value))
		{
			return null;
		}
		return value;
	}

	public SpreadsheetXmlNode? FirstChild(string localName)
	{
		return ChildNodes.FirstOrDefault((SpreadsheetXmlNode c) => c.Name == localName);
	}

	public List<SpreadsheetXmlNode> Children(string localName)
	{
		return ChildNodes.Where((SpreadsheetXmlNode c) => c.Name == localName).ToList();
	}

	public static SpreadsheetXmlNode? Parse(byte[] data)
	{
		SpreadsheetXmlNode result = null;
		List<SpreadsheetXmlNode> list = new List<SpreadsheetXmlNode>();
		try
		{
			using XmlReader xmlReader = SpreadsheetXlsxReader.CreateReader(data);
			while (xmlReader.Read())
			{
				switch (xmlReader.NodeType)
				{
				case XmlNodeType.Element:
				{
					bool isEmptyElement = xmlReader.IsEmptyElement;
					SpreadsheetXmlNode spreadsheetXmlNode = new SpreadsheetXmlNode(SpreadsheetXlsxReader.LocalName(xmlReader.Name), SpreadsheetXlsxReader.Attributes(xmlReader));
					if (list.Count > 0)
					{
						list[list.Count - 1].ChildNodes.Add(spreadsheetXmlNode);
					}
					else
					{
						result = spreadsheetXmlNode;
					}
					if (!isEmptyElement)
					{
						list.Add(spreadsheetXmlNode);
					}
					break;
				}
				case XmlNodeType.EndElement:
					if (list.Count > 0)
					{
						list.RemoveAt(list.Count - 1);
					}
					break;
				case XmlNodeType.Text:
				case XmlNodeType.CDATA:
				case XmlNodeType.Whitespace:
				case XmlNodeType.SignificantWhitespace:
					if (list.Count > 0)
					{
						list[list.Count - 1].Text += xmlReader.Value;
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
}
