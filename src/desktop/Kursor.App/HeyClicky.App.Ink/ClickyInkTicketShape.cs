using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkTicketShape : IClickyShape
{
	public PaperPath PathIn(Rect rect)
	{
		double x = rect.X;
		double y = rect.Y;
		double num = rect.X + rect.Width;
		double num2 = rect.Y + rect.Height;
		double num3 = rect.X + 270.0;
		double num4 = 4.4182779984;
		double num5 = 5.522847498;
		return new PaperPath().MoveTo(x + 10.0, y).LineTo(num3 - 8.0, y).CubicTo(new Point(num3, y + 8.0), new Point(num3 - 8.0, y + num4), new Point(num3 - num4, y + 8.0))
			.CubicTo(new Point(num3 + 8.0, y), new Point(num3 + num4, y + 8.0), new Point(num3 + 8.0, y + num4))
			.LineTo(num - 10.0, y)
			.CubicTo(new Point(num, y + 10.0), new Point(num - 10.0 + num5, y), new Point(num, y + 10.0 - num5))
			.LineTo(num, num2 - 10.0)
			.CubicTo(new Point(num - 10.0, num2), new Point(num, num2 - 10.0 + num5), new Point(num - 10.0 + num5, num2))
			.LineTo(num3 + 8.0, num2)
			.CubicTo(new Point(num3, num2 - 8.0), new Point(num3 + 8.0, num2 - num4), new Point(num3 + num4, num2 - 8.0))
			.CubicTo(new Point(num3 - 8.0, num2), new Point(num3 - num4, num2 - 8.0), new Point(num3 - 8.0, num2 - num4))
			.LineTo(x + 10.0, num2)
			.CubicTo(new Point(x, num2 - 10.0), new Point(x + 10.0 - num5, num2), new Point(x, num2 - 10.0 + num5))
			.LineTo(x, y + 10.0)
			.CubicTo(new Point(x + 10.0, y), new Point(x, y + 10.0 - num5), new Point(x + 10.0 - num5, y))
			.Close();
	}
}
