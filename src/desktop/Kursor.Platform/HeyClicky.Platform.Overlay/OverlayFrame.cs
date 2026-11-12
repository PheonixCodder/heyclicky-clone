namespace HeyClicky.Platform.Overlay;

internal readonly struct OverlayFrame
{
	public readonly double TimeSeconds;

	public readonly double DeltaSeconds;

	public readonly PointD MousePx;

	public OverlayFrame(double timeSeconds, double deltaSeconds, PointD mousePx)
	{
		TimeSeconds = timeSeconds;
		DeltaSeconds = deltaSeconds;
		MousePx = mousePx;
	}
}
