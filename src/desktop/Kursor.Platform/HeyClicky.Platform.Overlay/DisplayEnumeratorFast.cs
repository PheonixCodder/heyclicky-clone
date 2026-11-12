using System;
using Windows.Win32;

namespace HeyClicky.Platform.Overlay;

internal static class DisplayEnumeratorFast
{
	public static Func<PointD>? CursorPositionOverride;

	public static PointD CursorPositionPx()
	{
		Func<PointD> cursorPositionOverride = CursorPositionOverride;
		if (cursorPositionOverride != null)
		{
			return cursorPositionOverride();
		}
		PInvoke.GetCursorPos(out var lpPoint);
		return new PointD(lpPoint.X, lpPoint.Y);
	}
}
