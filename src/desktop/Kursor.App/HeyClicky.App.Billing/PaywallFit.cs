using System;

namespace HeyClicky.App.Billing;

public static class PaywallFit
{
	public const double MinCardWidth = 360.0;

	public const double MinCardHeight = 240.0;

	public static double CardWidth(double requested, double workWidthDip, double shadowMargin)
	{
		return Math.Max(360.0, Math.Min(requested, workWidthDip - shadowMargin * 2.0));
	}

	public static double MaxCardHeight(double workHeightDip, double shadowMargin)
	{
		return Math.Max(240.0, workHeightDip - shadowMargin * 2.0);
	}

	public static (int X, int Y) Origin(int boundsLeft, int boundsTop, int boundsWidth, int boundsHeight, int workLeft, int workTop, int workRight, int workBottom, int width, int height)
	{
		int val = boundsLeft + (boundsWidth - width) / 2;
		int val2 = boundsTop + (boundsHeight - height) / 2;
		val = Math.Max(workLeft, Math.Min(val, workRight - width));
		val2 = Math.Max(workTop, Math.Min(val2, workBottom - height));
		return (X: val, Y: val2);
	}
}
