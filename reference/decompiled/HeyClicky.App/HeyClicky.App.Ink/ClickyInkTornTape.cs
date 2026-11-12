using System;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkTornTape(double tooth = 3.5) : IClickyShape
{
	public PaperPath PathIn(Rect rect)
	{
		PaperPath paperPath = new PaperPath();
		int num = Math.Max(3, (int)(rect.Height / 6.0));
		double x = rect.X;
		double y = rect.Y;
		double num2 = rect.X + rect.Width;
		double y2 = rect.Y + rect.Height;
		paperPath.MoveTo(x, y);
		paperPath.LineTo(num2, y);
		for (int i = 1; i <= num; i++)
		{
			paperPath.LineTo(num2 - ((i % 2 == 1) ? tooth : 0.0), y + rect.Height * (double)i / (double)num);
		}
		paperPath.LineTo(x, y2);
		for (int num3 = num - 1; num3 >= 0; num3--)
		{
			paperPath.LineTo(x + ((num3 % 2 == 1) ? tooth : 0.0), y + rect.Height * (double)num3 / (double)num);
		}
		return paperPath.Close();
	}
}
