using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace HeyClicky.Agents.Turns;

public static class CitationMarkers
{
	private const char Open = '\ue200';

	private const char Close = '\ue201';

	private const char Separator = '\ue202';

	private static readonly Regex BareCitation = new Regex("\\s*(?:file)?cite(?:\\s*turn\\d+(?:search|news|view|fetch|open|image|file)\\d+)+", RegexOptions.CultureInvariant);

	private static readonly Regex CitationLink = new Regex("\\[([^\\]\\n]+)\\]\\(turn\\d+(?:search|news|view|fetch|open|image|file)\\d+\\)", RegexOptions.CultureInvariant);

	public static string StrippingBareCitationTokens(string text)
	{
		if (text.Contains("](turn", StringComparison.Ordinal))
		{
			text = CitationLink.Replace(text, "$1");
		}
		if (!text.Contains("turn", StringComparison.Ordinal) || !text.Contains("cite", StringComparison.Ordinal))
		{
			return text;
		}
		return BareCitation.Replace(text, "");
	}

	public static string Strip(string text)
	{
		return StrippingBareCitationTokens(StripMarkers(text));
	}

	private static string StripMarkers(string text)
	{
		if (!text.Any(IsMarker))
		{
			return text;
		}
		StringBuilder stringBuilder = new StringBuilder(text.Length);
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			if (c != '\ue200')
			{
				if (!IsMarker(c))
				{
					stringBuilder.Append(c);
				}
				continue;
			}
			int num = text.IndexOf('\ue201', i + 1);
			if (num < 0)
			{
				break;
			}
			int num2 = i + 1;
			string text2 = EntityName(text.Substring(num2, num - num2));
			if (text2 != null)
			{
				stringBuilder.Append(text2);
			}
			else
			{
				while (stringBuilder.Length > 0)
				{
					if (stringBuilder[stringBuilder.Length - 1] != ' ')
					{
						break;
					}
					stringBuilder.Length--;
				}
			}
			i = num;
		}
		return stringBuilder.ToString();
	}

	private static bool IsMarker(char c)
	{
		if (c >= '\ue200')
		{
			return c <= '\ue2ff';
		}
		return false;
	}

	private static string? EntityName(string body)
	{
		int num = body.IndexOf('\ue202');
		if (num < 0 || body.Substring(0, num) != "entity")
		{
			return null;
		}
		try
		{
			int num2 = num + 1;
			if (JsonNode.Parse(body.Substring(num2, body.Length - num2), null) is JsonArray { Count: >=2 } jsonArray)
			{
				string text = jsonArray[1]?.ToString();
				if (text != null && text.Trim().Length > 0)
				{
					return text;
				}
			}
		}
		catch (JsonException)
		{
		}
		return null;
	}
}
