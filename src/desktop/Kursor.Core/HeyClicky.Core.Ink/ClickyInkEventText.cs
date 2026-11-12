using System;
using System.Globalization;

namespace HeyClicky.Core.Ink;

public static class ClickyInkEventText
{
	public static string When(DateTimeOffset start, DateTimeOffset? end, bool isAllDay, CultureInfo? culture = null)
	{
		CultureInfo cultureInfo = culture ?? CultureInfo.CurrentCulture;
		DateTimeOffset date = start.ToLocalTime();
		if (isAllDay)
		{
			if (end.HasValue)
			{
				DateTimeOffset valueOrDefault = end.GetValueOrDefault();
				if (valueOrDefault > start)
				{
					return MonthDay(date, cultureInfo) + " – " + MonthDay(valueOrDefault.ToLocalTime(), cultureInfo);
				}
			}
			return date.ToString("dddd", cultureInfo) + ", " + MonthDay(date, cultureInfo);
		}
		string text = date.ToString("ddd", cultureInfo) + ", " + MonthDay(date, cultureInfo) + ", " + Time(date, cultureInfo);
		if (end.HasValue)
		{
			text = text + " – " + Time(end.GetValueOrDefault().ToLocalTime(), cultureInfo);
		}
		return text;
	}

	public static string MonthDay(DateTimeOffset date, CultureInfo c)
	{
		return date.ToString("MMM d", c);
	}

	public static string Time(DateTimeOffset date, CultureInfo c)
	{
		return date.ToString(c.DateTimeFormat.ShortTimePattern.Contains('H') ? "HH:mm" : "h:mm tt", c);
	}

	public static (string Month, string Day, string Weekday) Page(DateTimeOffset start, CultureInfo? culture = null)
	{
		CultureInfo cultureInfo = culture ?? CultureInfo.CurrentCulture;
		DateTimeOffset dateTimeOffset = start.ToLocalTime();
		return (Month: dateTimeOffset.ToString("MMM", cultureInfo).TrimEnd('.').ToUpper(cultureInfo), Day: dateTimeOffset.Day.ToString(cultureInfo), Weekday: dateTimeOffset.ToString("ddd", cultureInfo).TrimEnd('.').ToUpper(cultureInfo));
	}
}
