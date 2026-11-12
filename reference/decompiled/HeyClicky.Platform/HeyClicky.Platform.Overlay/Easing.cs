using System;

namespace HeyClicky.Platform.Overlay;

internal static class Easing
{
	public static double EaseInOut(double t)
	{
		return CubicBezier(0.42, 0.0, 0.58, 1.0, t);
	}

	public static double EaseIn(double t)
	{
		return CubicBezier(0.42, 0.0, 1.0, 1.0, t);
	}

	public static double EaseOut(double t)
	{
		return CubicBezier(0.0, 0.0, 0.58, 1.0, t);
	}

	public static double CubicBezier(double x1, double y1, double x2, double y2, double x)
	{
		x = Math.Clamp(x, 0.0, 1.0);
		double cx = 3.0 * x1;
		double bx = 3.0 * (x2 - x1) - cx;
		double ax = 1.0 - cx - bx;
		double num = 3.0 * y1;
		double num2 = 3.0 * (y2 - y1) - num;
		double num3 = 1.0 - num - num2;
		double num4 = x;
		for (int i = 0; i < 8; i++)
		{
			double num5 = SampleX(num4) - x;
			if (Math.Abs(num5) < 1E-06)
			{
				return ((num3 * num4 + num2) * num4 + num) * num4;
			}
			double num6 = (3.0 * ax * num4 + 2.0 * bx) * num4 + cx;
			if (Math.Abs(num6) < 1E-06)
			{
				break;
			}
			num4 -= num5 / num6;
		}
		double num7 = 0.0;
		double num8 = 1.0;
		num4 = x;
		for (int j = 0; j < 30; j++)
		{
			double num9 = SampleX(num4);
			if (Math.Abs(num9 - x) < 1E-06)
			{
				break;
			}
			if (num9 < x)
			{
				num7 = num4;
			}
			else
			{
				num8 = num4;
			}
			num4 = (num7 + num8) / 2.0;
		}
		return ((num3 * num4 + num2) * num4 + num) * num4;
		double SampleX(double t)
		{
			return ((ax * t + bx) * t + cx) * t;
		}
	}

	public static double EaseInOutCubic(double t)
	{
		if (!(t < 0.5))
		{
			return 1.0 - Math.Pow(-2.0 * t + 2.0, 3.0) / 2.0;
		}
		return 4.0 * t * t * t;
	}

	public static double EaseInOutCubicSpeed(double t)
	{
		if (!(t < 0.5))
		{
			return 3.0 * Math.Pow(-2.0 * t + 2.0, 2.0);
		}
		return 12.0 * t * t;
	}

	public static double EaseOutCubic(double t)
	{
		return 1.0 - Math.Pow(1.0 - t, 3.0);
	}

	public static double SpringOut(double t)
	{
		return 1.0 - Math.Exp(-7.0 * t) * Math.Cos(9.0 * t);
	}

	public static double Lerp(double a, double b, double t)
	{
		return a + (b - a) * t;
	}
}
