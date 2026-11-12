using System;
using System.Globalization;
using System.Text;

namespace HeyClicky.Core.Documents.Spreadsheet;

public static class SwiftText
{
	public static double? ParseDouble(string? text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		ReadOnlySpan<char> readOnlySpan = text.AsSpan();
		int num = 0;
		bool flag = false;
		char c = readOnlySpan[0];
		if ((c == '+' || c == '-') ? true : false)
		{
			flag = readOnlySpan[0] == '-';
			num = 1;
		}
		ref ReadOnlySpan<char> reference = ref readOnlySpan;
		int num2 = num;
		ReadOnlySpan<char> readOnlySpan2 = reference.Slice(num2, reference.Length - num2);
		if (readOnlySpan2.Length == 0)
		{
			return null;
		}
		if (MemoryExtensions.Equals(readOnlySpan2, "inf", StringComparison.OrdinalIgnoreCase) || MemoryExtensions.Equals(readOnlySpan2, "infinity", StringComparison.OrdinalIgnoreCase))
		{
			return flag ? double.NegativeInfinity : double.PositiveInfinity;
		}
		if (readOnlySpan2.StartsWith("nan", StringComparison.OrdinalIgnoreCase))
		{
			if (readOnlySpan2.Length == 3)
			{
				return double.NaN;
			}
			if (readOnlySpan2[3] == '(')
			{
				if (readOnlySpan2[readOnlySpan2.Length - 1] == ')')
				{
					reference = ref readOnlySpan2;
					ReadOnlySpan<char> readOnlySpan3 = reference.Slice(4, reference.Length - 1 - 4);
					for (num2 = 0; num2 < readOnlySpan3.Length; num2++)
					{
						char c2 = readOnlySpan3[num2];
						if (!char.IsAsciiLetterOrDigit(c2) && c2 != '_')
						{
							return null;
						}
					}
					return double.NaN;
				}
			}
			return null;
		}
		bool flag2 = readOnlySpan2.Length > 2 && readOnlySpan2[0] == '0';
		if (flag2)
		{
			c = readOnlySpan2[1];
			bool flag3 = ((c == 'X' || c == 'x') ? true : false);
			flag2 = flag3;
		}
		if (flag2)
		{
			reference = ref readOnlySpan2;
			return ParseHex(reference.Slice(2, reference.Length - 2), flag);
		}
		int num3 = 0;
		int num4 = 0;
		while (num3 < readOnlySpan2.Length && char.IsAsciiDigit(readOnlySpan2[num3]))
		{
			num3++;
			num4++;
		}
		if (num3 < readOnlySpan2.Length && readOnlySpan2[num3] == '.')
		{
			num3++;
			while (num3 < readOnlySpan2.Length && char.IsAsciiDigit(readOnlySpan2[num3]))
			{
				num3++;
				num4++;
			}
		}
		if (num4 == 0)
		{
			return null;
		}
		flag2 = num3 < readOnlySpan2.Length;
		if (flag2)
		{
			c = readOnlySpan2[num3];
			bool flag3 = ((c == 'E' || c == 'e') ? true : false);
			flag2 = flag3;
		}
		if (flag2)
		{
			num3++;
			flag2 = num3 < readOnlySpan2.Length;
			if (flag2)
			{
				c = readOnlySpan2[num3];
				bool flag3 = ((c == '+' || c == '-') ? true : false);
				flag2 = flag3;
			}
			if (flag2)
			{
				num3++;
			}
			int num5 = 0;
			while (num3 < readOnlySpan2.Length && char.IsAsciiDigit(readOnlySpan2[num3]))
			{
				num3++;
				num5++;
			}
			if (num5 == 0)
			{
				return null;
			}
		}
		if (num3 != readOnlySpan2.Length)
		{
			return null;
		}
		if (!double.TryParse(readOnlySpan2, NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var result))
		{
			return null;
		}
		return flag ? (0.0 - result) : result;
	}

