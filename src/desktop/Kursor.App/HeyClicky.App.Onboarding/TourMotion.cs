using System;

namespace HeyClicky.App.Onboarding;

public static class TourMotion
{
	public const double StepSpringResponse = 0.38;

	public const double StepSpringDamping = 0.86;

	public const double ReducedMotionSeconds = 0.12;

	public static double EaseOut(double t)
	{
		if (t <= 0.0)
		{
			return 0.0;
		}
		if (t >= 1.0)
		{
			return 1.0;
		}
		double num = 0.0;
		double num2 = 1.0;
		double num3 = t;
		for (int i = 0; i < 40; i++)
		{
			num3 = (num + num2) / 2.0;
			if (3.0 * (1.0 - num3) * num3 * num3 * 0.58 + num3 * num3 * num3 < t)
			{
				num = num3;
			}
			else
			{
				num2 = num3;
			}
		}
		return 3.0 * (1.0 - num3) * num3 * num3 + num3 * num3 * num3;
	}

	public static (int X, int Y, int W, int H) CenteredRegionPx(int windowWidthPx, int windowHeightPx, double scale, double panelWidth, double panelHeight, double margin)
	{
		int num = Math.Min(windowWidthPx, (int)Math.Ceiling((panelWidth + margin * 2.0) * scale));
		int num2 = Math.Min(windowHeightPx, (int)Math.Ceiling((panelHeight + margin * 2.0) * scale));
		return (X: (windowWidthPx - num) / 2, Y: (windowHeightPx - num2) / 2, W: num, H: num2);
	}
}
