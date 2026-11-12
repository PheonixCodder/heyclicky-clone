using System;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyPaperStripes(double spacing = 10.0, double width = 5.0, double angleDegrees = 60.0) : IClickyShape
{
	public PaperPath PathIn(Rect rect)
	{
		PaperPath paperPath = new PaperPath();
		double num = rect.Height / Math.Tan(angleDegrees * Math.PI / 180.0);
		for (double num2 = 0.0 - num - spacing; num2 < rect.Right + spacing; num2 += spacing)
		{
			paperPath.MoveTo(num2, rect.Bottom).LineTo(num2 + num, rect.Top).LineTo(num2 + num + width, rect.Top)
				.LineTo(num2 + width, rect.Bottom)
				.Close();
		}
		return paperPath;
	}
}
