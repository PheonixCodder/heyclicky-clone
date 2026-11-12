using System.Linq;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkBox(int seed = 1) : IClickyShape
{
	public PaperPath PathIn(Rect rect)
	{
		ClickyPaperRandom generator = new ClickyPaperRandom((ulong)(seed + 5));
		Point[] array = new Point[4]
		{
			new Point(rect.X, rect.Y),
			new Point(rect.X + rect.Width, rect.Y),
			new Point(rect.X + rect.Width, rect.Y + rect.Height),
			new Point(rect.X, rect.Y + rect.Height)
		};
		PaperPath paperPath = new PaperPath();
		paperPath.MoveTo(array[0].X + Jitter(), array[0].Y + Jitter());
		foreach (Point item in array.Skip(1).Append(array[0]))
		{
			paperPath.LineTo(item.X + Jitter(), item.Y + Jitter());
		}
		return paperPath;
		double Jitter()
		{
			return ((double)(generator.Next() % 100) / 100.0 - 0.5) * 1.6;
		}
	}
}
