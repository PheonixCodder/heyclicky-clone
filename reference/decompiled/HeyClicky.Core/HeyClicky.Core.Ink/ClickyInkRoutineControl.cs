using System;
using System.Globalization;

namespace HeyClicky.Core.Ink;

public sealed record ClickyInkRoutineControl(string Id, string Title, int IntervalMinutes, bool IsPaused, bool IsPausedAfterFailures, bool IsRunning, DateTimeOffset NextRunAt, int RunCount)
{
	public static string NextRunWords(DateTimeOffset date, DateTimeOffset? now = null)
	{
		DateTimeOffset dateTimeOffset = date.ToLocalTime();
		DateTime date2 = (now ?? DateTimeOffset.Now).ToLocalTime().Date;
		string text = dateTimeOffset.ToString("t", CultureInfo.CurrentCulture);
		if (dateTimeOffset.Date == date2)
		{
			return "today " + text;
		}
		if (dateTimeOffset.Date == date2.AddDays(1.0))
		{
			return "tomorrow " + text;
		}
		return dateTimeOffset.ToString("ddd", CultureInfo.CurrentCulture) + " " + text;
	}

	public static string CadenceWords(int minutes)
	{
		if (minutes != 10080)
		{
			return CadenceLabel(minutes);
		}
		return "Every week";
	}

	public static string CadenceLabel(int minutes)
	{
		if (minutes % 1440 == 0)
		{
			int num = minutes / 1440;
			if (num != 1)
			{
				return $"Every {num} days";
			}
			return "Every day";
		}
		if (minutes % 60 == 0)
		{
			int num2 = minutes / 60;
			if (num2 != 1)
			{
				return $"Every {num2} hours";
			}
			return "Every hour";
		}
		return $"Every {minutes} min";
	}
}
