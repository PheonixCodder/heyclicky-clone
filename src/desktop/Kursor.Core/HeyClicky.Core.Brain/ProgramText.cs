using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;

namespace HeyClicky.Core.Brain;

public static class ProgramText
{
	public const string BlockEndMarker = "\n;";

	private static readonly (Regex Pattern, string Replacement)[] LinkPatterns = new(Regex, string)[3]
	{
		(CitationPattern(), ""),
		(MarkdownLinkPattern(), "$1"),
		(BareUrlPattern(), "")
	};

	public static string WithPlainFences(string text)
	{
		if (!text.Contains("\\`"))
		{
			return text;
		}
		return text.Replace("\\`", "`");
	}

	public static string ExtractProgramCode(string rawText)
	{
		string text = WithPlainFences(rawText);
		int num = text.IndexOf("```", StringComparison.Ordinal);
		if (num >= 0)
		{
			int num2 = num + 3;
			int num3 = text.IndexOf('\n', num2);
			int num4 = ((num3 >= 0) ? (num3 + 1) : num2);
			int num5 = text.IndexOf("```", num4, StringComparison.Ordinal);
			int num6 = num4;
			return text.Substring(num6, ((num5 >= 0) ? num5 : text.Length) - num6).Trim();
		}
		if (!text.Contains("step(") && !text.Contains("say("))
		{
			return "";
		}
		return text.Trim();
	}

	public static string? ProgramCode(string rawText, bool isComplete)
	{
		string text = WithPlainFences(rawText);
		List<string> pieces = new List<string>(4);
		bool flag = false;
		int num = 0;
		while (true)
		{
			int num2 = text.IndexOf("```", num, StringComparison.Ordinal);
			if (num2 < 0)
			{
				break;
			}
			AddOutside(text.AsSpan(num, num2 - num), closed: true);
			int startIndex = num2 + 3;
			int num3 = text.IndexOf('\n', startIndex);
			if (num3 < 0)
			{
				num = text.Length;
				break;
			}
			flag = true;
			int num4 = num3 + 1;
			int num5 = text.IndexOf("```", num4, StringComparison.Ordinal);
			int num6;
			if (num5 < 0)
			{
				string text2 = text;
				num6 = num4;
				string text3 = text2.Substring(num6, text2.Length - num6).Trim('\n', '\r');
				pieces.Add(isComplete ? text3 : text3.TrimEnd('`').TrimEnd('\n', '\r'));
				num = text.Length;
				break;
			}
			num6 = num4;
			string text4 = text.Substring(num6, num5 - num6).Trim('\n', '\r');
			if (!string.IsNullOrWhiteSpace(text4))
			{
				pieces.Add(text4 + "\n;");
			}
			num = num5 + 3;
		}
		if (num < text.Length)
		{
			AddOutside(text.AsSpan(num), closed: false);
		}
		string text5 = string.Join("\n", pieces.Where((string p) => p.Length > 0));
		if (isComplete)
		{
			if (text5.Length != 0 || flag)
			{
				return text5.Trim();
			}
			return ExtractProgramCode(rawText);
		}
		if (text5.Length != 0)
		{
			return text5;
		}
		return null;
		void AddOutside(ReadOnlySpan<char> segment, bool closed)
		{
			ReadOnlySpan<char> span = segment.Trim();
			if (!isComplete)
			{
				span = span.TrimEnd('`').Trim();
			}
			if (span.IndexOf("say(", StringComparison.Ordinal) >= 0)
			{
				pieces.Add(span.ToString() + (closed ? "\n;" : ""));
			}
		}
	}

	public static string WithoutLinks(string code)
	{
		if (!code.Contains("http", StringComparison.Ordinal))
		{
			return code;
		}
		string text = code;
		(Regex, string)[] linkPatterns = LinkPatterns;
		for (int i = 0; i < linkPatterns.Length; i++)
		{
			(Regex, string) tuple = linkPatterns[i];
			Regex item = tuple.Item1;
			string item2 = tuple.Item2;
			List<(int Start, int Length)> stringRanges = StringLiteralRanges(text);
			List<Match> list = (from m in item.Matches(text)
				where !stringRanges.Any(((int Start, int Length) r) => r.Start < m.Index + m.Length && m.Index < r.Start + r.Length)
				select m).ToList();
			if (list.Count != 0)
			{
				StringBuilder stringBuilder = new StringBuilder(text);
				for (int num = list.Count - 1; num >= 0; num--)
				{
					Match match = list[num];
					stringBuilder.Remove(match.Index, match.Length).Insert(match.Index, match.Result(item2));
				}
				text = stringBuilder.ToString();
			}
		}
		return text;
	}

