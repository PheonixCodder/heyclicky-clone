using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkLuggageTagShape : IClickyShape
{
	public PaperPath PathIn(Rect rect)
	{
		double x = rect.X;
		double num = rect.X + rect.Width;
		double y = rect.Y;
		double num2 = rect.Y + rect.Height;
		return new PaperPath().MoveTo(x + 16.0, y).LineTo(num - 8.0, y).QuadTo(new Point(num, y + 8.0), new Point(num, y))
			.LineTo(num, num2 - 8.0)
			.QuadTo(new Point(num - 8.0, num2), new Point(num, num2))
			.LineTo(x + 16.0, num2)
			.LineTo(x, num2 - 16.0)
			.LineTo(x, y + 16.0)
			.Close();
	}
}
