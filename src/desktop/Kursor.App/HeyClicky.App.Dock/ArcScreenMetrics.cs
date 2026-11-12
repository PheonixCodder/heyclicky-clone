using System;

namespace HeyClicky.App.Dock;

public readonly record struct ArcScreenMetrics(double PanelWidth, double PanelHeight, double MenuBarHeight, ArcRect? HardwareNotch = null, ArcRect? NotchSurface = null)
{
	public ArcRect NotchTriggerFrame => HardwareNotch?.Inset(-6.0, 0.0) ?? new ArcRect(PanelWidth / 2.0 - 110.0, 0.0, 220.0, Math.Max(8.0, MenuBarHeight * 0.6));

	public static readonly ArcScreenMetrics Fallback = new ArcScreenMetrics(1512.0, 982.0, 32.0, null, null);
}
