using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;

namespace HeyClicky.Core.Documents.Spreadsheet;

public static class SpreadsheetNumberFormat
{
	internal readonly record struct CalendarDate(long Year, int Month, int Day, int DayOfWeek, int Hour, int Minute, int Second)
	{
		private const long ExcelEpochJulianDay = 2415019L;

		private const long GregorianCutoverJulianDay = 2299161L;

		public static CalendarDate? FromExcelSeconds(double totalSeconds)
		{
			if (double.IsNaN(totalSeconds) || Math.Abs(totalSeconds) > 1000000000000000.0)
			{
				return null;
			}
			long result;
			long num = Math.DivRem((long)totalSeconds, 86400L, out result);
			if (result < 0)
			{
				result += 86400;
				num--;
			}
			long num2 = 2415019 + num;
			long num3;
			int month;
			int day;
			if (num2 >= 2299161)
			{
				(num3, month, day) = GregorianFromJulianDay(num2);
			}
			else
			{
				(num3, month, day) = JulianFromJulianDay(num2);
			}
			int dayOfWeek = (int)(((num2 + 1) % 7 + 7) % 7);
			return new CalendarDate((num3 > 0) ? num3 : (1 - num3), month, day, dayOfWeek, (int)(result / 3600), (int)(result / 60 % 60), (int)(result % 60));
		}

		private static (long Year, int Month, int Day) GregorianFromJulianDay(long jd)
		{
			long num = jd + 32044;
			long num2 = FloorDiv(4 * num + 3, 146097L);
			return FromDayOfFourYears(num - FloorDiv(146097 * num2, 4L), 100 * num2);
		}

		private static (long Year, int Month, int Day) JulianFromJulianDay(long jd)
		{
			return FromDayOfFourYears(jd + 32082, 0L);
		}

		private static (long Year, int Month, int Day) FromDayOfFourYears(long c, long centuries)
		{
			long num = FloorDiv(4 * c + 3, 1461L);
			long num2 = c - FloorDiv(1461 * num, 4L);
			long num3 = FloorDiv(5 * num2 + 2, 153L);
			int item = (int)(num2 - FloorDiv(153 * num3 + 2, 5L) + 1);
			int item2 = (int)(num3 + 3 - 12 * FloorDiv(num3, 10L));
			return (Year: centuries + num - 4800 + FloorDiv(num3, 10L), Month: item2, Day: item);
		}

		private static long FloorDiv(long a, long b)
		{
			long num = a / b;
			if (a % b != 0L && a < 0 != b < 0)
			{
				num--;
			}
			return num;
		}
	}

	private const char LiteralMarker = '\u0001';

	private const char ElapsedMarker = '\u0002';

	private static readonly string[] MonthNames = CultureInfo.InvariantCulture.DateTimeFormat.MonthNames;

	private static readonly string[] MonthAbbreviations = CultureInfo.InvariantCulture.DateTimeFormat.AbbreviatedMonthNames;

	private static readonly string[] DayNames = CultureInfo.InvariantCulture.DateTimeFormat.DayNames;

	private static readonly string[] DayAbbreviations = CultureInfo.InvariantCulture.DateTimeFormat.AbbreviatedDayNames;

	public static string? BuiltInFormatCode(int id)
	{
		return id switch
		{
			0 => "General", 
			1 => "0", 
			2 => "0.00", 
			3 => "#,##0", 
			4 => "#,##0.00", 
			5 => "$#,##0;($#,##0)", 
			6 => "$#,##0;[Red]($#,##0)", 
			7 => "$#,##0.00;($#,##0.00)", 
			8 => "$#,##0.00;[Red]($#,##0.00)", 
			9 => "0%", 
			10 => "0.00%", 
			11 => "0.00E+00", 
			12 => "# ?/?", 
			13 => "# ??/??", 
			14 => "m/d/yyyy", 
			15 => "d-mmm-yy", 
			16 => "d-mmm", 
			17 => "mmm-yy", 
			18 => "h:mm AM/PM", 
			19 => "h:mm:ss AM/PM", 
			20 => "h:mm", 
			21 => "h:mm:ss", 
			22 => "m/d/yyyy h:mm", 
			37 => "#,##0 ;(#,##0)", 
			38 => "#,##0 ;[Red](#,##0)", 
			39 => "#,##0.00;(#,##0.00)", 
			40 => "#,##0.00;[Red](#,##0.00)", 
			44 => "$#,##0.00", 
			45 => "mm:ss", 
			46 => "[h]:mm:ss", 
			47 => "mm:ss.0", 
			48 => "##0.0E+0", 
			49 => "@", 
			_ => null, 
		};
	}

