using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace HeyClicky.Core.Documents.Markdown;

public static class MarkdownInlineParser
{
	private abstract class Node
	{
	}

	private sealed class TextNode : Node
	{
		public StringBuilder Text = new StringBuilder(text);

		public TextNode(string text)
		{
		}
	}

	private sealed class CodeNode(string text) : Node()
	{
		public string Text = text;
	}

	private sealed class StyleNode(char kind, List<Node> children) : Node()
	{
		public char Kind = kind;

		public List<Node> Children = children;
	}

	private sealed class LinkNode(string url, List<Node> children) : Node()
	{
		public string Url = url;

		public List<Node> Children = children;
	}

	private sealed class Delimiter
	{
		public char Char = ch;

		public int Count = count;

		public int Original = count;

		public bool CanOpen = canOpen;

		public bool CanClose = canClose;

		public bool Active = true;

		public TextNode Node = node;

		public Delimiter(char ch, int count, bool canOpen, bool canClose, TextNode node)
		{
		}
	}

	private sealed class Bracket(int nodeIndex, int delimiterIndex, TextNode node)
	{
		public int NodeIndex = nodeIndex;

		public int DelimiterIndex = delimiterIndex;

		public TextNode Node = node;

		public bool Active = true;
	}

	public static IReadOnlyList<MarkdownInlineRun> Parse(string markdown)
	{
		List<Node> nodes = ParseNodes(markdown);
		List<MarkdownInlineRun> list = new List<MarkdownInlineRun>();
		Flatten(nodes, strong: false, emphasis: false, strike: false, null, list);
		List<MarkdownInlineRun> list2 = new List<MarkdownInlineRun>();
		foreach (MarkdownInlineRun item in list)
		{
			if (item.Text.Length == 0)
			{
				continue;
			}
			if (list2.Count > 0)
			{
				MarkdownInlineRun markdownInlineRun = list2[list2.Count - 1];
				if (markdownInlineRun with
				{
					Text = ""
				} == item with
				{
					Text = ""
				})
				{
					list2[list2.Count - 1] = markdownInlineRun with
					{
						Text = markdownInlineRun.Text + item.Text
					};
					continue;
				}
			}
			list2.Add(item);
		}
		return list2;
	}

	public static string PlainText(string markdown)
	{
		return string.Concat(from r in Parse(markdown)
			select r.Text);
	}

	private static void Flatten(List<Node> nodes, bool strong, bool emphasis, bool strike, string? link, List<MarkdownInlineRun> runs)
	{
		foreach (Node node in nodes)
		{
			if (!(node is TextNode textNode))
			{
				if (!(node is CodeNode codeNode))
				{
					if (!(node is StyleNode styleNode))
					{
						if (node is LinkNode linkNode)
						{
							Flatten(linkNode.children, strong, emphasis, strike, linkNode.url, runs);
						}
					}
					else
					{
						Flatten(styleNode.children, strong || styleNode.kind == 'S', emphasis || styleNode.kind == 'E', strike || styleNode.kind == '~', link, runs);
					}
				}
				else
				{
					runs.Add(new MarkdownInlineRun(codeNode.text, strong, emphasis, IsCode: true, strike, link));
				}
			}
			else
			{
				runs.Add(new MarkdownInlineRun(textNode.Text.ToString(), strong, emphasis, IsCode: false, strike, link));
			}
		}
	}

	private static bool IsAsciiPunctuation(char c)
	{
		switch (c)
		{
		case '!':
		case '"':
		case '#':
		case '$':
		case '%':
		case '&':
		case '\'':
		case '(':
		case ')':
		case '*':
		case '+':
		case ',':
		case '-':
		case '.':
		case '/':
		case ':':
		case ';':
		case '<':
		case '=':
		case '>':
		case '?':
		case '@':
		case '[':
		case '\\':
		case ']':
		case '^':
		case '_':
		case '`':
		case '{':
		case '|':
		case '}':
		case '~':
			return true;
		default:
			return false;
		}
	}

	private static bool IsPunctuation(char c)
	{
		bool flag = IsAsciiPunctuation(c);
		if (!flag)
		{
			UnicodeCategory unicodeCategory = char.GetUnicodeCategory(c);
			bool flag2 = (uint)(unicodeCategory - 18) <= 10u;
			flag = flag2;
		}
		return flag;
	}

