using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyWobblyRect(int seed = 1, double wobble = 1.1) : IClickyShape
{
	public PaperPath PathIn(Rect rect)
	{
		ClickyPaperRandom generator = new ClickyPaperRandom((ulong)(seed + 1));
		PaperPath paperPath = new PaperPath();
		double x = rect.Left + Jitter();
		double y = rect.Top + Jitter();
		paperPath.MoveTo(x, y);
		double x2 = rect.Right + Jitter();
		double y2 = rect.Top + Jitter();
		paperPath.LineTo(x2, y2);
		double x3 = rect.Right + Jitter();
		double y3 = rect.Bottom + Jitter();
		paperPath.LineTo(x3, y3);
		double x4 = rect.Left + Jitter();
		double y4 = rect.Bottom + Jitter();
		paperPath.LineTo(x4, y4);
		return paperPath.Close();
		double Jitter()
		{
			return generator.Next(-1.0, 1.0) * wobble;
		}
	}
}
