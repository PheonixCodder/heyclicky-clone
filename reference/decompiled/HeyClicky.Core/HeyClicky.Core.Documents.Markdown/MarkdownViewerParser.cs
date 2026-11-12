using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;

namespace HeyClicky.Core.Documents.Markdown;

public static class MarkdownViewerParser
{
	private const char LineSeparator = '\u2028';

	private const char ParagraphSeparator = '\u2029';

	public static string[] SplitLines(string source)
	{
		return source.Replace("\r\n", "\n").Split(RuntimeHelpers.CreateSpan<char>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/));
	}

	public static string TrimWhitespace(string text)
	{
		int i = 0;
		int num;
		for (num = text.Length; i < num && IsSpace(text[i]); i++)
		{
		}
		while (num > i && IsSpace(text[num - 1]))
		{
			num--;
		}
		int num2 = i;
		return text.Substring(num2, num - num2);
	}

	private static bool IsSpace(char c)
	{
		if (c != ' ' && c != '\t')
		{
			return char.GetUnicodeCategory(c) == UnicodeCategory.SpaceSeparator;
		}
		return true;
	}

	public static IReadOnlyList<MarkdownViewerBlock> ParseBlocks(string markdownSource)
	{
		string[] array = SplitLines(markdownSource);
		List<MarkdownViewerBlock> blocks = new List<MarkdownViewerBlock>();
		List<string> pendingParagraphLines = new List<string>();
		int i = 0;
		while (i < array.Length)
		{
			string text = array[i];
			string text2 = TrimWhitespace(text);
			if (text2.Length == 0)
			{
				FlushPendingParagraph();
				i++;
				continue;
			}
			if (text2.StartsWith("```", StringComparison.Ordinal) || text2.StartsWith("~~~", StringComparison.Ordinal))
			{
				FlushPendingParagraph();
				string value = text2.Substring(0, 3);
				string text3 = text2;
				string text4 = TrimWhitespace(text3.Substring(3, text3.Length - 3));
				List<string> list = new List<string>();
				for (i++; i < array.Length; i++)
				{
					string text5 = array[i];
					if (TrimWhitespace(text5).StartsWith(value, StringComparison.Ordinal))
					{
						i++;
						break;
					}
					list.Add(text5);
				}
				blocks.Add(new MarkdownViewerBlock.CodeBlock((text4.Length == 0) ? null : text4, string.Join("\n", list)));
				continue;
			}
			if (pendingParagraphLines.Count > 0)
			{
				int? num = SetextHeadingLevel(text2);
				if (num.HasValue)
				{
					int valueOrDefault = num.GetValueOrDefault();
					string inlineMarkdown = string.Join(" ", pendingParagraphLines);
					pendingParagraphLines.Clear();
					blocks.Add(new MarkdownViewerBlock.Heading(valueOrDefault, inlineMarkdown));
					i++;
					continue;
				}
			}
			(int, string)? tuple = ParseAtxHeading(text2);
			if (tuple.HasValue)
			{
				(int, string) valueOrDefault2 = tuple.GetValueOrDefault();
				FlushPendingParagraph();
				blocks.Add(new MarkdownViewerBlock.Heading(valueOrDefault2.Item1, valueOrDefault2.Item2));
				i++;
			}
			else if (IsHorizontalRule(text2))
			{
				FlushPendingParagraph();
				blocks.Add(new MarkdownViewerBlock.HorizontalRule());
				i++;
			}
			else if (text2.StartsWith('>'))
			{
				FlushPendingParagraph();
				List<string> list2 = new List<string>();
				for (; i < array.Length; i++)
				{
					string text6 = TrimWhitespace(array[i]);
					if (!text6.StartsWith('>'))
					{
						break;
					}
					string text3 = text6;
					string text7 = text3.Substring(1, text3.Length - 1);
					if (text7.StartsWith(' '))
					{
						text3 = text7;
						text7 = text3.Substring(1, text3.Length - 1);
					}
					list2.Add(text7);
				}
				blocks.Add(new MarkdownViewerBlock.Blockquote(ParseBlocks(string.Join("\n", list2))));
			}
			else if (text2.Contains('|') && i + 1 < array.Length && IsTableSeparatorRow(array[i + 1]))
			{
				FlushPendingParagraph();
				List<string> headerCells = ParseTableRowCells(text2);
				i += 2;
				List<IReadOnlyList<string>> list3 = new List<IReadOnlyList<string>>();
				for (; i < array.Length; i++)
				{
					string text8 = TrimWhitespace(array[i]);
					if (text8.Length == 0 || !text8.Contains('|'))
					{
						break;
					}
					list3.Add(ParseTableRowCells(text8));
				}
				blocks.Add(new MarkdownViewerBlock.Table(headerCells, list3));
			}
			else if ((object)ParseListItemStart(text) != null)
			{
				FlushPendingParagraph();
				List<MarkdownViewerListItem> list4 = new List<MarkdownViewerListItem>();
				while (i < array.Length)
				{
					string text9 = array[i];
					string text10 = TrimWhitespace(text9);
					if (text10.Length == 0)
					{
						int j;
						for (j = i + 1; j < array.Length && TrimWhitespace(array[j]).Length == 0; j++)
						{
						}
						if (j >= array.Length || (object)ParseListItemStart(array[j]) == null)
						{
							break;
						}
						i = j;
						continue;
					}
					MarkdownViewerListItem markdownViewerListItem = ParseListItemStart(text9);
					if ((object)markdownViewerListItem != null)
					{
						list4.Add(markdownViewerListItem);
						i++;
						continue;
					}
					if (list4.Count <= 0 || !text9.StartsWith("  ", StringComparison.Ordinal))
					{
						break;
					}
					MarkdownViewerListItem markdownViewerListItem2 = list4[list4.Count - 1];
					list4[list4.Count - 1] = markdownViewerListItem2 with
					{
						InlineMarkdown = markdownViewerListItem2.InlineMarkdown + " " + text10
					};
					i++;
				}
				blocks.Add(new MarkdownViewerBlock.List(list4));
			}
			else
			{
				(string, string)? tuple2 = ParseStandaloneImage(text2);
				if (tuple2.HasValue)
				{
					(string, string) valueOrDefault3 = tuple2.GetValueOrDefault();
					FlushPendingParagraph();
					blocks.Add(new MarkdownViewerBlock.Image(valueOrDefault3.Item1, valueOrDefault3.Item2));
					i++;
				}
				else
				{
					pendingParagraphLines.Add(text2);
					i++;
				}
			}
		}
		FlushPendingParagraph();
		return blocks;
		void FlushPendingParagraph()
		{
			if (pendingParagraphLines.Count != 0)
			{
				string inlineMarkdown2 = string.Join(" ", pendingParagraphLines);
				pendingParagraphLines.Clear();
				blocks.Add(new MarkdownViewerBlock.Paragraph(inlineMarkdown2));
			}
		}
	}

	private static (int Level, string InlineMarkdown)? ParseAtxHeading(string trimmedLine)
	{
		if (!trimmedLine.StartsWith('#'))
		{
			return null;
		}
		int i;
		for (i = 0; i < trimmedLine.Length && trimmedLine[i] == '#'; i++)
		{
		}
		if (i > 6)
		{
			return null;
		}
		int num = i;
		string text = trimmedLine.Substring(num, trimmedLine.Length - num);
		if (text.Length != 0 && text[0] != ' ')
		{
			return null;
		}
		string text2 = TrimWhitespace(text);
		text2 = text2.TrimEnd('#');
		text2 = TrimWhitespace(text2);
		return (i, text2);
	}

	private static int? SetextHeadingLevel(string trimmedLine)
	{
		if (trimmedLine.Length < 2)
		{
			return null;
		}
		if (trimmedLine.All((char c) => c == '='))
		{
			return 1;
		}
		if (trimmedLine.All((char c) => c == '-'))
		{
			return 2;
		}
		return null;
	}

	private static bool IsHorizontalRule(string trimmedLine)
	{
		string text = trimmedLine.Replace(" ", "");
		if (text.Length < 3)
		{
			return false;
		}
		if (!text.All((char c) => c == '-') && !text.All((char c) => c == '*'))
		{
			return text.All((char c) => c == '_');
		}
		return true;
	}

	public static bool IsTableSeparatorRow(string line)
	{
		string text = TrimWhitespace(line);
		if (!text.Contains('-') || (!text.Contains('|') && !text.StartsWith(':')))
		{
			return false;
		}
		if (text.Length > 0)
		{
			return text.All(delegate(char c)
			{
				switch (c)
				{
				case ' ':
				case '-':
				case ':':
				case '|':
					return true;
				default:
					return false;
				}
			});
		}
		return false;
	}

	private static List<string> ParseTableRowCells(string rowLine)
	{
		string text = TrimWhitespace(rowLine);
		if (text.StartsWith('|'))
		{
			string text2 = text;
			text = text2.Substring(1, text2.Length - 1);
		}
		if (text.EndsWith('|'))
		{
			string text2 = text;
			text = text2.Substring(0, text2.Length - 1);
		}
		return text.Split('|').Select(TrimWhitespace).ToList();
	}

	public static MarkdownViewerListItem? ParseListItemStart(string rawLine)
	{
		int num = 0;
		int num2 = 0;
		bool flag;
		while (true)
		{
			flag = num < rawLine.Length;
			if (flag)
			{
				char c = rawLine[num];
				bool flag2 = ((c == '\t' || c == ' ') ? true : false);
				flag = flag2;
			}
			if (!flag)
			{
				break;
			}
			num2 += ((rawLine[num] != '\t') ? 1 : 4);
			num++;
		}
		string text = rawLine;
		int num3 = num;
		string text2 = text.Substring(num3, text.Length - num3);
		string ordinalLabel = null;
		int num4 = text2.IndexOfAny(new char[2] { '.', ')' });
		flag = text2.Length >= 2;
		if (flag)
		{
			bool flag2;
			switch (text2[0])
			{
			case '*':
			case '+':
			case '-':
				flag2 = true;
				break;
			default:
				flag2 = false;
				break;
			}
			flag = flag2;
		}
		string text3;
		if (flag && text2[1] == ' ')
		{
			text = text2;
			text3 = text.Substring(2, text.Length - 2);
		}
		else
		{
			if (num4 < 1 || num4 > 3 || !text2.Substring(0, num4).All(char.IsNumber) || num4 + 1 >= text2.Length || text2[num4 + 1] != ' ')
			{
				return null;
			}
			ordinalLabel = text2.Substring(0, num4) + ".";
			text = text2;
			num3 = num4 + 2;
			text3 = text.Substring(num3, text.Length - num3);
		}
		bool? checkboxState = null;
		string text4 = text3.ToLowerInvariant();
		if (text4.StartsWith("[ ] ", StringComparison.Ordinal))
		{
			checkboxState = false;
			text = text3;
			text3 = text.Substring(4, text.Length - 4);
		}
		else if (text4.StartsWith("[x] ", StringComparison.Ordinal))
		{
			checkboxState = true;
			text = text3;
			text3 = text.Substring(4, text.Length - 4);
		}
		return new MarkdownViewerListItem(Math.Min(num2 / 2, 5), ordinalLabel, checkboxState, TrimWhitespace(text3));
	}

	[GeneratedRegex("^!\\[([^\\]]*)\\]\\(\\s*(\\S+?)(?:\\s+\"[^\"]*\")?\\s*\\)$")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex StandaloneImageRegex()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__StandaloneImageRegex_18.Instance;
	}

	private static (string AlternateText, string Source)? ParseStandaloneImage(string trimmedLine)
	{
		if (!trimmedLine.StartsWith("![", StringComparison.Ordinal) || !StandaloneImageRegex().IsMatch(trimmedLine))
		{
			return null;
		}
		int num = trimmedLine.IndexOf(']');
		if (num < 0)
		{
			return null;
		}
		string item = trimmedLine.Substring(2, num - 2);
		int num2 = num + 2;
		string item2 = trimmedLine.Substring(num2, trimmedLine.Length - 1 - num2).Split(RuntimeHelpers.CreateSpan<char>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/)).FirstOrDefault((string s) => s.Length > 0) ?? "";
		return (item, item2);
	}

	public static bool IsCallout(IReadOnlyList<MarkdownViewerBlock> childBlocks)
	{
		if (childBlocks.Count == 0 || !(childBlocks[0] is MarkdownViewerBlock.Paragraph paragraph))
		{
			return false;
		}
		string head = new string(paragraph.InlineMarkdown.Take(24).ToArray()).ToLowerInvariant();
		return new string[5] { "note", "tip", "important", "warning", "caution" }.Any((string word) => head.StartsWith("**" + word + "** ·", StringComparison.Ordinal));
	}

	[GeneratedRegex("!\\[([^\\]]*)\\]\\(")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex InlineImageRegex()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__InlineImageRegex_19.Instance;
	}

	public static string RewriteInlineImagesAsLinks(string inlineMarkdown)
	{
		return InlineImageRegex().Replace(inlineMarkdown, "[$1](");
	}
}