	private static List<Node> ParseNodes(string s)
	{
		List<Node> nodes = new List<Node>();
		List<Delimiter> delimiters = new List<Delimiter>();
		List<Bracket> brackets = new List<Bracket>();
		StringBuilder text = new StringBuilder();
		int num = 0;
		while (num < s.Length)
		{
			char c = s[num];
			if (c == '\\' && num + 1 < s.Length && IsAsciiPunctuation(s[num + 1]))
			{
				text.Append(s[num + 1]);
				num += 2;
				continue;
			}
			switch (c)
			{
			case '`':
			{
				int i;
				for (i = 0; num + i < s.Length && s[num + i] == '`'; i++)
				{
				}
				int num2 = FindBacktickRun(s, num + i, i);
				if (num2 < 0)
				{
					text.Append('`', i);
					num += i;
					continue;
				}
				int num3 = num + i;
				string text2 = s.Substring(num3, num2 - num3).Replace('\n', ' ');
				if (text2.Length >= 2 && text2[0] == ' ')
				{
					string text3 = text2;
					if (text3[text3.Length - 1] == ' ' && text2.Any((char ch) => ch != ' '))
					{
						string text4 = text2;
						text2 = text4.Substring(1, text4.Length - 1 - 1);
					}
				}
				FlushText();
				nodes.Add(new CodeNode(text2));
				num = num2 + i;
				continue;
			}
			case '&':
			{
				if (TryEntity(s, num, out string decoded, out int length))
				{
					text.Append(decoded);
					num += length;
					continue;
				}
				break;
			}
			}
			if (c == '<' && TryAutolink(s, num, out string url, out int length2))
			{
				FlushText();
				string text5;
				if (!url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) || s.AsSpan(num + 1).StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
				{
					text5 = url;
				}
				else
				{
					string text4 = url;
					text5 = text4.Substring(7, text4.Length - 7);
				}
				string text6 = text5;
				List<Node> list = nodes;
				string url2 = url;
				int num3 = 1;
				List<Node> list2 = new List<Node>(num3);
				CollectionsMarshal.SetCount(list2, num3);
				Span<Node> span = CollectionsMarshal.AsSpan(list2);
				int index = 0;
				span[index] = new TextNode(text6);
				list.Add(new LinkNode(url2, list2));
				num += length2;
				continue;
			}
			if ((c == '*' || c == '_' || c == '~') ? true : false)
			{
				int j;
				for (j = 0; num + j < s.Length && s[num + j] == c; j++)
				{
				}
				if (c == '~' && j > 2)
				{
					text.Append(c, j);
					num += j;
					continue;
				}
				char c2 = ((num > 0) ? s[num - 1] : ' ');
				char c3 = ((num + j < s.Length) ? s[num + j] : ' ');
				bool flag = char.IsWhiteSpace(c3);
				bool flag2 = char.IsWhiteSpace(c2);
				bool flag3 = !flag && (!IsPunctuation(c3) || flag2 || IsPunctuation(c2));
				bool flag4 = !flag2 && (!IsPunctuation(c2) || flag || IsPunctuation(c3));
				bool canOpen;
				bool canClose;
				if (c == '_')
				{
					canOpen = flag3 && (!flag4 || IsPunctuation(c2));
					canClose = flag4 && (!flag3 || IsPunctuation(c3));
				}
				else
				{
					canOpen = flag3;
					canClose = flag4;
				}
				FlushText();
				TextNode textNode = new TextNode(new string(c, j));
				nodes.Add(textNode);
				delimiters.Add(new Delimiter(c, j, canOpen, canClose, textNode));
				num += j;
				continue;
			}
			switch (c)
			{
			case '[':
			{
				FlushText();
				TextNode textNode2 = new TextNode("[");
				nodes.Add(textNode2);
				brackets.Add(new Bracket(nodes.Count - 1, delimiters.Count, textNode2));
				num++;
				break;
			}
			case ']':
			{
				object obj;
				if (brackets.Count <= 0)
				{
					obj = null;
				}
				else
				{
					List<Bracket> list3 = brackets;
					obj = list3[list3.Count - 1];
				}
				Bracket bracket = (Bracket)obj;
				string destination;
				int length3;
				if (bracket == null)
				{
					text.Append(']');
					num++;
				}
				else if (!bracket.Active)
				{
					brackets.RemoveAt(brackets.Count - 1);
					text.Append(']');
					num++;
				}
				else if (TryLinkTail(s, num + 1, out destination, out length3))
				{
					FlushText();
					List<Node> range = nodes.GetRange(bracket.nodeIndex + 1, nodes.Count - bracket.nodeIndex - 1);
					List<Delimiter> range2 = delimiters.GetRange(bracket.delimiterIndex, delimiters.Count - bracket.delimiterIndex);
					ProcessEmphasis(range, range2);
					nodes.RemoveRange(bracket.nodeIndex, nodes.Count - bracket.nodeIndex);
					delimiters.RemoveRange(bracket.delimiterIndex, delimiters.Count - bracket.delimiterIndex);
					nodes.Add(new LinkNode(destination, range));
					brackets.RemoveAt(brackets.Count - 1);
					foreach (Bracket item in brackets)
					{
						item.Active = false;
					}
					num += 1 + length3;
				}
				else
				{
					brackets.RemoveAt(brackets.Count - 1);
					text.Append(']');
					num++;
				}
				break;
			}
			default:
				text.Append(c);
				num++;
				break;
			}
		}
		FlushText();
		ProcessEmphasis(nodes, delimiters);
		return nodes;
		void FlushText()
		{
			if (text.Length == 0)
			{
				return;
			}
			if (nodes.Count > 0)
			{
				List<Node> list4 = nodes;
				Node node = list4[list4.Count - 1];
				TextNode last = node as TextNode;
				if (last != null && !delimiters.Any((Delimiter d) => d.node == last) && !brackets.Any((Bracket b) => b.node == last))
				{
					last.Text.Append(text);
					goto IL_00b0;
				}
			}
			nodes.Add(new TextNode(text.ToString()));
			goto IL_00b0;
			IL_00b0:
			text.Clear();
		}
	}

