using System;
using System.Collections.Generic;
using System.Text;

namespace HeyClicky.Core.Brain;

public static class SayLines
{
	public static List<string> InOrder(string code)
	{
		List<string> lines = new List<string>();
		Scan(code, delegate(int? _, string line)
		{
			lines.Add(line);
		});
		return lines;
	}

	public static Dictionary<int, string> ByStep(string code)
	{
		Dictionary<int, string> byStep = new Dictionary<int, string>();
		List<string> inOrder = new List<string>();
		Scan(code, delegate(int? step, string line)
		{
			inOrder.Add(line);
			if (step.HasValue)
			{
				int valueOrDefault = step.GetValueOrDefault();
				byStep.TryAdd(valueOrDefault, line);
			}
		});
		if (byStep.Count < inOrder.Count && byStep.Count == 0)
		{
			for (int i = 0; i < inOrder.Count; i++)
			{
				byStep[i + 1] = inOrder[i];
			}
		}
		return byStep;
	}

	private static void Scan(string code, Action<int?, string> onLine)
	{
		int index = 0;
		int? arg = null;
		while (index < code.Length)
		{
			if (code[index] == '/' && index + 1 < code.Length && code[index + 1] == '/')
			{
				index += 2;
				while (index < code.Length && code[index] != '\n' && code[index] != '\r')
				{
					index++;
				}
				continue;
			}
			if (code[index] == '/' && index + 1 < code.Length && code[index + 1] == '*')
			{
				index += 2;
				while (index + 1 < code.Length && !(code[index] == '*' && code[index + 1] == '/'))
				{
					index++;
				}
				index = Math.Min(code.Length, index + 2);
				continue;
			}
			if (StartsCall(code, "step", index, out var stepParenIndex))
			{
				index = stepParenIndex + 1;
				SkipSpaces();
				int num = index;
				for (; index < code.Length && char.IsDigit(code[index]); index++)
				{
				}
				if (int.TryParse(code.AsSpan(num, index - num), out var result))
				{
					arg = result;
				}
			}
			else if (StartsCall(code, "say", index, out var sayParenIndex))
			{
				index = sayParenIndex + 1;
				SkipSpaces();
				bool flag = index >= code.Length;
				if (!flag)
				{
					char c = code[index];
					bool flag2 = ((c == '"' || c == '\'' || c == '`') ? true : false);
					flag = !flag2;
				}
				if (flag)
				{
					continue;
				}
				char quote = code[index++];
				string raw = ReadQuotedLiteral(code, ref index, quote);
				string text = Unescape(raw).Trim();
				if (text.Length > 0)
				{
					onLine(arg, text);
				}
			}
			else if (code[index] == '"' || code[index] == '\'' || code[index] == '`')
			{
				char quote = code[index++];
				_ = ReadQuotedLiteral(code, ref index, quote);
			}
			else
			{
				index++;
			}
		}
		void SkipSpaces()
		{
			for (; index < code.Length && char.IsWhiteSpace(code[index]); index++)
			{
			}
		}
	}

	private static string ReadQuotedLiteral(string code, ref int index, char quote)
	{
		int start = index;
		while (index < code.Length && code[index] != quote)
		{
			if (code[index] == '\\' && index + 1 < code.Length)
			{
				index += 2;
			}
			else
			{
				index++;
			}
		}
		string raw = code.Substring(start, index - start);
		if (index < code.Length && code[index] == quote)
		{
			index++;
		}
		return raw;
	}

	private static string Unescape(string raw)
	{
		StringBuilder stringBuilder = new StringBuilder(raw.Length);
		int i = 0;
		while (i < raw.Length)
		{
			if (raw[i] == '\\' && i + 1 < raw.Length)
			{
				char c = raw[i + 1];
				switch (c)
				{
				case 'n':
				case 't':
					stringBuilder.Append(' ');
					i += 2;
					break;
				case 'r':
					if (i + 3 < raw.Length && raw[i + 2] == '\\' && raw[i + 3] == 'n')
					{
						stringBuilder.Append(' ');
						i += 4;
					}
					else
					{
						stringBuilder.Append('\r');
						i += 2;
					}
					break;
				case 'u':
					if (i + 5 < raw.Length && IsHexQuad(raw, i + 2))
					{
						string hex = raw.Substring(i + 2, 4);
						stringBuilder.Append(char.ConvertFromUtf32(Convert.ToInt32(hex, 16)));
						i += 6;
					}
					else
					{
						stringBuilder.Append(c);
						i += 2;
					}
					break;
				default:
					stringBuilder.Append(c);
					i += 2;
					break;
				}
			}
			else
			{
				stringBuilder.Append(raw[i++]);
			}
		}
		return stringBuilder.ToString();
	}

	private static bool IsHexQuad(string s, int offset)
	{
		for (int i = 0; i < 4; i++)
		{
			if (!Uri.IsHexDigit(s[offset + i]))
			{
				return false;
			}
		}
		return true;
	}

	private static bool StartsCall(string code, string name, int position, out int openParenIndex)
	{
		openParenIndex = -1;
		if (position + name.Length + 1 > code.Length)
		{
			return false;
		}
		if (string.CompareOrdinal(code, position, name, 0, name.Length) != 0)
		{
			return false;
		}
		if (position > 0)
		{
			char c = code[position - 1];
			bool flag = char.IsLetterOrDigit(c) || c == '.' || c == '_' || c == '$';
			if (flag)
			{
				return false;
			}
		}
		int i = position + name.Length;
		while (i < code.Length && char.IsWhiteSpace(code[i]))
		{
			i++;
		}
		if (i >= code.Length || code[i] != '(')
		{
			return false;
		}
		openParenIndex = i;
		return true;
	}
}
