using System;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyBrandMarkShape : IClickyShape
{
	public static readonly Point CropOrigin = new Point(8.0, 4.5);

	public static readonly Size CropSize = new Size(32f, 21f);

	public PaperPath PathIn(Rect rect)
	{
		double scale = Math.Min(rect.Width / CropSize.Width, rect.Height / CropSize.Height);
		return new PaperPath().MoveTo(P(9.5, 12.4)).LineTo(P(14.5, 6.5)).LineTo(P(24.3, 23.3))
			.MoveTo(P(24.3, 23.3))
			.LineTo(P(33.4, 23.3))
			.MoveTo(P(27.1, 12.4))
			.LineTo(P(32.86, 6.5))
			.LineTo(P(38.4, 12.4));
		Point P(double x, double y)
		{
			return new Point(rect.X + (x - CropOrigin.X) * scale, rect.Y + (y - CropOrigin.Y) * scale);
		}
	}
}
