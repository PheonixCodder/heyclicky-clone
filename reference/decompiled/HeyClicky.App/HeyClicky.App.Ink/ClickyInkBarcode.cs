using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkBarcode
{
	public static FrameworkElement Make(int seed, double width, double height, Color color)
	{
		ClickyPaperRandom clickyPaperRandom = new ClickyPaperRandom((ulong)(seed + 31));
		PaperPath paperPath = new PaperPath();
		double num2;
		for (double num = 0.0; num < width; num += num2 + (double)(clickyPaperRandom.Next() % 3) + 1.0)
		{
			num2 = clickyPaperRandom.Next() % 3 + 1;
			paperPath.AddRect(new Rect(num, 0.0, num2, height));
		}
		PaperDrawing paperDrawing = new PaperDrawing().Fill(paperPath, color).Sized(width, height);
		paperDrawing.Clip = new RectangleGeometry
		{
			Rect = new Rect(0.0, 0.0, width, height)
		};
		return paperDrawing;
	}
}
