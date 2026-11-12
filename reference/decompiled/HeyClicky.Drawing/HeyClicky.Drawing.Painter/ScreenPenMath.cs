using System;
using System.Globalization;
using System.Linq;

namespace HeyClicky.Drawing.Painter;

internal static class ScreenPenMath
{
	internal readonly record struct MarqueeBox(double X, double Y, double Width, double Height, double Radius);

	private static double[] Numbers(string value)
	{
		double result;
		return (from part in value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
			select (!double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out result)) ? double.NaN : result into n
			where !double.IsNaN(n)
			select n).ToArray();
	}

	internal static (float Opacity, float Radius)? GlowSpec(string? value)
	{
		if (value == null)
		{
			return null;
		}
		double[] array = Numbers(value);
		if (array.Length != 2 || !(array[0] > 0.0) || !(array[1] > 0.0))
		{
			return null;
		}
		return ((float)Math.Min(1.0, array[0]), (float)array[1]);
	}

	internal static MarqueeBox? ParseMarquee(string? value)
	{
		if (value == null)
		{
			return null;
		}
		double[] array = Numbers(value);
		if (array.Length < 4 || !(array[2] > 0.0) || !(array[3] > 0.0))
		{
			return null;
		}
		return new MarqueeBox(array[0], array[1], array[2], array[3], (array.Length > 4) ? Math.Max(0.0, array[4]) : 0.0);
	}

	internal static float MarqueeCorner(double width, double height, double radius, bool hasCorners)
	{
		if (!hasCorners)
		{
			return 0f;
		}
		return (float)Math.Max(0.01, Math.Min(radius, Math.Min(width / 2.0 - 0.001, height / 2.0 - 0.001)));
	}

	internal static double SpringProgress(double t, double response, double dampingRatio = 0.68)
	{
		if (t <= 0.0)
		{
			return 0.0;
		}
		double num = Math.PI * 2.0 / response;
		double num2 = num * Math.Sqrt(1.0 - dampingRatio * dampingRatio);
		double num3 = Math.Exp((0.0 - dampingRatio) * num * t);
		return 1.0 - num3 * (Math.Cos(num2 * t) + dampingRatio * num / num2 * Math.Sin(num2 * t));
	}

	internal static double SpringSettlingSeconds(double response, double dampingRatio = 0.68)
	{
		return Math.Log(1000.0) / (dampingRatio * 2.0 * Math.PI / response);
	}
}
