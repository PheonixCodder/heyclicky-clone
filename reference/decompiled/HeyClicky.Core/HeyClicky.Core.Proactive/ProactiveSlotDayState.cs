using System;

namespace HeyClicky.Core.Proactive;

public sealed record ProactiveSlotDayState(DateTimeOffset? MorningFiredAt = null, DateTimeOffset? RetryAfter = null, int? ConsecutiveFailedAttempts = null)
{
	public static TimeSpan RetryDelay(int consecutiveFailures)
	{
		if (consecutiveFailures > 1)
		{
			return consecutiveFailures switch
			{
				2 => TimeSpan.FromMinutes(15L), 
				3 => TimeSpan.FromHours(1), 
				_ => TimeSpan.FromHours(3), 
			};
		}
		return TimeSpan.FromMinutes(5L);
	}
}
