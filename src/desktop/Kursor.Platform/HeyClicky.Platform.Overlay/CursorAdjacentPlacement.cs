using System;

namespace HeyClicky.Platform.Overlay;

public static class CursorAdjacentPlacement
{
	public readonly record struct Sides(bool FlippedLeft, bool FlippedAbove);

	public readonly record struct Placed(double Left, double Top, Sides Sides);

	public const double EdgeMarginPoints = 8.0;

	public const double FlipBackSlackPoints = 24.0;

	public static Placed Place(Sides previous, double defaultLeft, double defaultTop, double flippedRight, double flippedBottom, double width, double height, double decisionWidth, double decisionHeight, RectD workArea, double scale)
	{
		double num = 8.0 * scale;
		double num2 = 24.0 * scale;
		double num3 = workArea.Right - num - defaultLeft;
		double num4 = flippedRight - (workArea.X + num);
		double num5 = workArea.Bottom - num - defaultTop;
		double num6 = flippedBottom - (workArea.Y + num);
		bool flag = !(num3 >= decisionWidth + (previous.FlippedLeft ? num2 : 0.0)) && num4 > num3;
		bool flag2 = !(num5 >= decisionHeight + (previous.FlippedAbove ? num2 : 0.0)) && num6 > num5;
		double val = (flag ? (flippedRight - width) : defaultLeft);
		double val2 = (flag2 ? (flippedBottom - height) : defaultTop);
		double num7 = workArea.X + num;
		double val3 = Math.Max(num7, workArea.Right - num - width);
		double num8 = workArea.Y + num;
		double val4 = Math.Max(num8, workArea.Bottom - num - height);
		double left = Math.Min(Math.Max(val, num7), val3);
		val2 = Math.Min(Math.Max(val2, num8), val4);
		return new Placed(left, val2, new Sides(flag, flag2));
	}
}
