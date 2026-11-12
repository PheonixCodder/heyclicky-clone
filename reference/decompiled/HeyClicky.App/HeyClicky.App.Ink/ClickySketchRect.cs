using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickySketchRect(double radius = 3.0, long seed = 1L, double jitter = 0.7) : IClickyShape
{
	public PaperPath PathIn(Rect rect)
	{
		return ClickySketchPath.RoundedRect(rect, radius, seed, jitter);
	}
}
