using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkRuledLine(Point from, Point to, int seed = 1) : IClickyShape
{
	public PaperPath PathIn(Rect rect)
	{
		ClickyPaperRandom clickyPaperRandom = new ClickyPaperRandom((ulong)(seed + 89));
		Point p = new Point(rect.X + rect.Width * from.X, rect.Y + rect.Height * from.Y);
		Point pTo = new Point(rect.X + rect.Width * to.X, rect.Y + rect.Height * to.Y);
		double x = (p.X + pTo.X) / 2.0 + clickyPaperRandom.Next(-0.9, 0.9);
		double y = (p.Y + pTo.Y) / 2.0 + clickyPaperRandom.Next(-0.9, 0.9);
		return new PaperPath().MoveTo(p).QuadTo(pTo, new Point(x, y));
	}
}
