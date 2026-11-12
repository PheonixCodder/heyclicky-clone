using System;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public static class PaperWashExtensions
{
	public static PaperSurface WithWash(this PaperSurface surface, PaperFill fill, Func<Size, PaperPath> path, bool clipped = true)
	{
		surface.Washes.Add(new PaperWash(fill, null, clipped, path));
		surface.Redraw();
		return surface;
	}
}
