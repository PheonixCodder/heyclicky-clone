using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkArrowStroke(int seed = 1) : IClickyShape
{
	public PaperPath PathIn(Rect rect)
	{
		double num = new ClickyPaperRandom((ulong)(seed + 3)).Next(-1.2, 1.2);
		double x = rect.X + rect.Width / 2.0;
		double num2 = rect.Y + rect.Height / 2.0;
		double num3 = rect.X + rect.Width;
		return new PaperPath().MoveTo(rect.X, num2 + num).QuadTo(new Point(num3, num2), new Point(x, num2 - 2.0 + num)).MoveTo(num3 - 5.0, num2 - 4.0)
			.LineTo(num3, num2)
			.LineTo(num3 - 5.0, num2 + 4.0);
	}
}
