using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace HeyClicky.Core.Notch;

public static class ChatReplyPreview
{
	public const int MaximumLines = 10;

	public const int CharactersPerLine = 52;

	private const string CitationPattern = "\\s?\\(\\[([^\\]]{1,80})\\]\\((https?://[^)\\s]+)\\)\\)";

	private static readonly Regex CitationRegex = new Regex("\\s?\\(\\[([^\\]]{1,80})\\]\\((https?://[^)\\s]+)\\)\\)", RegexOptions.Compiled);

	private static readonly string[] CalloutKinds = new string[5] { "note", "tip", "important", "warning", "caution" };

	public static Func<string, string> StripCitationMarkers { get; set; } = (string s) => s;

	public static (string? Chat, bool HasWriteUp) Parts(string markdown)
	{
		bool flag = false;
		bool item = false;
		List<string> list = new List<string>();
		string[] array = markdown.Split('\n');
		foreach (string text in array)
		{
			string text2 = text.Trim();
			if (text2.StartsWith("```", StringComparison.Ordinal))
			{
				flag = !flag;
			}
			if (!flag && text2.StartsWith('#') && text2.TrimStart('#').StartsWith(' '))
			{
				item = true;
				break;
			}
			list.Add(text);
		}
		string text3 = string.Join("\n", list).Trim();
		return (Chat: (text3.Length == 0) ? null : text3, HasWriteUp: item);
	}

	public static string WithoutCitations(string markdown)
	{
		return CitationRegex.Replace(markdown, "");
	}

	public static string? FullReplyMarkdown(ChatHandOff? entry)
	{
		string text = entry?.FullReply ?? entry?.ReplyText;
		if (text == null)
		{
			return null;
		}
		return ForDocumentViewer(StripCitationMarkers(text));
	}

