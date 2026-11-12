using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Ink;

public sealed class ClickyBaselinePlaced : Canvas
{
	public ClickyBaselinePlaced(UIElement content, double x, double y, double baseline)
	{
		base.IsHitTestVisible = false;
		Canvas.SetLeft(content, x);
		Canvas.SetTop(content, y - baseline);
		base.Children.Add(content);
	}

	public static double TextBaseline(PaperFont font)
	{
		return ClickyHandLineBreaker.Ascent(font);
	}
}
