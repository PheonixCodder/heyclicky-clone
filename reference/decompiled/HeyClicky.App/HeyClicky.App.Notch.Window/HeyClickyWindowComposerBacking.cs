using HeyClicky.App.Design;
using HeyClicky.App.Home;
using HeyClicky.App.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Notch.Window;

internal sealed class HeyClickyWindowComposerBacking : Grid
{
	private readonly Border fade;

	private readonly Border fill;

	public HeyClickyWindowComposerBacking()
	{
		MessagesPalette current = MessagesPalette.Current;
		base.IsHitTestVisible = false;
		base.VerticalAlignment = VerticalAlignment.Bottom;
		fade = new Border
		{
			Height = 38.0,
			VerticalAlignment = VerticalAlignment.Bottom,
			Background = Ui.Vertical((0.0, current.PaneFill.WithAlpha(0.0)), (1.0, current.PaneFill))
		};
		fill = new Border
		{
			VerticalAlignment = VerticalAlignment.Bottom,
			Background = Ui.Brush(current.PaneFill)
		};
		base.Children.Add(fade);
		base.Children.Add(fill);
		Layout(0.0);
	}

	public void Layout(double extraHeight)
	{
		double num = 54.0 + extraHeight;
		fill.Height = num;
		fade.Margin = new Thickness(0.0, 0.0, 0.0, num);
	}
}