	public static string Format(double value, string rawCode)
	{
		if (!double.IsFinite(value))
		{
			return "#NUM!";
		}
		List<string> list = SplitSections(rawCode);
		string section = ((list.Count > 0) ? list[0] : "General");
		double num = value;
		if (value < 0.0 && list.Count > 1)
		{
			section = list[1];
			num = 0.0 - value;
		}
		else if (value == 0.0 && list.Count > 2)
		{
			section = list[2];
		}
		string text = CleanedSection(section);
		if (text.Length == 0 || text.Equals("General", StringComparison.OrdinalIgnoreCase) || text == "@")
		{
			return General(value);
		}
		if (IsDateFormat(text))
		{
			return FormatDate(num, text);
		}
		string text2 = FormatNumber(num, text);
		if (value < 0.0 && list.Count == 1 && !text2.StartsWith('-'))
		{
			return "-" + text2;
		}
		return text2;
	}

	public static string General(double value)
	{
		if (value == Math.Round(value) && Math.Abs(value) < 1000000000000000.0)
		{
			return ((long)value).ToString(CultureInfo.InvariantCulture);
		}
		double num = Math.Abs(value);
		if (num >= 100000000000.0 || num < 1E-09)
		{
			return TrailingZerosBeforeExponent().Replace(PrintfE(value, 5), "E");
		}
		return value.ToString("G10", CultureInfo.InvariantCulture).Replace("E", "e");
	}

