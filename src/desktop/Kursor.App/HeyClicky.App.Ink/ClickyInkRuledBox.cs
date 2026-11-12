using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkRuledBox(int seed = 1, double overshoot = 4.0) : IClickyShape
{
	public PaperPath PathIn(Rect rect)
	{
		ClickyPaperRandom generator = new ClickyPaperRandom((ulong)(seed + 67));
		double num = overshoot;
		double x = rect.X;
		double y = rect.Y;
		double num2 = rect.X + rect.Width;
		double num3 = rect.Y + rect.Height;
		PaperPath paperPath = new PaperPath();
		paperPath.MoveTo(x - num, y + J());
		paperPath.LineTo(num2 + num * 0.6, y + J());
		double x2 = num2 + J();
		paperPath.MoveTo(x2, y - num);
		paperPath.LineTo(num2 + J(), num3 + num * 0.7);
		paperPath.MoveTo(num2 + num, num3 + J());
		paperPath.LineTo(x - num * 0.5, num3 + J());
		x2 = x + J();
		paperPath.MoveTo(x2, num3 + num);
		paperPath.LineTo(x + J(), y - num * 0.6);
		return paperPath;
		double J()
		{
			return generator.Next(-0.8, 0.8);
		}
	}
}
