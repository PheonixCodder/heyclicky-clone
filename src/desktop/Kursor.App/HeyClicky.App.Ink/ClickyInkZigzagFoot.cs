using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkZigzagFoot : IClickyShape
{
	public PaperPath PathIn(Rect rect)
	{
		double num = rect.X + rect.Width;
		double num2 = rect.Y + rect.Height;
		PaperPath paperPath = new PaperPath().MoveTo(rect.X, rect.Y).LineTo(num, rect.Y).LineTo(num, num2 - 6.0);
		int num3 = (int)(rect.Width / 10.0);
		for (int i = 0; i < num3; i++)
		{
			double num4 = num - (double)i * rect.Width / (double)num3;
			paperPath.LineTo(num4 - rect.Width / (double)num3 / 2.0, num2);
			paperPath.LineTo(num4 - rect.Width / (double)num3, num2 - 6.0);
		}
		return paperPath.Close();
	}
}
