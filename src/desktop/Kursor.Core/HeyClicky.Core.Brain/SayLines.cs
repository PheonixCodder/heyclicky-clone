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
			if (StartsCall(code, "step", index))
			{
				index += 5;
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
			else if (StartsCall(code, "say", index))
			{
				index += 4;
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
				char c2 = code[index++];
				StringBuilder stringBuilder = new StringBuilder();
				while (index < code.Length && code[index] != c2)
				{
					if (code[index] == '\\' && index + 1 < code.Length)
					{
						char c3 = code[index + 1];
						StringBuilder stringBuilder2 = stringBuilder;
						flag = ((c3 == 'n' || c3 == 't') ? true : false);
						stringBuilder2.Append(flag ? ' ' : c3);
						index += 2;
					}
					else
					{
						stringBuilder.Append(code[index++]);
					}
				}
				index++;
				string text = stringBuilder.ToString().Trim();
				if (text.Length > 0)
				{
					onLine(arg, text);
				}
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

	private static bool StartsCall(string code, string name, int position)
	{
		if (position + name.Length + 1 > code.Length)
		{
			return false;
		}
		if (string.CompareOrdinal(code, position, name, 0, name.Length) != 0 || code[position + name.Length] != '(')
		{
			return false;
		}
		if (position > 0)
		{
			char c = code[position - 1];
			bool flag = char.IsLetterOrDigit(c);
			if (!flag)
			{
				bool flag2 = ((c == '.' || c == '_') ? true : false);
				flag = flag2;
			}
			if (flag)
			{
				return false;
			}
		}
		return true;
	}
}
