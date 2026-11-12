using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace HeyClicky.Agents.Routines;

public sealed class ClickyRoutine
{
	public readonly record struct Schedule(int Minutes, int? MinuteOfDay, int? Weekday, IReadOnlyList<int>? Weekdays);

	public const double FailureRetryDelaySeconds = 300.0;

	public const int MaximumConsecutiveFailedRunsBeforePause = 3;

	public const int MinimumIntervalMinutes = 2;

	public const int MaximumIntervalMinutes = 10080;

	public const int DayMinutes = 1440;

	public const int WeekMinutes = 10080;

	public string Id { get; set; } = Guid.NewGuid().ToString("D").ToUpperInvariant();

	public string ClickySlug { get; set; } = "";

	public string? JobId { get; set; }

	public int? ScheduledMinuteOfDay { get; set; }

	public int? ScheduledWeekday { get; set; }

	public List<int>? ScheduledWeekdays { get; set; }

	public string Title { get; set; } = "";

	public string TaskPrompt { get; set; } = "";

	public double IntervalSeconds { get; set; }

	public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

	public bool IsPaused { get; set; }

	public DateTimeOffset NextRunAt { get; set; } = DateTimeOffset.Now;

	public int RunCount { get; set; }

	public DateTimeOffset? LastRunStartedAt { get; set; }

	public DateTimeOffset? LastRunFinishedAt { get; set; }

	public string? LastRunSummary { get; set; }

	public bool LastRunFailed { get; set; }

	public DateTimeOffset? PendingRetryAt { get; set; }

	public string? LastRunTaskId { get; set; }

	public int ConsecutiveFailedRunCount { get; set; }

	public DateTimeOffset? WaitingForNetworkSince { get; set; }

	[JsonIgnore]
	public int IntervalMinutes => (int)Math.Round(IntervalSeconds / 60.0);

	[JsonIgnore]
	public bool IsPausedAfterRepeatedFailures
	{
		get
		{
			if (IsPaused)
			{
				return ConsecutiveFailedRunCount >= 3;
			}
			return false;
		}
	}

	[JsonIgnore]
	public string CadenceLabel => CadenceLabelFor(IntervalMinutes);

	[JsonIgnore]
	public string? OwningClickySlug
	{
		get
		{
			if (JobId != null || ClickySlug.Length <= 0)
			{
				return null;
			}
			return ClickySlug;
		}
	}

	[JsonIgnore]
	public bool IsSavedOnAClicky => OwningClickySlug != null;

	[JsonIgnore]
	public string OwnerLogName
	{
		get
		{
			string jobId = JobId;
			if (jobId == null)
			{
				return ClickySlug;
			}
			return "job " + jobId;
		}
	}

	[JsonIgnore]
	public bool HasSetTime
	{
		get
		{
			bool flag = ScheduledMinuteOfDay.HasValue;
			if (flag)
			{
				int intervalMinutes = IntervalMinutes;
				bool flag2 = ((intervalMinutes == 1440 || intervalMinutes == 10080) ? true : false);
				flag = flag2;
			}
			return flag;
		}
	}

	[JsonIgnore]
	public IReadOnlyList<int>? SetDays
	{
		get
		{
			if (IntervalMinutes == 1440)
			{
				List<int> scheduledWeekdays = ScheduledWeekdays;
				if (scheduledWeekdays != null)
				{
					int count = scheduledWeekdays.Count;
					if (count > 0 && count < 7)
					{
						return scheduledWeekdays;
					}
				}
			}
			return null;
		}
	}

