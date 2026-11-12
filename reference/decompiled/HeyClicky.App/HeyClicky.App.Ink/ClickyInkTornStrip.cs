using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkTornStrip : IClickyShape
{
	public PaperPath PathIn(Rect rect)
	{
		double num = rect.Height / 4.0;
		double x = rect.X;
		double num2 = rect.X + rect.Width;
		double y = rect.Y;
		double y2 = rect.Y + rect.Height;
		PaperPath paperPath = new PaperPath().MoveTo(x, y).LineTo(num2, y);
		for (int i = 0; i < 4; i++)
		{
			paperPath.LineTo(num2 - 2.5, y + num * ((double)i + 0.5));
			paperPath.LineTo(num2, y + num * (double)(i + 1));
		}
		paperPath.LineTo(x, y2);
		for (int num3 = 3; num3 >= 0; num3--)
		{
			paperPath.LineTo(x + 2.5, y + num * ((double)num3 + 0.5));
			paperPath.LineTo(x, y + num * (double)num3);
		}
		return paperPath.Close();
	}
}