	private static double? ParseHex(ReadOnlySpan<char> s, bool negative)
	{
		double num = 0.0;
		int num2 = 0;
		int num3 = 0;
		int i = 0;
		bool flag = false;
		for (; i < s.Length; i++)
		{
			char c = s[i];
			if (c == '.' && !flag)
			{
				flag = true;
				continue;
			}
			int num4 = HexValue(c);
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
		bool flag2 = i < s.Length;
		if (flag2)
		{
			char c2 = s[i];
			bool flag3 = ((c2 == 'P' || c2 == 'p') ? true : false);
			flag2 = flag3;
		}
		if (flag2)
		{
			i++;
			bool flag4 = false;
			flag2 = i < s.Length;
			if (flag2)
			{
				char c2 = s[i];
				bool flag3 = ((c2 == '+' || c2 == '-') ? true : false);
				flag2 = flag3;
			}
			if (flag2)
			{
				flag4 = s[i] == '-';
				i++;
			}
			int num6 = 0;
			while (i < s.Length && char.IsAsciiDigit(s[i]))
			{
				num5 = Math.Min(100000, num5 * 10 + (s[i] - 48));
				i++;
				num6++;
			}
			if (num6 == 0)
			{
				return null;
			}
			if (flag4)
			{
				num5 = -num5;
			}
		}
		if (i != s.Length)
		{
			return null;
		}
		double num7 = num * Math.Pow(2.0, num5 - 4 * num3);
		return negative ? (0.0 - num7) : num7;
	}

	private static int HexValue(char ch)
	{
		switch (ch)
		{
		case '0':
		case '1':
		case '2':
		case '3':
		case '4':
		case '5':
		case '6':
		case '7':
		case '8':
		case '9':
			return ch - 48;
		case 'a':
		case 'b':
		case 'c':
		case 'd':
		case 'e':
		case 'f':
			return ch - 97 + 10;
		case 'A':
		case 'B':
		case 'C':
		case 'D':
		case 'E':
		case 'F':
			return ch - 65 + 10;
		default:
			return -1;
		}
	}

	public static int CharacterCount(string text)
	{
		if (text.Length == 0)
		{
			return 0;
		}
		bool flag = true;
		foreach (char c in text)
		{
			if (c > '\u02ff' || char.IsSurrogate(c))
			{
				flag = false;
				break;
			}
		}
		if (flag)
		{
			return text.Replace("\r\n", "\n").Length;
		}
		return new StringInfo(text).LengthInTextElements;
	}

	public static bool IsWhitespace(char ch)
	{
		if (ch != '\t')
		{
			return CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.SpaceSeparator;
		}
		return true;
	}

	public static string TrimWhitespace(string text)
	{
		int i = 0;
		int num;
		for (num = text.Length; i < num && IsWhitespace(text[i]); i++)
		{
		}
		while (num > i && IsWhitespace(text[num - 1]))
		{
			num--;
		}
		if (i != 0 || num != text.Length)
		{
			int num2 = i;
			return text.Substring(num2, num - num2);
		}
		return text;
	}

	public static string TrimWhitespaceAndNewlines(string text)
	{
		int i = 0;
		int num;
		for (num = text.Length; i < num && Is(text[i]); i++)
		{
		}
		while (num > i && Is(text[num - 1]))
		{
			num--;
		}
		int num2 = i;
		return text.Substring(num2, num - num2);
		static bool Is(char ch)
		{
			bool flag = IsWhitespace(ch);
			if (!flag)
			{
				bool flag2;
				switch (ch)
				{
				case '\n':
				case '\v':
				case '\f':
				case '\r':
				case '\u0085':
					flag2 = true;
					break;
				default:
					flag2 = false;
					break;
				}
				flag = flag2;
			}
			if (!flag && ch != '\u2028')
			{
				return ch == '\u2029';
			}
			return true;
		}
	}

	public static int? ParseInt(string? text)
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
			if (!char.IsAsciiDigit(text[i]))
			{
				return null;
			}
		}
		if (!int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var result))
		{
			return null;
		}
		return result;
	}

	public static string? DecodeText(byte[] data)
	{
		try
		{
			return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(data);
		}
		catch (DecoderFallbackException)
		{
		}
		if (data.Length % 2 == 0 && data.Length != 0)
		{
			bool flag = data.Length >= 2 && data[0] == byte.MaxValue && data[1] == 254;
			try
			{
				return new UnicodeEncoding(!flag, byteOrderMark: false, throwOnInvalidBytes: true).GetString(data);
			}
			catch (DecoderFallbackException)
			{
			}
		}
		try
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			return Encoding.GetEncoding(1252, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback).GetString(data);
		}
		catch (Exception)
		{
			return null;
		}
	}
}
