using System;
using System.Numerics;

namespace HeyClicky.Platform.Overlay;

internal static class ThinkingDots
{
	public const double TriangleSide = 16.0;

	public static readonly double TriangleHeight = 16.0 * Math.Sqrt(3.0) / 2.0;

	public const double TriangleRotation = Math.PI * -7.0 / 36.0;

	public const double TriangleCornerRadius = 1.44;

	public static readonly double TriangleCircleDistance = TriangleHeight * 2.0 / 3.0 - 2.88;

	public const double DotRadius = 2.3;

	public const double DotDistance = 8.2;

	private const double BeatSeconds = 0.72;

	private const double MoveSeconds = 0.44;

	private const double LoopSeconds = 2.16;

	private const double MaximumStretch = 0.32;

	public static Vector2 CornerPoint(int cornerIndex, double distance)
	{
		double num = -Math.PI / 2.0 + (double)(cornerIndex * 2) * Math.PI / 3.0 + Math.PI * -7.0 / 36.0;
		return new Vector2((float)(Math.Cos(num) * distance), (float)(Math.Sin(num) * distance));
	}

	public static void LoopPose(double secondsIntoLoop, ref CursorClusterPose pose)
	{
		double num = secondsIntoLoop % 2.16;
		int num2 = (int)(num / 0.72);
		double t = Math.Clamp((num - (double)num2 * 0.72) / 0.44, 0.0, 1.0);
		pose.DotCount = 3;
		for (int i = 0; i < 3; i++)
		{
			Vector2 value = CornerPoint((i + num2) % 3, 8.2);
			Vector2 value2 = CornerPoint((i + num2 + 1) % 3, 8.2);
			pose.DotCenters[i] = Vector2.Lerp(value, value2, (float)Easing.EaseInOutCubic(t));
			double num3 = 1.0 + 0.32 * Easing.EaseInOutCubicSpeed(t) / 3.0;
			pose.DotRadii[i] = 2.3 * num3;
			pose.DotAcrossScale[i] = 1.0 / Math.Sqrt(num3) / num3;
			pose.DotAngles[i] = Math.Atan2(value2.Y - value.Y, value2.X - value.X);
		}
	}
}
