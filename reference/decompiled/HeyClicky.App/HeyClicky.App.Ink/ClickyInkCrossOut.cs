using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkCrossOut(int seed = 1) : IClickyShape
{
	public PaperPath PathIn(Rect rect)
	{
		double num = ((double)(new ClickyPaperRandom((ulong)(seed + 23)).Next() % 100) / 100.0 - 0.5) * rect.Height * 0.2;
		return new PaperPath().MoveTo(rect.X, rect.Y + rect.Height - rect.Height * 0.25).QuadTo(new Point(rect.X + rect.Width, rect.Y + rect.Height * 0.25), new Point(rect.X + rect.Width / 2.0, rect.Y + rect.Height / 2.0 + num));
	}
}
