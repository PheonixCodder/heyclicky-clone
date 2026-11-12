using System;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyNoteShape(bool isPerforated = true, double biteSpacing = 10.0, double biteRadius = 2.7, double biteCenterOffset = 0.8, double bottomRadius = 3.0) : IClickyShape
{
	public PaperPath PathIn(Rect rect)
	{
		PaperPath paperPath = new PaperPath();
		if (!isPerforated)
		{
			return paperPath.AddRoundedRect(rect, bottomRadius);
		}
		double num = Math.Sqrt(Math.Max(0.0, biteRadius * biteRadius - biteCenterOffset * biteCenterOffset));
		double num2 = Math.Asin(biteCenterOffset / biteRadius);
		paperPath.MoveTo(rect.Left, rect.Top);
		for (double num3 = rect.Left + biteSpacing / 2.0; num3 + num <= rect.Right; num3 += biteSpacing)
		{
			paperPath.LineTo(num3 - num, rect.Top);
			paperPath.AddArc(new Point(num3, rect.Top - biteCenterOffset), biteRadius, Math.PI - num2, num2, clockwise: true);
		}
		paperPath.LineTo(rect.Right, rect.Top);
		paperPath.LineTo(rect.Right, rect.Bottom - bottomRadius);
		paperPath.QuadTo(new Point(rect.Right - bottomRadius, rect.Bottom), new Point(rect.Right, rect.Bottom));
		paperPath.LineTo(rect.Left + bottomRadius, rect.Bottom);
		paperPath.QuadTo(new Point(rect.Left, rect.Bottom - bottomRadius), new Point(rect.Left, rect.Bottom));
		return paperPath.Close();
	}
}
