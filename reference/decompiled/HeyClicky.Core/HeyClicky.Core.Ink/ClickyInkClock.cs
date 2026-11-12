using System;

namespace HeyClicky.Core.Ink;

public static class ClickyInkClock
{
	public static double Progress(double clock, double start, double end)
	{
		if (end <= start)
		{
			return (clock >= start) ? 1 : 0;
		}
		return Math.Min(1.0, Math.Max(0.0, (clock - start) / (end - start)));
	}

	public static double EaseInOut(double t)
	{
		double num = Math.Min(1.0, Math.Max(0.0, t));
		return num * num * (3.0 - 2.0 * num);
	}

	public static double EaseOut(double t)
	{
		double num = Math.Min(1.0, Math.Max(0.0, t));
		return 1.0 - Math.Pow(1.0 - num, 3.0);
	}
}
