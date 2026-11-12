using System;
using System.Runtime.CompilerServices;

namespace HeyClicky.Core.Proactive;

public sealed record ProactiveRoutine(int EveryMinutes, string Title)
{
	public int ClampedIntervalMinutes => Math.Clamp(EveryMinutes, 2, 10080);

	public string CadenceLabel => CadenceLabelFor(ClampedIntervalMinutes);

	public string CadenceSpokenLabel
	{
		get
		{
			int clampedIntervalMinutes = ClampedIntervalMinutes;
			if (clampedIntervalMinutes % 10080 == 0)
			{
				int num = clampedIntervalMinutes / 10080;
				if (num != 1)
				{
					return $"every {num} weeks";
				}
				return "every week";
			}
			if (clampedIntervalMinutes % 1440 == 0)
			{
				int num2 = clampedIntervalMinutes / 1440;
				if (num2 != 1)
				{
					return $"every {num2} days";
				}
				return "every day";
			}
			if (clampedIntervalMinutes % 60 == 0)
			{
				int num3 = clampedIntervalMinutes / 60;
				if (num3 != 1)
				{
					return $"every {num3} hours";
				}
				return "every hour";
			}
			return $"every {clampedIntervalMinutes} minutes";
		}
	}

	public const int MinimumIntervalMinutes = 2;

	public const int MaximumIntervalMinutes = 10080;

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

	[CompilerGenerated]
	private ProactiveRoutine(ProactiveRoutine original)
	{
		EveryMinutes = original.EveryMinutes;
		Title = original.Title;
	}
}