	// [GeneratedRegex("\\.?0+E")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex TrailingZerosBeforeExponent()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__TrailingZerosBeforeExponent_35.Instance;
	}

	private static string PrintfE(double value, int decimals)
	{
		string text = ((decimals > 0) ? ("0." + new string('0', decimals)) : "0") + "E+00";
		return value.ToString(text, CultureInfo.InvariantCulture);
	}

	private static List<string> SplitSections(string code)
	{
		List<string> list = new List<string>();
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = false;
		bool flag2 = false;
		foreach (char c in code)
		{
			if (flag2)
			{
				stringBuilder.Append(c);
				flag2 = false;
				continue;
			}
			switch (c)
			{
			case '\\':
				stringBuilder.Append(c);
				flag2 = true;
				continue;
			case '"':
				flag = !flag;
				break;
			}
			if (c == ';' && !flag)
			{
				list.Add(stringBuilder.ToString());
				stringBuilder.Clear();
			}
			else
			{
				stringBuilder.Append(c);
			}
		}
		list.Add(stringBuilder.ToString());
		return list;
	}

	private static string CleanedSection(string section)
	{
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		while (num < section.Length)
		{
			char c = section[num];
			switch (c)
			{
			case '[':
			{
				int i;
				for (i = num + 1; i < section.Length && section[i] != ']'; i++)
				{
				}
				int num2 = num + 1;
				string text = section.Substring(num2, Math.Min(i, section.Length) - num2);
				if (text.StartsWith('$'))
				{
					string text2 = text;
					string text3 = SwiftSplitFirst(text2.Substring(1, text2.Length - 1), '-') ?? "";
					stringBuilder.Append(Literal(text3));
				}
				else
				{
					string lowered = text.ToLowerInvariant();
					if (lowered.Length > 0 && "hms".Contains(lowered[0]) && lowered.All((char ch) => ch == lowered[0]))
					{
						stringBuilder.Append('\u0002').Append(lowered);
					}
				}
				num = i + 1;
				break;
			}
			case '"':
			{
				int j = num + 1;
				StringBuilder stringBuilder2 = new StringBuilder();
				for (; j < section.Length && section[j] != '"'; j++)
				{
					stringBuilder2.Append(section[j]);
				}
				stringBuilder.Append(Literal(stringBuilder2.ToString()));
				num = j + 1;
				break;
			}
			case '\\':
				if (num + 1 < section.Length)
				{
					stringBuilder.Append(Literal(section[num + 1].ToString()));
				}
				num += 2;
				break;
			case '*':
			case '_':
				num += 2;
				break;
			default:
				stringBuilder.Append(c);
				num++;
				break;
			}
		}
		return SwiftText.TrimWhitespace(stringBuilder.ToString());
	}

	private static string? SwiftSplitFirst(string text, char separator)
	{
		int i;
		for (i = 0; i < text.Length && text[i] == separator; i++)
		{
		}
		if (i == text.Length)
		{
			return null;
		}
		int num = text.IndexOf(separator, i);
		int num2;
		if (num >= 0)
		{
			num2 = i;
			return text.Substring(num2, num - num2);
		}
		num2 = i;
		return text.Substring(num2, text.Length - num2);
	}

	private static string Literal(string text)
	{
		StringBuilder stringBuilder = new StringBuilder(text.Length * 2);
		foreach (char value in text)
		{
			stringBuilder.Append('\u0001').Append(value);
		}
		return stringBuilder.ToString();
	}

	private static bool IsDateFormat(string cleaned)
	{
		bool flag = false;
		foreach (char c in cleaned)
		{
			switch (c)
			{
			case '\u0001':
				flag = true;
				continue;
			case '\u0002':
				return true;
			}
			bool num = flag;
			flag = false;
			if (num)
			{
				continue;
			}
			switch (char.ToLowerInvariant(c))
			{
			case 'd':
			case 'h':
			case 's':
			case 'y':
				return true;
			case 'm':
				if (!cleaned.Contains('0'))
				{
					return !cleaned.Contains('#');
				}
				return false;
			}
		}
		return false;
	}

	private static string FormatNumber(double value, string code)
	{
		List<(char Character, bool IsLiteral)> items = new List<(char, bool)>();
		int num = 0;
		while (num < code.Length)
		{
			if (code[num] == '\u0001' && num + 1 < code.Length)
			{
				items.Add((code[num + 1], true));
				num += 2;
			}
			else
			{
				items.Add((code[num], false));
				num++;
			}
		}
		List<int> list = (from i in Enumerable.Range(0, items.Count)
			where !items[i].IsLiteral && "0#?".Contains(items[i].Character)
			select i).ToList();
		if (list.Count == 0)
		{
			return new string(items.Select(((char Character, bool IsLiteral) i) => i.Character).ToArray()) + General(value);
		}
		int num2 = list[0];
		int j = list[list.Count - 1];
		int num3 = num2;
		if (num3 > 0 && !items[num3 - 1].IsLiteral && items[num3 - 1].Character == '.')
		{
			num3--;
		}
		for (; j + 1 < items.Count && !items[j + 1].IsLiteral && items[j + 1].Character == ','; j++)
		{
		}
		string text = new string((from i in items.Take(num3)
			select i.Character).ToArray());
		string text2 = new string((from i in items.Skip(num3).Take(j - num3 + 1)
			where !i.IsLiteral
			select i.Character).ToArray());
		string text3 = new string((from i in items.Skip(j + 1)
			select i.Character).ToArray());
		int num4 = items.Take(num3).Concat(items.Skip(j + 1)).Count(((char Character, bool IsLiteral) i) => !i.IsLiteral && i.Character == '%');
		double num5 = value;
		if (num4 > 0)
		{
			num5 *= Math.Pow(100.0, num4);
		}
		while (text2.EndsWith(','))
		{
			string text4 = text2;
			text2 = text4.Substring(0, text4.Length - 1);
			num5 /= 1000.0;
		}
		int num6 = text2.IndexOfAny(new char[2] { 'E', 'e' });
		if (num6 >= 0)
		{
			List<string> list2 = (from p in text2.Substring(0, num6).Split('.', 2)
				where p.Length > 0
				select p).ToList();
			int decimals = ((list2.Count > 1) ? list2[1].Length : 0);
			return text + PrintfE(num5, decimals) + text3;
		}
		string[] array = text2.Split('.');
		string text5 = array[0];
		string source = ((array.Length > 1) ? array[1] : "");
		int minimumFractionDigits = source.Count((char c) => c == '0');
		int maximumFractionDigits = source.Count((char c) => "0#?".Contains(c));
		int minimumIntegerDigits = text5.Count((char c) => c == '0');
		string text6 = DecimalFormat(num5, text5.Contains(','), minimumIntegerDigits, minimumFractionDigits, maximumFractionDigits) ?? General(num5);
		return text + text6 + text3;
	}

	internal static string? DecimalFormat(double number, bool usesGrouping, int minimumIntegerDigits, int minimumFractionDigits, int maximumFractionDigits)
	{
		if (!double.IsFinite(number))
		{
			return null;
		}
		bool flag = number < 0.0 || (number == 0.0 && double.IsNegative(number));
		(string Integer, string Fraction) tuple = RoundedDigits(Math.Abs(number), maximumFractionDigits);
		string item = tuple.Integer;
		string text = tuple.Fraction;
		item = item.TrimStart('0');
		while (text.Length > minimumFractionDigits && text.EndsWith('0'))
		{
			string text2 = text;
			text = text2.Substring(0, text2.Length - 1);
		}
		if (item.Length < minimumIntegerDigits)
		{
			item = new string('0', minimumIntegerDigits - item.Length) + item;
		}
		if (item.Length == 0 && text.Length == 0)
		{
			item = "0";
		}
		if (usesGrouping && item.Length > 3)
		{
			StringBuilder stringBuilder = new StringBuilder();
			int num = item.Length % 3;
			if (num > 0)
			{
				stringBuilder.Append(item, 0, num);
			}
			for (int i = num; i < item.Length; i += 3)
			{
				if (stringBuilder.Length > 0)
				{
					stringBuilder.Append(',');
				}
				stringBuilder.Append(item, i, 3);
			}
			item = stringBuilder.ToString();
		}
		string text3 = ((text.Length > 0) ? (item + "." + text) : item);
		if (!flag)
		{
			return text3;
		}
		return "-" + text3;
	}

	private static (string Integer, string Fraction) RoundedDigits(double magnitude, int fractionDigits)
	{
		string text = magnitude.ToString("R", CultureInfo.InvariantCulture);
		string text2 = text;
		int num = 0;
		int num2 = text.IndexOfAny(new char[2] { 'E', 'e' });
		string text3;
		int num3;
		if (num2 >= 0)
		{
			text2 = text.Substring(0, num2);
			text3 = text;
			num3 = num2 + 1;
			num = int.Parse(text3.Substring(num3, text3.Length - num3), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
		}
		int num4 = text2.IndexOf('.');
		string text4 = ((num4 >= 0) ? text2.Remove(num4, 1) : text2);
		int num5 = ((num4 >= 0) ? num4 : text2.Length) + num;
		if (num5 < 0)
		{
			text4 = new string('0', -num5) + text4;
			num5 = 0;
		}
		if (num5 > text4.Length)
		{
			text4 += new string('0', num5 - text4.Length);
		}
		string text5 = text4.Substring(0, num5);
		text3 = text4;
		num3 = num5;
		string text6 = text3.Substring(num3, text3.Length - num3);
		if (text6.Length <= fractionDigits)
		{
			return (Integer: text5, Fraction: text6 + new string('0', fractionDigits - text6.Length));
		}
		bool num6 = text6[fractionDigits] >= '5';
		char[] array = (text5 + text6.Substring(0, fractionDigits)).ToCharArray();
		if (num6)
		{
			int num7 = array.Length - 1;
			while (num7 >= 0)
			{
				if (array[num7] == '9')
				{
					array[num7] = '0';
					num7--;
					continue;
				}
				array[num7] += '\u0001';
				break;
			}
			string text7 = new string(array);
			if (num7 < 0)
			{
				text7 = "1" + text7;
				text5 = text7.Substring(0, text5.Length + 1);
				string item = text5;
				text3 = text7;
				num3 = text5.Length;
				return (Integer: item, Fraction: text3.Substring(num3, text3.Length - num3));
			}
			string item2 = text7.Substring(0, text5.Length);
			text3 = text7;
			num3 = text5.Length;
			return (Integer: item2, Fraction: text3.Substring(num3, text3.Length - num3));
		}
		string text8 = new string(array);
		string item3 = text8.Substring(0, text5.Length);
		text3 = text8;
		num3 = text5.Length;
		return (Integer: item3, Fraction: text3.Substring(num3, text3.Length - num3));
	}

	private static string FormatDate(double serial, string code)
	{
		List<string> list = DateTokens(code);
		if (list.Any((string t) => t.Length > 0 && t[0] == '\u0002') && serial < 0.0)
		{
			return "-" + FormatDate(0.0 - serial, code);
		}
		double num = Math.Round(serial * 86400.0, MidpointRounding.AwayFromZero);
		CalendarDate? calendarDate = CalendarDate.FromExcelSeconds(num);
		if (calendarDate.HasValue)
		{
			CalendarDate valueOrDefault = calendarDate.GetValueOrDefault();
			bool flag = list.Any(delegate(string t)
			{
				string text = t.ToLowerInvariant();
				return (text == "am/pm" || text == "a/p") ? true : false;
			});
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < list.Count; i++)
			{
				string text2 = list[i];
				string text3 = text2.ToLowerInvariant();
				if (text2[0] == '\u0002')
				{
					string text4 = text2;
					string text5 = text4.Substring(1, text4.Length - 1);
					double num2 = ((text5[0] == 'h') ? 3600 : ((text5[0] != 'm') ? 1 : 60));
					string text6 = ((long)Math.Floor(num / num2)).ToString(CultureInfo.InvariantCulture);
					if (text6.Length < text5.Length)
					{
						text6 = new string('0', text5.Length - text6.Length) + text6;
					}
					stringBuilder.Append(text6);
					continue;
				}
				if (text2[0] == '\u0001')
				{
					stringBuilder.Append(text2, 1, text2.Length - 1);
					continue;
				}
				switch (text3[0])
				{
				case 'y':
					stringBuilder.Append((text3.Length <= 2) ? (valueOrDefault.Year % 100).ToString("00", CultureInfo.InvariantCulture) : valueOrDefault.Year.ToString("0000", CultureInfo.InvariantCulture));
					break;
				case 'd':
				{
					StringBuilder stringBuilder2 = stringBuilder;
					stringBuilder2.Append(text3.Length switch
					{
						1 => valueOrDefault.Day.ToString(CultureInfo.InvariantCulture), 
						2 => valueOrDefault.Day.ToString("00", CultureInfo.InvariantCulture), 
						3 => DayAbbreviations[valueOrDefault.DayOfWeek], 
						_ => DayNames[valueOrDefault.DayOfWeek], 
					});
					break;
				}
				case 'h':
					stringBuilder.Append(((!flag) ? valueOrDefault.Hour : ((valueOrDefault.Hour % 12 == 0) ? 12 : (valueOrDefault.Hour % 12))).ToString((text3.Length >= 2) ? "00" : "0", CultureInfo.InvariantCulture));
					break;
				case 's':
					stringBuilder.Append(valueOrDefault.Second.ToString((text3.Length >= 2) ? "00" : "0", CultureInfo.InvariantCulture));
					break;
				case 'm':
				{
					if (IsMinuteToken(i, list))
					{
						stringBuilder.Append(valueOrDefault.Minute.ToString((text3.Length >= 2) ? "00" : "0", CultureInfo.InvariantCulture));
						break;
					}
					StringBuilder stringBuilder2 = stringBuilder;
					stringBuilder2.Append(text3.Length switch
					{
						1 => valueOrDefault.Month.ToString(CultureInfo.InvariantCulture), 
						2 => valueOrDefault.Month.ToString("00", CultureInfo.InvariantCulture), 
						3 => MonthAbbreviations[valueOrDefault.Month - 1], 
						5 => MonthNames[valueOrDefault.Month - 1].Substring(0, 1), 
						_ => MonthNames[valueOrDefault.Month - 1], 
					});
					break;
				}
				case 'a':
					stringBuilder.Append((valueOrDefault.Hour < 12) ? "AM" : "PM");
					break;
				case '0':
					stringBuilder.Append('0', text3.Length);
					break;
				default:
					stringBuilder.Append(text2);
					break;
				}
			}
			return stringBuilder.ToString();
		}
		return General(serial);
	}

	private static List<string> DateTokens(string code)
	{
		List<string> list = new List<string>();
		int num = 0;
		while (num < code.Length)
		{
			char c = code[num];
			if (c == '\u0001' && num + 1 < code.Length)
			{
				global::_003C_003Ey__InlineArray2<char> buffer = default(global::_003C_003Ey__InlineArray2<char>);
				global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray2<char>, char>(ref buffer, 0) = '\u0001';
				global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray2<char>, char>(ref buffer, 1) = code[num + 1];
				list.Add(new string(global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<global::_003C_003Ey__InlineArray2<char>, char>(in buffer, 2)));
				num += 2;
				continue;
			}
			if (c == '\u0002' && num + 1 < code.Length)
			{
				char c2 = code[num + 1];
				int i;
				for (i = num + 1; i < code.Length && code[i] == c2; i++)
				{
				}
				int num2 = num;
				list.Add(code.Substring(num2, i - num2));
				num = i;
				continue;
			}
			char c3 = char.ToLowerInvariant(c);
			if ("ydhms0".Contains(c3))
			{
				int j;
				for (j = num; j < code.Length && char.ToLowerInvariant(code[j]) == c3; j++)
				{
				}
				int num2 = num;
				list.Add(code.Substring(num2, j - num2));
				num = j;
				continue;
			}
			if (c3 == 'a')
			{
				string text = code;
				int num2 = num;
				string text2 = text.Substring(num2, text.Length - num2).ToLowerInvariant();
				if (text2.StartsWith("am/pm", StringComparison.Ordinal))
				{
					list.Add("AM/PM");
					num += 5;
					continue;
				}
				if (text2.StartsWith("a/p", StringComparison.Ordinal))
				{
					list.Add("a/p");
					num += 3;
					continue;
				}
			}
			list.Add(c.ToString());
			num++;
		}
		return list;
	}

	private static bool IsMinuteToken(int index, List<string> tokens)
	{
		for (int num = index - 1; num >= 0; num--)
		{
			char? c = UnitLetter(tokens[num]);
			if (c.HasValue)
			{
				char valueOrDefault = c.GetValueOrDefault();
				if ("ydhms".Contains(valueOrDefault))
				{
					if (valueOrDefault != 'h')
					{
						break;
					}
					return true;
				}
			}
		}
		for (int i = index + 1; i < tokens.Count; i++)
		{
			char? c = UnitLetter(tokens[i]);
			if (c.HasValue)
			{
				char valueOrDefault2 = c.GetValueOrDefault();
				if ("ydhms".Contains(valueOrDefault2))
				{
					return valueOrDefault2 == 's';
				}
			}
		}
		return false;
	}

	private static char? UnitLetter(string token)
	{
		if (token.Length == 0)
		{
			return null;
		}
		string text = token.ToLowerInvariant();
		if (text[0] == '\u0002')
		{
			if (text.Length <= 1)
			{
				return null;
			}
			return text[1];
		}
		return text[0];
	}
}