	[JsonIgnore]
	public string ScheduleLabel
	{
		get
		{
			if (HasSetTime)
			{
				int? scheduledMinuteOfDay = ScheduledMinuteOfDay;
				if (scheduledMinuteOfDay.HasValue)
				{
					int valueOrDefault = scheduledMinuteOfDay.GetValueOrDefault();
					string text = TimeWords(valueOrDefault);
					IReadOnlyList<int> setDays = SetDays;
					if (setDays != null)
					{
						return DaysWords(setDays) + " at " + text;
					}
					if (IntervalMinutes == 10080)
					{
						int num = ScheduledWeekday ?? ((int)(NextRunAt.ToLocalTime().DayOfWeek + 1));
						string[] dayNames = CultureInfo.CurrentCulture.DateTimeFormat.DayNames;
						string text2 = ((num >= 1 && num <= 7) ? dayNames[num - 1] : "week");
						return "Every " + text2 + " at " + text;
					}
					return "Every day at " + text;
				}
			}
			return CadenceLabel;
		}
	}

	public bool IsRunInFlight(DateTimeOffset now, TimeSpan staleAfter)
	{
		DateTimeOffset? lastRunStartedAt = LastRunStartedAt;
		if (lastRunStartedAt.HasValue)
		{
			DateTimeOffset valueOrDefault = lastRunStartedAt.GetValueOrDefault();
			if (!LastRunFinishedAt.HasValue)
			{
				return now - valueOrDefault < staleAfter;
			}
		}
		return false;
	}

	public static string StartCursorLine(string jobTitle, string title)
	{
		if (!string.Equals(jobTitle, title, StringComparison.OrdinalIgnoreCase))
		{
			return $"Running your routine: {title} ({jobTitle})";
		}
		return "Running your routine: " + title;
	}

	public DateTimeOffset? NextSetTime(DateTimeOffset date)
	{
		if (HasSetTime)
		{
			int? scheduledMinuteOfDay = ScheduledMinuteOfDay;
			if (scheduledMinuteOfDay.HasValue)
			{
				int valueOrDefault = scheduledMinuteOfDay.GetValueOrDefault();
				DateTimeOffset dateTimeOffset = date.ToLocalTime();
				DateTimeOffset candidate = new DateTimeOffset(dateTimeOffset.Year, dateTimeOffset.Month, dateTimeOffset.Day, valueOrDefault / 60, valueOrDefault % 60, 0, dateTimeOffset.Offset);
				IReadOnlyList<int> setDays = SetDays;
				if (setDays != null)
				{
					candidate = setDays.Select((int day) => OnWeekdayAfter(candidate, day, date)).Min();
				}
				else if (IntervalMinutes == 10080)
				{
					candidate = OnWeekdayAfter(candidate, ScheduledWeekday ?? ((int)(dateTimeOffset.DayOfWeek + 1)), date);
				}
				else if (candidate <= date)
				{
					candidate = candidate.AddDays(1.0);
				}
				DateTime dateTime = candidate.DateTime;
				return new DateTimeOffset(dateTime, TimeZoneInfo.Local.GetUtcOffset(dateTime));
			}
		}
		return null;
	}

	private static DateTimeOffset OnWeekdayAfter(DateTimeOffset candidate, int weekday, DateTimeOffset date)
	{
		DateTimeOffset dateTimeOffset = candidate.AddDays((int)(weekday - 1 - candidate.DayOfWeek + 7) % 7);
		if (!(dateTimeOffset <= date))
		{
			return dateTimeOffset;
		}
		return dateTimeOffset.AddDays(7.0);
	}

	public DateTimeOffset RegularNextRunAt(DateTimeOffset date)
	{
		return NextSetTime(date) ?? date.AddSeconds(IntervalSeconds);
	}

	public static string DaysWords(IEnumerable<int> days, CultureInfo? culture = null)
	{
		DateTimeFormatInfo format = (culture ?? CultureInfo.CurrentCulture).DateTimeFormat;
		HashSet<int> hashSet = days.ToHashSet();
		if (hashSet.SetEquals(new global::_003C_003Ez__ReadOnlyArray<int>(new int[5] { 2, 3, 4, 5, 6 })))
		{
			return "Every weekday";
		}
		if (hashSet.SetEquals(new global::_003C_003Ez__ReadOnlyArray<int>(new int[2] { 1, 7 })))
		{
			return "Every weekend";
		}
		int firstWeekday = (int)(format.FirstDayOfWeek + 1);
		List<string> list = (from day in (from i in Enumerable.Range(0, 7)
				select (firstWeekday - 1 + i) % 7 + 1).Where(hashSet.Contains)
			select format.DayNames[day - 1]).ToList();
		object obj;
		if (list.Count <= 1)
		{
			obj = list.FirstOrDefault() ?? "";
		}
		else
		{
			List<string> list2 = list;
			obj = string.Join(", ", list2.Slice(0, list2.Count - 1)) + " and " + list[list.Count - 1];
		}
		string text = (string)obj;
		return "Every " + text;
	}