	// [GeneratedRegex("\\s*\\(\\[[^\\]\\n]{1,80}\\]\\(https?://[^)\\s\"]+\\)\\)")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex CitationPattern()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__CitationPattern_0.Instance;
	}

	// [GeneratedRegex("\\[([^\\]\\n]{1,80})\\]\\(https?://[^)\\s\"]+\\)")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex MarkdownLinkPattern()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__MarkdownLinkPattern_1.Instance;
	}

	// [GeneratedRegex("\\s*https?://[^\\s\"'),]+")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex BareUrlPattern()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__BareUrlPattern_2.Instance;
	}

	public static List<(int Start, int Length)> StringLiteralRanges(string code)
	{
		List<(int, int)> list = new List<(int, int)>();
		int i = 0;
		while (i < code.Length)
		{
			char c = code[i];
			if (c == '/' && i + 1 < code.Length && code[i + 1] == '/' && (i == 0 || code[i - 1] != ':'))
			{
				for (; i < code.Length && code[i] != '\n'; i++)
				{
				}
			}
			else if (c == '/' && i + 1 < code.Length && code[i + 1] == '*')
			{
				for (i += 2; i + 1 < code.Length && (code[i] != '*' || code[i + 1] != '/'); i++)
				{
				}
				i += 2;
			}
			else if ((c == '"' || c == '\'' || c == '`') ? true : false)
			{
				int num = i;
				for (i++; i < code.Length && code[i] != c && (c == '`' || code[i] != '\n'); i += ((code[i] != '\\') ? 1 : 2))
				{
				}
				int num2 = Math.Min(i + 1, code.Length);
				list.Add((num, num2 - num));
				i = num2;
			}
			else
			{
				i++;
			}
		}
		return list;
	}

	public static string? FirstSayLiteral(string code)
	{
		int startIndex = 0;
		int num;
		do
		{
			num = code.IndexOf("say(", startIndex, StringComparison.Ordinal);
			if (num < 0)
			{
				return null;
			}
			startIndex = num + 4;
		}
		while (!StartsStatementOnItsLine(code, num));
		int num2 = num + 4;
		bool flag;
		while (true)
		{
			flag = num2 < code.Length;
			if (flag)
			{
				char c = code[num2];
				bool flag2 = ((c == '\t' || c == ' ') ? true : false);
				flag = flag2;
			}
			if (!flag)
			{
				break;
			}
			num2++;
		}
		if (num2 >= code.Length)
		{
			return null;
		}
		char c2 = code[num2];
		if ((c2 != '"' && c2 != '\'' && c2 != '`') || 1 == 0)
		{
			return null;
		}
		int num3 = num2 + 1;
		int i;
		for (i = num3; i < code.Length && code[i] != c2; i++)
		{
			if (code[i] == '\\' || (c2 != '`' && code[i] == '\n'))
			{
				return null;
			}
			if (c2 == '`' && code[i] == '$' && i + 1 < code.Length && code[i + 1] == '{')
			{
				return null;
			}
		}
		if (i >= code.Length)
		{
			return null;
		}
		int num4 = i + 1;
		while (true)
		{
			flag = num4 < code.Length;
			if (flag)
			{
				char c = code[num4];
				bool flag2 = ((c == '\t' || c == ' ') ? true : false);
				flag = flag2;
			}
			if (!flag)
			{
				break;
			}
			num4++;
		}
		flag = num4 >= code.Length;
		if (!flag)
		{
			char c = code[num4];
			bool flag2 = ((c == ')' || c == ',') ? true : false);
			flag = !flag2;
		}
		if (flag)
		{
			return null;
		}
		int num5 = num3;
		string text = code.Substring(num5, i - num5).Trim();
		if (text.Length != 0)
		{
			return text;
		}
		return null;
	}

	private static bool StartsStatementOnItsLine(string code, int index)
	{
		int num = ((index != 0) ? (code.LastIndexOf('\n', index - 1) + 1) : 0);
		ReadOnlySpan<char> span = code.AsSpan(num, index - num).TrimEnd();
		if (span.Length == 0)
		{
			return true;
		}
		char c = span[span.Length - 1];
		if ((c == ';' || c == '{' || c == '}') ? true : false)
		{
			return span.IndexOfAny("\"'`/") < 0;
		}
		return false;
	}
}
