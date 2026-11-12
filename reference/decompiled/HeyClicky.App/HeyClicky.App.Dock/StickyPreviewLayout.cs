using System;

namespace HeyClicky.App.Dock;

public static class StickyPreviewLayout
{
	public const double ScreenWidth = 480.0;

	public const double ScreenHeight = 300.0;

	public const double Bezel = 7.0;

	public const double ScreenCornerRadius = 10.0;

	public const double Width = 494.0;

	public const double Height = 314.0;

	public static (double Screen, double Outer) CornerRadii(double? outer = null)
	{
		if (outer.HasValue)
		{
			double valueOrDefault = outer.GetValueOrDefault();
			return (Screen: Math.Max(0.0, valueOrDefault - 7.0), Outer: valueOrDefault);
		}
		return (Screen: 10.0, Outer: 17.0);
	}
}
