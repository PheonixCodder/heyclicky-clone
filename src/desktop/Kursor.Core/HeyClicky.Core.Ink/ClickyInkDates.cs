using System;
using System.CodeDom.Compiler;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;

namespace HeyClicky.Core.Ink;

public static class ClickyInkDates
{
	public readonly record struct Remaining(string Big, string Unit, bool IsPast, bool IsClose);

	private static readonly string[] Formats = new string[32]
	{
		"yyyy-MM-dd HH:mm", "yyyy-MM-dd'T'HH:mm", "yyyy-MM-dd'T'HH:mm:ss", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd h:mm tt", "yyyy-MM-dd", "MMM d, yyyy h:mm tt", "MMM d, yyyy HH:mm", "MMM d yyyy h:mm tt", "MMM d, yyyy",
		"MMM d yyyy", "MMMM d, yyyy h:mm tt", "MMMM d, yyyy", "d MMM yyyy", "d MMMM yyyy", "yyyy-MM-dd h tt", "MMM d, yyyy h tt", "MMM d, h:mm tt", "MMM d h:mm tt", "MMM d, HH:mm",
		"MMM d HH:mm", "MMMM d, h:mm tt", "MMMM d h:mm tt", "MMMM d HH:mm", "MMM d, h tt", "MMM d h tt", "MMMM d, h tt", "MMMM d h tt", "MMM d", "MMMM d",
		"d MMM", "d MMMM"
	};

	private static readonly string[] Weekdays = new string[17]
	{
		"monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday", "mon", "tue", "tues",
		"wed", "thu", "thur", "thurs", "fri", "sat", "sun"
	};

	public static Func<DateTimeOffset> ReferenceNow { get; set; } = () => DateTimeOffset.Now;

	// [GeneratedRegex("(\\d)\\s*([ap])\\.?m\\.?\\b", RegexOptions.IgnoreCase)]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex MeridiemRegex()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__MeridiemRegex_37.Instance;
	}

	public static DateTimeOffset? Parse(string? input, DateTimeOffset? now = null)
	{
		DateTimeOffset dateTimeOffset = now ?? ReferenceNow();
		string text = input?.TrimWs() ?? "";
		if (text.Length == 0 || text.CharacterCount() > 40)
		{
			return null;
		}
		text = text.Replace(" at ", " ").Replace("·", " ").Replace("  ", " ");
		string text2 = text.SplitOmittingEmpty(' ').FirstOrDefault();
		if (text2 != null && Weekdays.Contains(text2.Lowered().TrimSet(",.")))
		{
			string text3 = text;
			int num = text.IndexOf(text2, StringComparison.Ordinal) + text2.Length;
			text = text3.Substring(num, text3.Length - num).TrimSet(" ,");
		}
		text = MeridiemRegex().Replace(text, "$1 $2M").Replace(" aM", " AM").Replace(" pM", " PM");
		string[] formats = Formats;
		foreach (string text4 in formats)
		{
			bool flag = text4.Contains("yyyy", StringComparison.Ordinal);
			string s = (flag ? text : (text + " 2000"));
			string format = (flag ? text4 : (text4 + " yyyy"));
			if (DateTime.TryParseExact(s, format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var result))
			{
				if (flag)
				{
					return new DateTimeOffset(DateTime.SpecifyKind(result, DateTimeKind.Local));
				}
				int year = dateTimeOffset.ToLocalTime().Year;
				DateTimeOffset dateTimeOffset2 = LocalDate(year, result.Month, result.Day, result.Hour, result.Minute);
				if (dateTimeOffset2 < dateTimeOffset.AddSeconds(-5184000.0))
				{
					dateTimeOffset2 = LocalDate(year + 1, result.Month, result.Day, result.Hour, result.Minute);
				}
				return dateTimeOffset2;
			}
		}
		return null;
	}

	private static DateTimeOffset LocalDate(int year, int month, int day, int hour, int minute)
	{
		return new DateTimeOffset(new DateTime(year, month, 1, hour, minute, 0, DateTimeKind.Local).AddDays(day - 1));
	}

	public static Remaining RemainingUntil(DateTimeOffset deadline, DateTimeOffset? now = null)
	{
		DateTimeOffset dateTimeOffset = now ?? ReferenceNow();
		double totalSeconds = (deadline - dateTimeOffset).TotalSeconds;
		if (totalSeconds < 0.0)
		{
			double num = 0.0 - totalSeconds;
			if (num < 3600.0)
			{
				return new Remaining(Math.Max(1L, (long)(num / 60.0)).ToString(CultureInfo.InvariantCulture), "min ago", IsPast: true, IsClose: false);
			}
			if (num < 86400.0)
			{
				return new Remaining(Whole(num / 3600.0), ((long)(num / 3600.0) == 1) ? "hour ago" : "hours ago", IsPast: true, IsClose: false);
			}
			long num2 = (long)(num / 86400.0);
			return new Remaining(num2.ToString(CultureInfo.InvariantCulture), (num2 == 1) ? "day ago" : "days ago", IsPast: true, IsClose: false);
		}
		if (totalSeconds < 3600.0)
		{
			return new Remaining(Math.Max(1L, (long)(totalSeconds / 60.0)).ToString(CultureInfo.InvariantCulture), "min", IsPast: false, IsClose: true);
		}
		if (totalSeconds < 86400.0)
		{
			long num3 = (long)(totalSeconds / 3600.0);
			return new Remaining(num3.ToString(CultureInfo.InvariantCulture), (num3 == 1) ? "hour" : "hours", IsPast: false, IsClose: true);
		}
		int num4 = Math.Max(1, (int)Math.Round((deadline.ToLocalTime().Date - dateTimeOffset.ToLocalTime().Date).TotalDays));
		return new Remaining(num4.ToString(CultureInfo.InvariantCulture), (num4 == 1) ? "day" : "days", IsPast: false, IsClose: false);
		static string Whole(double v)
		{
			return ((long)v).ToString(CultureInfo.InvariantCulture);
		}
	}

	public static string ShortLine(DateTimeOffset date)
	{
		DateTimeOffset dateTimeOffset = date.ToLocalTime();
		bool flag = dateTimeOffset.Hour != 0 || dateTimeOffset.Minute != 0;
		return dateTimeOffset.ToString(flag ? "ddd MMM d · h:mm tt" : "ddd MMM d", CultureInfo.InvariantCulture);
	}
}
