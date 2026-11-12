using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkSquiggle(int seed = 1) : IClickyShape
{
	public PaperPath PathIn(Rect rect)
	{
		ClickyPaperRandom clickyPaperRandom = new ClickyPaperRandom((ulong)(seed + 11));
		PaperPath paperPath = new PaperPath();
		double num = rect.Y + rect.Height / 2.0;
		paperPath.MoveTo(rect.X, num);
		for (int i = 1; i <= 14; i++)
		{
			double x = rect.X + rect.Width * (double)i / 14.0;
			double num2 = ((double)(clickyPaperRandom.Next() % 100) / 100.0 - 0.5) * rect.Height * 0.9;
			paperPath.LineTo(x, num + num2);
		}
		return paperPath;
	}
}
