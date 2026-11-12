using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace HeyClicky.Core.Ink;

public static class InkText
{
	public static bool IsSwiftWhitespace(char c)
	{
		if (c != '\t')
		{
			return CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.SpaceSeparator;
		}
		return true;
	}

	public static bool IsSwiftNewline(char c)
	{
		switch (c)
		{
		case '\n':
		case '\v':
		case '\f':
		case '\r':
		case '\u0085':
		case '\u2028':
		case '\u2029':
			return true;
		default:
			return false;
		}
	}

	public static string TrimWs(this string s)
	{
		return TrimWhere(s, IsSwiftWhitespace);
	}

	public static string TrimWsNl(this string s)
	{
		return TrimWhere(s, (char c) => IsSwiftWhitespace(c) || IsSwiftNewline(c));
	}

	public static string TrimSet(this string s, string set)
	{
		return TrimWhere(s, (char c) => set.Contains(c));
	}

	public static string TrimWhere(string s, Func<char, bool> trims)
	{
		int i = 0;
		int num;
		for (num = s.Length; i < num && trims(s[i]); i++)
		{
		}
		while (num > i && trims(s[num - 1]))
		{
			num--;
		}
		if (i != 0 || num != s.Length)
		{
			int num2 = i;
			return s.Substring(num2, num - num2);
		}
		return s;
	}

	public static bool HasPrefix(this string s, string prefix)
	{
		return s.StartsWith(prefix, StringComparison.Ordinal);
	}

	public static bool HasSuffix(this string s, string suffix)
	{
		return s.EndsWith(suffix, StringComparison.Ordinal);
	}

	public static string Lowered(this string s)
	{
		return s.ToLowerInvariant();
	}

	public static string[] Components(this string s, string separator)
	{
		return s.Split(separator);
	}

	public static int Find(this string s, string value)
	{
		return s.IndexOf(value, StringComparison.Ordinal);
	}

	public static int FindLast(this string s, string value)
	{
		return s.LastIndexOf(value, StringComparison.Ordinal);
	}

	public static string[] Characters(this string s)
	{
		if (s.Length == 0)
		{
			return Array.Empty<string>();
		}
		List<string> list = new List<string>(s.Length);
		TextElementEnumerator textElementEnumerator = StringInfo.GetTextElementEnumerator(s);
		while (textElementEnumerator.MoveNext())
		{
			list.Add((string)textElementEnumerator.Current);
		}
		return list.ToArray();
	}

	public static int CharacterCount(this string s)
	{
		if (s.Length == 0)
		{
			return 0;
		}
		bool flag = true;
		foreach (char c in s)
		{
			bool flag2 = char.IsSurrogate(c) || c == '\r';
			if (!flag2)
			{
				UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
				bool flag3 = (((uint)(unicodeCategory - 5) <= 2u || unicodeCategory == UnicodeCategory.Format) ? true : false);
				flag2 = flag3;
			}
			if (flag2)
			{
				flag = false;
				break;
			}
		}
		if (!flag)
		{
			return new StringInfo(s).LengthInTextElements;
		}
		return s.Length;
	}

	public static string PrefixCharacters(this string s, int n)
	{
		if (n <= 0)
		{
			return "";
		}
		if (s.CharacterCount() == s.Length)
		{
			if (n < s.Length)
			{
				return s.Substring(0, n);
			}
			return s;
		}
		string[] array = s.Characters();
		if (n < array.Length)
		{
			return string.Concat(array.Take(n));
		}
		return s;
	}

	public static string SuffixCharacters(this string s, int n)
	{
		if (n <= 0)
		{
			return "";
		}
		string[] array = s.Characters();
		if (n < array.Length)
		{
			return string.Concat(array.Skip(array.Length - n));
		}
		return s;
	}

	public static bool IsLetter(string character)
	{
		if (character.Length > 0)
		{
			return Rune.IsLetter(Rune.GetRuneAt(character, 0));
		}
		return false;
	}

	public static bool IsNumber(string character)
	{
		if (character.Length > 0)
		{
			return Rune.IsNumber(Rune.GetRuneAt(character, 0));
		}
		return false;
	}

	public static bool IsWhitespace(string character)
	{
		if (character.Length > 0)
		{
			return Rune.IsWhiteSpace(Rune.GetRuneAt(character, 0));
		}
		return false;
	}

	public static bool AllWhitespace(string s)
	{
		string[] array = s.Characters();
		for (int i = 0; i < array.Length; i++)
		{
			if (!IsWhitespace(array[i]))
			{
				return false;
			}
		}
		return true;
	}

	public static List<string> SplitCharacters(this string s, Func<string, bool> isSeparator)
	{
		List<string> list = new List<string>();
		StringBuilder stringBuilder = new StringBuilder();
		string[] array = s.Characters();
		foreach (string text in array)
		{
			if (isSeparator(text))
			{
				if (stringBuilder.Length > 0)
				{
					list.Add(stringBuilder.ToString());
					stringBuilder.Clear();
				}
			}
			else
			{
				stringBuilder.Append(text);
			}
		}
		if (stringBuilder.Length > 0)
		{
			list.Add(stringBuilder.ToString());
		}
		return list;
	}

	public static List<string> SplitOmittingEmpty(this string s, char separator)
	{
		return s.Split(separator, StringSplitOptions.RemoveEmptyEntries).ToList();
	}

