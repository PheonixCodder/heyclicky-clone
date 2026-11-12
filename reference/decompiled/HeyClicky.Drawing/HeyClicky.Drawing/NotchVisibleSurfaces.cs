using System;
using Windows.Graphics;

namespace HeyClicky.Drawing;

public static class NotchVisibleSurfaces
{
	private static readonly object gate = new object();

	private static RectInt32? frame;

	private static bool open;

	public static RectInt32? Frame
	{
		get
		{
			lock (gate)
			{
				return frame;
			}
		}
	}

	public static RectInt32? OpenFrame
	{
		get
		{
			lock (gate)
			{
				return open ? frame : ((RectInt32?)null);
			}
		}
	}

	public static event Action? Changed;

	public static void Replace(RectInt32? surface, bool isOpen)
	{
		lock (gate)
		{
			if (object.Equals(frame, surface) && open == isOpen)
			{
				return;
			}
			frame = surface;
			open = isOpen;
		}
		NotchVisibleSurfaces.Changed?.Invoke();
	}

	public static bool Intersects(RectInt32 a, RectInt32 b)
	{
		if (a.Width > 0 && a.Height > 0 && b.Width > 0 && b.Height > 0 && a.X < b.X + b.Width && b.X < a.X + a.Width && a.Y < b.Y + b.Height)
		{
			return b.Y < a.Y + a.Height;
		}
		return false;
	}
}