	public static string ForDocumentViewer(string markdown)
	{
		List<string> list = new List<string>();
		List<string> list2 = null;
		string[] array = CitationsMovedToSources(markdown).Split('\n');
		foreach (string text in array)
		{
			string text2 = text.Trim();
			if (list2 != null)
			{
				if (text2.StartsWith("```", StringComparison.Ordinal))
				{
					list.AddRange(ChartAsTable(list2));
					list2 = null;
				}
				else
				{
					list2.Add(text2);
				}
				continue;
			}
			if (text2 == "```chart")
			{
				list2 = new List<string>();
				continue;
			}
			if (text2.StartsWith('>'))
			{
				string text3 = CalloutKind(text2);
				if (text3 != null)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(8, 2);
					defaultInterpolatedStringHandler.AppendLiteral("> **");
					defaultInterpolatedStringHandler.AppendFormatted(char.ToUpperInvariant(text3[0]));
					string text4 = text3;
					defaultInterpolatedStringHandler.AppendFormatted(text4.Substring(1, text4.Length - 1));
					defaultInterpolatedStringHandler.AppendLiteral("** ·");
					list.Add(defaultInterpolatedStringHandler.ToStringAndClear());
					continue;
				}
			}
			list.Add(text);
		}
		if (list2 != null)
		{
			string text4 = "```chart";
			List<string> list3 = list2;
			int i = 0;
			array = new string[1 + list3.Count];
			array[i] = text4;
			i++;
			Span<string> span = CollectionsMarshal.AsSpan(list3);
			span.CopyTo(new Span<string>(array).Slice(i, span.Length));
			i += span.Length;
			list.AddRange(new global::_003C_003Ez__ReadOnlyArray<string>(array));
		}
		return string.Join("\n", list);
	}

	private static string? CalloutKind(string line)
	{
		string text = line;
		string text2 = text.Substring(1, text.Length - 1).Trim().ToLowerInvariant();
		if (!text2.StartsWith("[!", StringComparison.Ordinal) || !text2.EndsWith(']'))
		{
			return null;
		}
		text = text2;
		string text3 = text.Substring(2, text.Length - 1 - 2);
		if (!CalloutKinds.Contains(text3))
		{
			return null;
		}
		return text3;
	}

	private static List<string> ChartAsTable(List<string> lines)
	{
		string text = null;
		string text2 = null;
		List<string> list = new List<string>();
		string text4;
		int num2;
		foreach (string item in lines.Where((string l) => l.Length > 0))
		{
			int num = item.IndexOf(':');
			if (num >= 0)
			{
				string text3 = item.Substring(0, num).Trim();
				text4 = item;
				num2 = num + 1;
				string text5 = text4.Substring(num2, text4.Length - num2).Trim();
				switch (text3.ToLowerInvariant())
				{
				case "title":
					text = text5;
					continue;
				case "caption":
					text2 = text5;
					continue;
				case "type":
					continue;
				}
				list.Add($"| {text3} | {text5} |");
			}
		}
		if (list.Count == 0)
		{
			return new List<string>();
		}
		List<string> list2 = new List<string>();
		if (text != null)
		{
			list2.AddRange(new global::_003C_003Ez__ReadOnlyArray<string>(new string[2]
			{
				"**" + text + "**",
				""
			}));
		}
		text4 = "| | " + text2 + " |";
		string text6 = "|---|---|";
		List<string> list3 = list;
		num2 = 0;
		string[] array = new string[2 + list3.Count];
		array[num2] = text4;
		num2++;
		array[num2] = text6;
		num2++;
		Span<string> span = CollectionsMarshal.AsSpan(list3);
		span.CopyTo(new Span<string>(array).Slice(num2, span.Length));
		num2 += span.Length;
		list2.AddRange(new global::_003C_003Ez__ReadOnlyArray<string>(array));
		list2.Add("");
		return list2;
	}

	public static string CitationsMovedToSources(string markdown)
	{
		List<(string, string)> list = new List<(string, string)>();
		foreach (Match item in CitationRegex.Matches(markdown))
		{
			string url = item.Groups[2].Value;
			if (!list.Any<(string, string)>(((string Title, string Url) c) => c.Url == url))
			{
				list.Add((item.Groups[1].Value, url));
			}
		}
		if (list.Count == 0)
		{
			return markdown;
		}
		string text = CitationRegex.Replace(markdown, "");
		HashSet<string> seenTitles = new HashSet<string>();
		List<(string, string)> list2 = list.Where<(string, string)>(delegate((string Title, string Url) c)
		{
			string text2 = c.Title.ToLowerInvariant();
			if (text.Contains(c.Url, StringComparison.Ordinal) || text.ToLowerInvariant().Contains("[" + text2 + "]", StringComparison.Ordinal) || seenTitles.Contains(text2))
			{
				return false;
			}
			seenTitles.Add(text2);
			return true;
		}).ToList();
		if (list2.Count == 0)
		{
			return text;
		}
		string text3 = string.Join("\n", list2.Select<(string, string), string>(((string Title, string Url) c) => $"- [{c.Title}]({c.Url})"));
		bool num = Regex.IsMatch(text, "(?m)^#{2,6}\\s+Sources?\\s*$", RegexOptions.IgnoreCase);
		text = text.Trim();
		if (!num)
		{
			return text + "\n\n## Sources\n\n" + text3;
		}
		return text + "\n" + text3;
	}

	public static (string Text, bool IsTruncated) Preview(string markdown)
	{
		List<string> list = new List<string>();
		int num = 0;
		bool flag = false;
		string[] array = markdown.Split('\n');
		foreach (string text in array)
		{
			string text2 = text.Trim();
			int num2 = ((text2.Length == 0) ? (flag ? 1 : 0) : Math.Max(1, (int)Math.Ceiling((double)text2.Length / 52.0)));
			if (num + num2 > 10 && list.Count > 0)
			{
				if (flag)
				{
					list.Add("```");
				}
				while (list.Count > 0)
				{
					if (list[list.Count - 1].Trim().Length != 0)
					{
						break;
					}
					list.RemoveAt(list.Count - 1);
				}
				return (Text: string.Join("\n", list), IsTruncated: true);
			}
			num += num2;
			list.Add(text);
			if (text2.StartsWith("```", StringComparison.Ordinal))
			{
				flag = !flag;
			}
		}
		return (Text: markdown, IsTruncated: false);
	}

	public static string BubbleText(string markdown)
	{
		List<string> list = new List<string>();
		string[] array = markdown.Split('\n');
		foreach (string text in array)
		{
			string line = text;
			string text2 = line.Trim();
			if (text2.StartsWith("```", StringComparison.Ordinal))
			{
				continue;
			}
			if (text2.StartsWith('#'))
			{
				line = text2.TrimStart('#').Trim();
				if (line.Length > 0)
				{
					line = "**" + line + "**";
				}
			}
			else if (text2.StartsWith('|'))
			{
				List<string> source = (from c in text2.Split('|', StringSplitOptions.RemoveEmptyEntries)
					select c.Trim()).ToList();
				if (source.All((string c) => c.All((char ch) => "-: ".Contains(ch))))
				{
					continue;
				}
				line = string.Join(" · ", source.Where((string c) => c.Length > 0));
			}
			else if (text2.StartsWith("- [ ] ", StringComparison.Ordinal) || text2.StartsWith("- [x] ", StringComparison.Ordinal) || text2.StartsWith("- [X] ", StringComparison.Ordinal))
			{
				string text3 = Indent();
				string obj = (text2.StartsWith("- [ ] ", StringComparison.Ordinal) ? "○ " : "✓ ");
				string text4 = text2;
				line = text3 + obj + text4.Substring(6, text4.Length - 6);
			}
			else if (text2.StartsWith("- ", StringComparison.Ordinal) || text2.StartsWith("* ", StringComparison.Ordinal))
			{
				string text5 = Indent();
				string text4 = text2;
				line = text5 + "• " + text4.Substring(2, text4.Length - 2);
			}
			list.Add(line);
			string Indent()
			{
				return new string(' ', line.Length - line.TrimStart(' ').Length);
			}
		}
		List<string> list2 = new List<string>();
		foreach (string item in list)
		{
			if (item.Trim().Length == 0)
			{
				if (list2.Count == 0)
				{
					continue;
				}
				if (list2[list2.Count - 1].Trim().Length == 0)
				{
					continue;
				}
			}
			list2.Add(item);
		}
		return string.Join("\n", list2).Trim();
	}

	public static bool UsesWideLayout(string? markdown)
	{
		if (markdown == null)
		{
			return false;
		}
		if (markdown.Length > 1200)
		{
			return true;
		}
		if (markdown.Contains("```") || markdown.Contains("~~~") || markdown.Contains("!["))
		{
			return true;
		}
		return markdown.Split('\n', StringSplitOptions.RemoveEmptyEntries).Any(delegate(string line)
		{
			string text = line.Trim();
			return text.StartsWith('|') && text.Contains("---");
		});
	}
}