	public static Schedule NormalizedSchedule(int minutes, int? minuteOfDay, IEnumerable<int>? weekdays)
	{
		List<int> list = (weekdays ?? Array.Empty<int>()).Where((int day) => day >= 1 && day <= 7).Distinct().Order()
			.ToList();
		if (list.Count == 0 || list.Count >= 7)
		{
			int num = Math.Clamp(minutes, 2, 10080);
			bool flag = ((num == 1440 || num == 10080) ? true : false);
			bool flag2 = flag;
			int? minuteOfDay2;
			if (flag2 && minuteOfDay.HasValue)
			{
				int valueOrDefault = minuteOfDay.GetValueOrDefault();
				minuteOfDay2 = Math.Clamp(valueOrDefault, 0, 1439);
			}
			else
			{
				minuteOfDay2 = null;
			}
			return new Schedule(num, minuteOfDay2, null, null);
		}
		int value = Math.Clamp(minuteOfDay ?? 540, 0, 1439);
		if (list.Count != 1)
		{
			return new Schedule(1440, value, null, list);
		}
		return new Schedule(10080, value, list[0], null);
	}

	public static string TimeWords(int minuteOfDay)
	{
		return DateTime.Today.AddMinutes(minuteOfDay).ToString(CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern, CultureInfo.CurrentCulture);
	}

	public static ClickyRoutine Create(string clickySlug, string title, string taskPrompt, int intervalMinutes, string? jobId = null)
	{
		return new ClickyRoutine
		{
			ClickySlug = clickySlug,
			JobId = jobId,
			Title = title,
			TaskPrompt = taskPrompt,
			IntervalSeconds = Math.Clamp(intervalMinutes, 2, 10080) * 60
		};
	}

	public void ScheduleRetry(DateTimeOffset now)
	{
		NextRunAt = now.AddSeconds(300.0);
		PendingRetryAt = NextRunAt;
	}

	public void NoteWaitingForNetwork(DateTimeOffset now)
	{
		DateTimeOffset? waitingForNetworkSince = WaitingForNetworkSince;
		DateTimeOffset valueOrDefault = waitingForNetworkSince.GetValueOrDefault();
		if (!waitingForNetworkSince.HasValue)
		{
			valueOrDefault = now;
			DateTimeOffset? waitingForNetworkSince2 = valueOrDefault;
			WaitingForNetworkSince = waitingForNetworkSince2;
		}
	}

	public void RecordDispatch(DateTimeOffset now, string? taskId)
	{
		WaitingForNetworkSince = null;
		RunCount++;
		LastRunStartedAt = now;
		LastRunFinishedAt = null;
		LastRunSummary = null;
		LastRunFailed = false;
		LastRunTaskId = taskId;
		PendingRetryAt = null;
		NextRunAt = RegularNextRunAt(now);
	}

