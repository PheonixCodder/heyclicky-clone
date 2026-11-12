using System;

namespace HeyClicky.App.Ink;

public static class InkSpring
{
	public static double Value(double response, double dampingFraction, double elapsed)
	{
		if (elapsed <= 0.0)
		{
			return 0.0;
		}
		double num = Math.PI * 2.0 / response;
		if (dampingFraction >= 1.0)
		{
			return 1.0 - Math.Exp((0.0 - num) * elapsed) * (1.0 + num * elapsed);
		}
		double num2 = dampingFraction * num;
		double num3 = num * Math.Sqrt(1.0 - dampingFraction * dampingFraction);
		return 1.0 - Math.Exp((0.0 - num2) * elapsed) * (Math.Cos(num3 * elapsed) + num2 / num3 * Math.Sin(num3 * elapsed));
	}

	public static double SettleSeconds(double response, double dampingFraction)
	{
		return Math.Min(2.5, 6.9 / (Math.Max(0.05, Math.Min(1.0, dampingFraction)) * 2.0 * Math.PI / response));
	}

	public static double Between(double from, double to, double response, double dampingFraction, double startedAt)
	{
		return from + (to - from) * Value(response, dampingFraction, InkObjectState.Now - startedAt);
	}
}