	public static double? ParseDouble(string? text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		int num = 0;
		bool flag = false;
		char c = text[0];
		if ((c == '+' || c == '-') ? true : false)
		{
			flag = text[0] == '-';
			num = 1;
		}
		string text2 = text;
		int num2 = num;
		string text3 = text2.Substring(num2, text2.Length - num2);
		if (text3.Length == 0)
		{
			return null;
		}
		string text4 = text3.ToLowerInvariant();
		if ((text4 == "inf" || text4 == "infinity") ? true : false)
		{
			return flag ? double.NegativeInfinity : double.PositiveInfinity;
		}
		if (text4 == "nan")
		{
			return double.NaN;
		}
		if (text4.HasPrefix("0x"))
		{
			text2 = text4;
			double? num3 = ParseHexDouble(text2.Substring(2, text2.Length - 2));
			if (num3.HasValue)
			{
				double valueOrDefault = num3.GetValueOrDefault();
				return flag ? (0.0 - valueOrDefault) : valueOrDefault;
			}
			return null;
		}
		int num4 = 0;
		int num5 = 0;
		while (num4 < text3.Length)
		{
			c = text3[num4];
			if (c < '0' || c > '9')
			{
				break;
			}
			num4++;
			num5++;
		}
		if (num4 < text3.Length && text3[num4] == '.')
		{
			num4++;
			while (num4 < text3.Length)
			{
				c = text3[num4];
				if (c < '0' || c > '9')
				{
					break;
				}
				num4++;
				num5++;
			}
		}
		if (num5 == 0)
		{
			return null;
		}
		bool flag2 = num4 < text3.Length;
		if (flag2)
		{
			c = text3[num4];
			bool flag3 = ((c == 'E' || c == 'e') ? true : false);
			flag2 = flag3;
		}
		if (flag2)
		{
			num4++;
			flag2 = num4 < text3.Length;
			if (flag2)
			{
				c = text3[num4];
				bool flag3 = ((c == '+' || c == '-') ? true : false);
				flag2 = flag3;
			}
			if (flag2)
			{
				num4++;
			}
			int num6 = 0;
			while (num4 < text3.Length)
			{
				c = text3[num4];
				if (c < '0' || c > '9')
				{
					break;
				}
				num4++;
				num6++;
			}
			if (num6 == 0)
			{
				return null;
			}
		}
		if (num4 != text3.Length)
		{
			return null;
		}
		if (!double.TryParse(text3, NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var result))
		{
			return null;
		}
		return flag ? (0.0 - result) : result;
	}

	private static double? ParseHexDouble(string s)
	{
		if (s.Length == 0)
		{
			return null;
		}
		double num = 0.0;
		int i = 0;
		int num2 = 0;
		int num3 = 0;
		bool flag = false;
		for (; i < s.Length; i++)
		{
			char c = s[i];
			if (c == '.' && !flag)
			{
				flag = true;
				continue;
			}
			int num4 = ((c >= '0' && c <= '9') ? (c - 48) : ((c >= 'a' && c <= 'f') ? (c - 97 + 10) : (-1)));
			if (num4 < 0)
			{
				break;
			}
			num = num * 16.0 + (double)num4;
			num2++;
			if (flag)
			{
				num3++;
			}
		}
		if (num2 == 0)
		{
			return null;
		}
		int num5 = 0;
		if (i < s.Length)
		{
			if (s[i] != 'p')
			{
				return null;
			}
			i++;
			int num6 = 1;
			bool flag2 = i < s.Length;
			if (flag2)
			{
				char c2 = s[i];
				bool flag3 = ((c2 == '+' || c2 == '-') ? true : false);
				flag2 = flag3;
			}
			if (flag2)
			{
				num6 = ((s[i] != '-') ? 1 : (-1));
				i++;
			}
			int num7 = 0;
			for (; i < s.Length; i++)
			{
				char c2 = s[i];
				if (c2 < '0' || c2 > '9')
				{
					break;
				}
				num5 = num5 * 10 + (s[i] - 48);
				num7++;
			}
			if (num7 == 0 || i != s.Length)
			{
				return null;
			}
			num5 *= num6;
		}
		return num * Math.Pow(2.0, num5 - 4 * num3);
	}

	public static long? ParseInt(string? text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		char c = text[0];
		bool flag = ((c == '+' || c == '-') ? true : false);
		int num = (flag ? 1 : 0);
		if (num == text.Length)
		{
			return null;
		}
		for (int i = num; i < text.Length; i++)
		{
			c = text[i];
			if ((c < '0' || c > '9') ? true : false)
			{
				return null;
			}
		}
		if (!long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var result))
		{
			return null;
		}
		return result;
	}

	public static string DoubleDescription(double value)
	{
		if (double.IsNaN(value))
		{
			return "nan";
		}
		if (double.IsPositiveInfinity(value))
		{
			return "inf";
		}
		if (double.IsNegativeInfinity(value))
		{
			return "-inf";
		}
		string text = value.ToString("R", CultureInfo.InvariantCulture);
		if (text.Contains('E'))
		{
			return text.Replace('E', 'e');
		}
		if (!text.Contains('.'))
		{
			return text + ".0";
		}
		return text;
	}

	public static long InkClamping(double value)
	{
		if (!double.IsFinite(value))
		{
			if (!double.IsNaN(value))
			{
				if (!(value > 0.0))
				{
					return -1000000000000000L;
				}
				return 1000000000000000L;
			}
			return 0L;
		}
		return (long)Math.Min(Math.Max(value, -1000000000000000.0), 1000000000000000.0);
	}
}
