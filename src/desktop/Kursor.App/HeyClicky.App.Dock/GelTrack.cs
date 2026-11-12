using System;

namespace HeyClicky.App.Dock;

public static class GelTrack
{
	public const double Inset = 2.0;

	public static double FillWidth(int steps, double inner, double segment, double gelHeight)
	{
		return Math.Min(inner, Math.Max(gelHeight, segment * (double)steps));
	}
}