	private static int FindBacktickRun(string s, int from, int length)
	{
		int num = from;
		while (num < s.Length)
		{
			if (s[num] != '`')
			{
				num++;
				continue;
			}
			int i;
			for (i = 0; num + i < s.Length && s[num + i] == '`'; i++)
			{
			}
			if (i == length)
			{
				return num;
			}
			num += i;
		}
		return -1;
	}

	private static bool TryEntity(string s, int i, out string decoded, out int length)
	{
		decoded = "";
		length = 0;
		int num = s.IndexOf(';', i);
		if (num < 0 || num - i > 32)
		{
			return false;
		}
		string text = s.Substring(i, num + 1 - i);
		string text2 = WebUtility.HtmlDecode(text);
		if (text2 == text)
		{
			return false;
		}
		decoded = text2;
		length = text.Length;
		return true;
	}

	private static bool TryAutolink(string s, int i, out string url, out int length)
	{
		url = "";
		length = 0;
		int num = s.IndexOf('>', i + 1);
		if (num < 0)
		{
			return false;
		}
		int num2 = i + 1;
		string text = s.Substring(num2, num - num2);
		if (text.Length == 0 || text.Any((char ch) => char.IsWhiteSpace(ch) || ch == '<'))
		{
			return false;
		}
		int num3 = text.IndexOf(':');
		int num4;
		int num5;
		if (num3 >= 2 && num3 <= 32 && char.IsAsciiLetter(text[0]))
		{
			num4 = (text.Substring(0, num3).All(delegate(char ch)
			{
				bool flag = char.IsAsciiLetterOrDigit(ch);
				if (!flag)
				{
					bool flag2;
					switch (ch)
					{
					case '+':
					case '-':
					case '.':
						flag2 = true;
						break;
					default:
						flag2 = false;
						break;
					}
					flag = flag2;
				}
				return flag;
			}) ? 1 : 0);
			if (num4 != 0)
			{
				num5 = 0;
				goto IL_00bf;
			}
		}
		else
		{
			num4 = 0;
		}
		num5 = (Regex.IsMatch(text, "^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$") ? 1 : 0);
		goto IL_00bf;
		IL_00bf:
		bool flag3 = (byte)num5 != 0;
		if (num4 == 0 && !flag3)
		{
			return false;
		}
		url = (flag3 ? ("mailto:" + text) : text);
		length = num - i + 1;
		return true;
	}

