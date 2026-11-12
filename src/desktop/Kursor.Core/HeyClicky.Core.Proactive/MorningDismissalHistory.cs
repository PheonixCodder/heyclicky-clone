using System;
using System.Collections.Generic;

namespace HeyClicky.Core.Proactive;

public sealed record MorningDismissalHistory(List<DateTime> DismissedDays, DateTimeOffset? RestingUntil = null)
{
	public static MorningDismissalHistory Empty => new MorningDismissalHistory(new List<DateTime>(), null);
}
