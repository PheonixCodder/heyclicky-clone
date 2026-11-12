using System;

namespace HeyClicky.Core.Onboarding;

public static class MicCheckLevel
{
	public const double PassLevel = 0.25;

	public const double PassSeconds = 0.25;

	public const double FallbackSeconds = 10.0;

	public static double FromLinearLevel(float level)
	{
		if (!(level > 0f))
		{
			return 0.0;
		}
		double d = (double)Math.Min(level, 1f) / 10.2;
		return Math.Clamp((20.0 * Math.Log10(d) + 50.0) / 50.0, 0.0, 1.0);
	}

	public static bool PassesByFallback(double secondsOnStep, bool heardAnyInput)
	{
		if (heardAnyInput)
		{
			return secondsOnStep >= 10.0;
		}
		return false;
	}
}