	private static bool TryLinkTail(string s, int i, out string destination, out int length)
	{
		destination = "";
		length = 0;
		if (i >= s.Length || s[i] != '(')
		{
			return false;
		}
		int j;
		for (j = i + 1; j < s.Length && char.IsWhiteSpace(s[j]); j++)
		{
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (j < s.Length && s[j] == '<')
		{
			j++;
			while (j < s.Length && s[j] != '>' && s[j] != '\n')
			{
				if (s[j] == '\\' && j + 1 < s.Length && IsAsciiPunctuation(s[j + 1]))
				{
					stringBuilder.Append(s[j + 1]);
					j += 2;
					continue;
				}
				if (s[j] == '<')
				{
					return false;
				}
				stringBuilder.Append(s[j++]);
			}
			if (j >= s.Length || s[j] != '>')
			{
				return false;
			}
			j++;
		}
		else
		{
			int num = 0;
			while (j < s.Length && !char.IsWhiteSpace(s[j]) && !char.IsControl(s[j]))
			{
				if (s[j] == '\\' && j + 1 < s.Length && IsAsciiPunctuation(s[j + 1]))
				{
					stringBuilder.Append(s[j + 1]);
					j += 2;
					continue;
				}
				if (s[j] == '(')
				{
					num++;
				}
				else if (s[j] == ')')
				{
					if (num == 0)
					{
						break;
					}
					num--;
				}
				stringBuilder.Append(s[j++]);
			}
			if (num != 0)
			{
				return false;
			}
		}
		int num2 = j;
		for (; j < s.Length && char.IsWhiteSpace(s[j]); j++)
		{
		}
		bool flag = j < s.Length && j > num2;
		if (flag)
		{
			char c = s[j];
			bool flag2 = ((c == '"' || c == '\'' || c == '(') ? true : false);
			flag = flag2;
		}
		if (flag)
		{
			char c2 = ((s[j] == '(') ? ')' : s[j]);
			for (j++; j < s.Length && s[j] != c2; j++)
			{
				if (s[j] == '\\' && j + 1 < s.Length)
				{
					j++;
				}
			}
			if (j >= s.Length)
			{
				return false;
			}
			for (j++; j < s.Length && char.IsWhiteSpace(s[j]); j++)
			{
			}
		}
		if (j >= s.Length || s[j] != ')')
		{
			return false;
		}
		destination = stringBuilder.ToString();
		length = j - i + 1;
		return true;
	}

	private static void ProcessEmphasis(List<Node> nodes, List<Delimiter> delimiters)
	{
		int num = 0;
		while (num < delimiters.Count)
		{
			Delimiter delimiter = delimiters[num];
			if (!delimiter.canClose || delimiter.count == 0)
			{
				num++;
				continue;
			}
			int num2 = -1;
			for (int num3 = num - 1; num3 >= 0; num3--)
			{
				Delimiter delimiter2 = delimiters[num3];
				if (delimiter2.ch != delimiter.ch || !delimiter2.canOpen || delimiter2.count == 0)
				{
					continue;
				}
				if (delimiter.ch == '~')
				{
					if (delimiter2.count != delimiter.count)
					{
						continue;
					}
				}
				else if ((delimiter2.canClose || delimiter.canOpen) && (delimiter2.count + delimiter.count) % 3 == 0 && (delimiter2.count % 3 != 0 || delimiter.count % 3 != 0))
				{
					continue;
				}
				num2 = num3;
				break;
			}
			if (num2 < 0)
			{
				num++;
				continue;
			}
			Delimiter delimiter3 = delimiters[num2];
			int num4 = ((delimiter.ch == '~') ? delimiter.count : ((delimiter.count < 2 || delimiter3.count < 2) ? 1 : 2));
			char kind = ((delimiter.ch == '~') ? '~' : ((num4 == 2) ? 'S' : 'E'));
			int num5 = nodes.IndexOf(delimiter3.node);
			int num6 = nodes.IndexOf(delimiter.node);
			List<Node> range = nodes.GetRange(num5 + 1, num6 - num5 - 1);
			nodes.RemoveRange(num5 + 1, num6 - num5 - 1);
			nodes.Insert(num5 + 1, new StyleNode(kind, range));
			delimiters.RemoveRange(num2 + 1, num - num2 - 1);
			num = num2 + 1;
			delimiter3.count -= num4;
			delimiter.count -= num4;
			delimiter3.node.Text.Length = delimiter3.count;
			delimiter.node.Text.Length = delimiter.count;
			if (delimiter3.count == 0)
			{
				nodes.Remove(delimiter3.node);
				delimiters.RemoveAt(num2);
				num--;
			}
			if (delimiter.count == 0)
			{
				nodes.Remove(delimiter.node);
				delimiters.RemoveAt(num);
			}
		}
	}
}
