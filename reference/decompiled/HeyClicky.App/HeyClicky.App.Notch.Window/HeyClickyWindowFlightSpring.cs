using System;

namespace HeyClicky.App.Notch.Window;

public static class HeyClickyWindowFlightSpring
{
	public const double Response = 0.5;

	public const double Damping = 0.74;

	public const double Duration = 0.95;

	private static double Omega => Math.PI * 4.0;

	private static double OmegaDamped => Omega * Math.Sqrt(0.4524);

	public static double Progress(double t)
	{
		return 1.0 - Math.Exp(-0.74 * Omega * t) * (Math.Cos(OmegaDamped * t) + 0.74 * Omega / OmegaDamped * Math.Sin(OmegaDamped * t));
	}

	public static double FirstCrossing()
	{
		return (Math.PI - Math.Atan(Math.Sqrt(0.4524) / 0.74)) / OmegaDamped;
	}
}