	public bool RecordOutcome(DateTimeOffset now, string? summary, bool failed, bool shouldRetry, bool countsTowardFailureCap = true)
	{
		LastRunFinishedAt = now;
		LastRunFailed = failed;
		string text = summary?.Trim() ?? "";
		LastRunSummary = ((text.Length == 0) ? null : ((text.Length > 500) ? text.Substring(0, 500) : text));
		PendingRetryAt = null;
		if (!failed)
		{
			ConsecutiveFailedRunCount = 0;
			return false;
		}
		if (shouldRetry && !IsPaused)
		{
			if (countsTowardFailureCap)
			{
				ConsecutiveFailedRunCount++;
				if (ConsecutiveFailedRunCount >= 3)
				{
					IsPaused = true;
					return true;
				}
			}
			ScheduleRetry(now);
		}
		else if (!shouldRetry)
		{
			DateTimeOffset dateTimeOffset = RegularNextRunAt(now);
			if (dateTimeOffset > NextRunAt)
			{
				NextRunAt = dateTimeOffset;
			}
		}
		return false;
	}

	public static DateTimeOffset NextRunAfterAgentMessageLimit(DateTimeOffset now, DateTimeOffset? allowanceResetsAt)
	{
		if (allowanceResetsAt.HasValue)
		{
			DateTimeOffset valueOrDefault = allowanceResetsAt.GetValueOrDefault();
			if (valueOrDefault > now)
			{
				return valueOrDefault.AddSeconds(60.0);
			}
		}
		return now.AddHours(24.0);
	}

	public void RecordAgentMessageLimit(DateTimeOffset now, string? summary, DateTimeOffset? allowanceResetsAt)
	{
		RecordOutcome(now, summary, failed: true, shouldRetry: false, countsTowardFailureCap: false);
		DateTimeOffset dateTimeOffset = NextRunAfterAgentMessageLimit(now, allowanceResetsAt);
		if (dateTimeOffset > NextRunAt)
		{
			NextRunAt = dateTimeOffset;
		}
	}

	public static string CadenceLabelFor(int minutes)
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

	public static string PromptHeaderLine(string title, int runNumber, string cadenceLabel, DateTimeOffset startedAt)
	{
		return $"[Routine \"{title}\" run #{runNumber}, {cadenceLabel.ToLowerInvariant()}, started {startedAt.ToString("MMM d, yyyy 'at' h:mm tt", CultureInfo.GetCultureInfo("en-US"))}]";
	}

	public static string TaskPromptStrippingLeadingCadence(string taskPrompt)
	{
		string text = taskPrompt.Trim();
		string[] obj = new string[2] { "^(every|each)\\s+(\\d+\\s+|other\\s+|few\\s+|couple\\s+of\\s+)?(minutes?|mins?|hours?|hrs?|days?|weeks?|months?|mornings?|afternoons?|evenings?|nights?|weekdays?|weekends?|mondays?|tuesdays?|wednesdays?|thursdays?|fridays?|saturdays?|sundays?)(\\s+(at|around|from|between)\\s+((\\d{1,2}([:.]\\d{2})?(\\s*[ap]\\.?\\s?m\\.?)?|noon|midnight)(\\s*(-|–|to|and)\\s*(\\d{1,2}([:.]\\d{2})?(\\s*[ap]\\.?\\s?m\\.?)?|noon|midnight))?(\\s+(pacific|eastern|central|mountain|local|utc|gmt|[pecm][sd]?t)(\\s+time)?)?|[^,.:;]{1,40}(?=\\s*[,:;])))?\\s*[,:;.\\-–—]?\\s+", "^(hourly|daily|weekly|nightly|monthly|(once|twice)\\s+(a|per)\\s+(day|week|month|hour))\\s*[,:;.\\-–—]?\\s+" };
		string text2 = text;
		string[] array = obj;
		foreach (string pattern in array)
		{
			Match match = Regex.Match(text2, pattern, RegexOptions.IgnoreCase);
			if (match.Success)
			{
				string text3 = text2;
				int length = match.Length;
				text2 = text3.Substring(length, text3.Length - length);
				break;
			}
		}
		text2 = text2.Trim();
		if (text2.Length != 0)
		{
			ReadOnlySpan<char> readOnlySpan = new ReadOnlySpan<char>(char.ToUpperInvariant(text2[0]));
			string text3 = text2;
			return string.Concat(readOnlySpan, text3.Substring(1, text3.Length - 1));
		}
		return text;
	}
}
