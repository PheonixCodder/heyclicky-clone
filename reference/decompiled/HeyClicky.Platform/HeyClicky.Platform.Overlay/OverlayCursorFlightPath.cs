using System;

namespace HeyClicky.Platform.Overlay;

internal readonly struct OverlayCursorFlightPath
{
	public readonly double StartX;

	public readonly double StartY;

	public readonly double EndX;

	public readonly double EndY;

	public readonly double ControlX;

	public readonly double ControlY;

	public readonly int TotalFrames;

	public double DurationSeconds => (double)TotalFrames / 60.0;

	public double CompletionDelaySeconds => (double)(TotalFrames + 1) / 60.0;

	public OverlayCursorFlightPath(double startX, double startY, double endX, double endY, double minimumFlightDurationSeconds = 0.6, double minimumArcHeight = 0.0)
	{
		StartX = startX;
		StartY = startY;
		EndX = endX;
		EndY = endY;
		double num = endX - startX;
		double num2 = endY - startY;
		double num3 = Math.Sqrt(num * num + num2 * num2);
		double num4 = Math.Min(Math.Max(num3 / 800.0, minimumFlightDurationSeconds), 1.4);
		TotalFrames = Math.Max(1, (int)(num4 / (1.0 / 60.0)));
		double controlX = (startX + endX) / 2.0;
		double num5 = (startY + endY) / 2.0;
		double num6 = Math.Max(Math.Min(num3 * 0.2, 80.0), minimumArcHeight);
		ControlX = controlX;
		ControlY = num5 - num6;
	}

	public (double X, double Y, double RotationDegrees, double Scale) Pose(double linearProgress)
	{
		double num = Math.Min(1.0, Math.Max(0.0, linearProgress));
		double num2 = num * num * (3.0 - 2.0 * num);
		double num3 = 1.0 - num2;
		double item = num3 * num3 * StartX + 2.0 * num3 * num2 * ControlX + num2 * num2 * EndX;
		double item2 = num3 * num3 * StartY + 2.0 * num3 * num2 * ControlY + num2 * num2 * EndY;
		double item3 = Math.Atan2(x: 2.0 * num3 * (ControlX - StartX) + 2.0 * num2 * (EndX - ControlX), y: 2.0 * num3 * (ControlY - StartY) + 2.0 * num2 * (EndY - ControlY)) * (180.0 / Math.PI) + 90.0;
		double num4 = Math.Sin(num * Math.PI);
		return (X: item, Y: item2, RotationDegrees: item3, Scale: 1.0 + num4 * 0.3);
	}
}
