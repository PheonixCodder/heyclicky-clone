using System;
using System.Collections.Generic;
using System.Globalization;

namespace HeyClicky.Core.Ink;

public static class ClickyInkEventDates
{
	private static readonly string[] Formats = new string[7] { "yyyy-MM-dd HH:mm", "yyyy-MM-dd'T'HH:mm", "yyyy-MM-dd'T'HH:mm:ss", "yyyy-MM-dd h:mm tt", "yyyy-MM-dd htt", "yyyy-MM-dd h tt", "yyyy-MM-dd" };

	public static bool IsDateOnly(string? text)
	{
		string text2 = text?.TrimWs();
		if (text2 == null)
		{
			return false;
		}
		if (text2.CharacterCount() == 10)
		{
			return Date(text2).HasValue;
		}
		return false;
	}

	public static DateTimeOffset? Date(string? text)
	{
		string text2 = text?.TrimWs() ?? "";
		if (text2.Length == 0)
		{
			return null;
		}
		if (DateTimeOffset.TryParseExact(text2, new string[2] { "yyyy-MM-dd'T'HH:mm:ssZ", "yyyy-MM-dd'T'HH:mm:sszzz" }, CultureInfo.InvariantCulture, text2.HasSuffix("Z") ? (DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal) : DateTimeStyles.None, out var result))
		{
			return result;
		}
		string[] formats = Formats;
		foreach (string format in formats)
		{
			if (DateTime.TryParseExact(text2, format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var result2))
			{
				return new DateTimeOffset(DateTime.SpecifyKind(result2, DateTimeKind.Local));
			}
		}
		return null;
	}

	public static DateTimeOffset? End(string? text, DateTimeOffset start)
	{
		string text2 = text?.TrimWs() ?? "";
		if (text2.Length == 0)
		{
			return null;
		}
		DateTimeOffset? dateTimeOffset = Date(text2);
		if (dateTimeOffset.HasValue)
		{
			return dateTimeOffset.GetValueOrDefault();
		}
		string[] array = new string[4] { "HH:mm", "h:mm tt", "htt", "h tt" };
		foreach (string format in array)
		{
			if (DateTime.TryParseExact(text2, format, CultureInfo.InvariantCulture, DateTimeStyles.NoCurrentDateDefault, out var result))
			{
				return new DateTimeOffset(DateTime.SpecifyKind(start.ToLocalTime().Date.AddHours(result.Hour).AddMinutes(result.Minute), DateTimeKind.Local));
			}
		}
		return null;
	}

	public static string Ics(string title, DateTimeOffset start, DateTimeOffset? end, bool isAllDay, string? location, string? notes)
	{
		List<string> list = new List<string>
		{
			"BEGIN:VCALENDAR",
			"VERSION:2.0",
			"PRODID:-//HeyClicky//Note//EN",
			"BEGIN:VEVENT",
			"UID:" + Guid.NewGuid().ToString().ToUpperInvariant() + "@heyclicky",
			"DTSTAMP:" + Utc(DateTimeOffset.Now),
			"SUMMARY:" + Escaped(title)
		};
		DateTimeOffset dateTimeOffset;
		if (isAllDay)
		{
			list.Add("DTSTART;VALUE=DATE:" + Day(start));
			if (end.HasValue)
			{
				DateTimeOffset valueOrDefault = end.GetValueOrDefault();
				if (valueOrDefault > start)
				{
					dateTimeOffset = valueOrDefault;
					goto IL_00c6;
				}
			}
			dateTimeOffset = start;
			goto IL_00c6;
		}
		list.Add("DTSTART:" + Utc(start));
		list.Add("DTEND:" + Utc(end ?? start.AddSeconds(3600.0)));
		goto IL_0147;
		IL_00c6:
		DateTimeOffset dateTimeOffset2 = dateTimeOffset;
		list.Add("DTEND;VALUE=DATE:" + Day(dateTimeOffset2.ToLocalTime().AddDays(1.0)));
		goto IL_0147;
		IL_0147:
		if (!string.IsNullOrEmpty(location))
		{
			list.Add("LOCATION:" + Escaped(ClickyInkNoteActions.Plain(location)));
		}
		if (!string.IsNullOrEmpty(notes))
		{
			list.Add("DESCRIPTION:" + Escaped(ClickyInkNoteActions.Plain(notes)));
		}
		list.AddRange(new global::_003C_003Ez__ReadOnlyArray<string>(new string[2] { "END:VEVENT", "END:VCALENDAR" }));
		return string.Join("\r\n", list);
		static string Day(DateTimeOffset date)
		{
			return date.ToLocalTime().ToString("yyyyMMdd", CultureInfo.InvariantCulture);
		}
		static string Escaped(string text)
		{
			return text.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,")
				.Replace("\n", "\\n");
		}
		static string Utc(DateTimeOffset date)
		{
			return date.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
		}
	}
}
