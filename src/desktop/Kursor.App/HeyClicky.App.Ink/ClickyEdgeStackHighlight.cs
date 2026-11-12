using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Ink;

public static class ClickyEdgeStackHighlight
{
	public static FrameworkElement Wrap(FrameworkElement content, bool isOn, int seed)
	{
		if (!isOn)
		{
			return content;
		}
		Grid grid = new Grid();
		ClickyShapeView clickyShapeView = ClickyShapeView.Fill(new ClickyWobblyRect(seed, 1.6), ClickyEdgeStackInkLook.Highlighter);
		clickyShapeView.Margin = new Thickness(-4.0, -1.0, -4.0, -1.0);
		grid.Children.Add(clickyShapeView);
		grid.Children.Add(content);
		return grid;
	}
}
