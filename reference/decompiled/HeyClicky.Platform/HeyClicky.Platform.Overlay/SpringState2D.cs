using System;

namespace HeyClicky.Platform.Overlay;

internal struct SpringState2D
{
	public double X;

	public double Y;

	public double VelocityX;

	public double VelocityY;

	public void Advance(double targetX, double targetY, double seconds, in OverlayCursorSpring spring)
	{
		(double, double) tuple = spring.ReleasedDisplacement(seconds);
		(double, double) tuple2 = spring.ReleasedVelocity(seconds);
		double num = X - targetX;
		double num2 = Y - targetY;
		X = targetX + num * tuple.Item1 + VelocityX * tuple2.Item1;
		Y = targetY + num2 * tuple.Item1 + VelocityY * tuple2.Item1;
		double velocityX = num * tuple.Item2 + VelocityX * tuple2.Item2;
		double velocityY = num2 * tuple.Item2 + VelocityY * tuple2.Item2;
		VelocityX = velocityX;
		VelocityY = velocityY;
	}

	public readonly bool IsSettled(double targetX, double targetY, double distanceTolerance, double velocityTolerance)
	{
		if (Math.Abs(X - targetX) < distanceTolerance && Math.Abs(Y - targetY) < distanceTolerance)
		{
			return Math.Sqrt(VelocityX * VelocityX + VelocityY * VelocityY) < velocityTolerance;
		}
		return false;
	}
}
