using System;

namespace HeyClicky.Core.Proactive;

public static class ProactiveCadenceRules
{
	public static int[] Hours(this ProactiveCadence c)
	{
		return c switch
		{
			ProactiveCadence.ThreeTimes => new int[3] { 6, 12, 18 }, 
			ProactiveCadence.FiveTimes => new int[5] { 6, 9, 12, 15, 18 }, 
			_ => new int[1] { 6 }, 
		};
	}

	public static string Title(this ProactiveCadence c)
	{
		return c switch
		{
			ProactiveCadence.ThreeTimes => "3 times a day", 
			ProactiveCadence.FiveTimes => "5 times a day", 
			_ => "Once a day", 
		};
	}

	public static bool IsBeforeFirstMorning(DateTime onboardingLocal, DateTime nowLocal)
	{
		return nowLocal < onboardingLocal.Date.AddDays(1.0).AddHours(6.0);
	}

	public static bool IsDue(this ProactiveCadence c, DateTime nowLocal, DateTime? lastSuccessLocal, DateTime? retryAfterLocal)
	{
		if (retryAfterLocal.HasValue)
		{
			DateTime valueOrDefault = retryAfterLocal.GetValueOrDefault();
			if (nowLocal < valueOrDefault)
			{
				return false;
			}
		}
		DateTime? dateTime = null;
		int[] array = c.Hours();
		foreach (int num in array)
		{
			DateTime dateTime2 = nowLocal.Date.AddHours(num);
			if (dateTime2 <= nowLocal)
			{
				dateTime = dateTime2;
			}
		}
		if (!dateTime.HasValue)
		{
			return false;
		}
		if (lastSuccessLocal.HasValue)
		{
			DateTime valueOrDefault2 = lastSuccessLocal.GetValueOrDefault();
			DateTime value = valueOrDefault2;
			DateTime? dateTime3 = dateTime;
			return value < dateTime3;
		}
		return true;
	}
}
