using Windows.Foundation;

namespace HeyClicky.App.Ink;

public static class ClickyShapeTrim
{
	public static IClickyShape Trim(this IClickyShape shape, double from, double to)
	{
		return new ClickyShape((Rect rect) => shape.PathIn(rect).Trimmed(from, to));
	}
}
