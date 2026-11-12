using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkSketchBar(int seed) : IClickyShape
{
	public PaperPath PathIn(Rect rect)
	{
		ClickyPaperRandom generator = new ClickyPaperRandom((ulong)(seed + 17));
		double x = rect.X;
		double y = rect.Y;
		double num = rect.X + rect.Width;
		double num2 = rect.Y + rect.Height;
		PaperPath paperPath = new PaperPath();
		double x2 = x + J();
		paperPath.MoveTo(x2, y + J());
		x2 = num + J();
		paperPath.LineTo(x2, y + J());
		x2 = num + J();
		paperPath.LineTo(x2, num2 + J());
		x2 = x + J();
		paperPath.LineTo(x2, num2 + J());
		return paperPath.Close();
		double J()
		{
			return ((double)(generator.Next() % 100) / 100.0 - 0.5) * 1.6;
		}
	}
}
