using System;
using System.Collections.Generic;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyHandRing(int seed = 1, double overshoot = 0.16) : IClickyShape
{
	public PaperPath PathIn(Rect rect)
	{
		ClickyPaperRandom clickyPaperRandom = new ClickyPaperRandom((ulong)(seed + 7));
		Point point = new Point(rect.X + rect.Width / 2.0, rect.Y + rect.Height / 2.0);
		double num = rect.Width / 2.0;
		double num2 = rect.Height / 2.0;
		double num3 = -0.35 + clickyPaperRandom.Next(-0.2, 0.2);
		List<Point> list = new List<Point>();
		for (int i = 0; i <= 40; i++)
		{
			double num4 = num3 + (double)i / 40.0 * (Math.PI * 2.0) * (1.0 + overshoot);
			double num5 = 1.0 + clickyPaperRandom.Next(-0.035, 0.035);
			list.Add(new Point(point.X + num * (Math.Cos(num4) * num5), point.Y + num2 * (Math.Sin(num4) * num5)));
		}
		PaperPath paperPath = new PaperPath().MoveTo(list[0]);
		for (int j = 1; j < list.Count - 1; j++)
		{
			paperPath.QuadTo(new Point((list[j].X + list[j + 1].X) / 2.0, (list[j].Y + list[j + 1].Y) / 2.0), list[j]);
		}
		return paperPath.LineTo(list[list.Count - 1]);
	}
}
