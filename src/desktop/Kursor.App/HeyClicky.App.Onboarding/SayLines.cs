using System;
using System.Collections.Generic;
using System.Text;

namespace HeyClicky.App.Onboarding;

public static class SayLines
{
	public static Dictionary<int, string> ByStep(string code)
	{
		Dictionary<int, string> dictionary = new Dictionary<int, string>();
		List<string> list = new List<string>();
		int? num = null;
		int i = 0;
		while (i < code.Length)
		{
			if (Starts("step"))
			{
				i += 5;
				SkipSpaces();
				int num2 = i;
				for (; i < code.Length && char.IsDigit(code[i]); i++)
				{
				}
				if (int.TryParse(code.AsSpan(num2, i - num2), out var result))
				{
					num = result;
				}
			}
			else if (Starts("say"))
			{
				i += 4;
				SkipSpaces();
				bool flag = i >= code.Length;
				if (!flag)
				{
					char c = code[i];
					bool flag2 = ((c == '"' || c == '\'' || c == '`') ? true : false);
					flag = !flag2;
				}
				if (flag)
				{
					continue;
				}
				char c2 = code[i++];
				StringBuilder stringBuilder = new StringBuilder();
				while (i < code.Length && code[i] != c2)
				{
					if (code[i] == '\\' && i + 1 < code.Length)
					{
						char c3 = code[i + 1];
						StringBuilder stringBuilder2 = stringBuilder;
						flag = ((c3 == 'n' || c3 == 't') ? true : false);
						stringBuilder2.Append(flag ? ' ' : c3);
						i += 2;
					}
					else
					{
						stringBuilder.Append(code[i++]);
					}
				}
				i++;
				string text = stringBuilder.ToString().Trim();
				if (text.Length == 0)
				{
					continue;
				}
				list.Add(text);
				if (num.HasValue)
				{
					int valueOrDefault = num.GetValueOrDefault();
					if (!dictionary.ContainsKey(valueOrDefault))
					{
						dictionary[valueOrDefault] = text;
					}
				}
			}
			else
			{
				i++;
			}
		}
		if (dictionary.Count == 0)
		{
			for (int j = 0; j < list.Count; j++)
			{
				dictionary[j + 1] = list[j];
			}
		}
		return dictionary;
		void SkipSpaces()
		{
			for (; i < code.Length && char.IsWhiteSpace(code[i]); i++)
			{
			}
		}
		bool Starts(string name)
		{
			string text2 = name + "(";
			if (i + text2.Length > code.Length || string.CompareOrdinal(code, i, text2, 0, text2.Length) != 0)
			{
				return false;
			}
			if (i > 0)
			{
				char c4 = code[i - 1];
				if (char.IsLetterOrDigit(c4) || c4 == '_' || c4 == '.')
				{
					return false;
				}
			}
			return true;
		}
	}
}
