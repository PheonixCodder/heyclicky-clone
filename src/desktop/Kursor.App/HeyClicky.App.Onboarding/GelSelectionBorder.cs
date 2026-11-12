using HeyClicky.App.Design;
using HeyClicky.App.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Onboarding;

public sealed class GelSelectionBorder : Grid
{
	public GelSelectionBorder(double radius)
	{
		base.IsHitTestVisible = false;
		base.Margin = new Thickness(-14.0);
		base.Children.Add(new Border
		{
			CornerRadius = new CornerRadius(radius),
			BorderThickness = new Thickness(3.0),
			BorderBrush = Ui.Vertical((0.0, DS.Hex(15594751u)), (0.18, DS.Hex(14084095u)), (0.5, DS.Hex(10668031u)), (0.8, DS.Hex(10079224u)), (1.0, DS.Hex(10670839u)))
		});
		base.Children.Add(new Border
		{
			CornerRadius = new CornerRadius(radius),
			BorderThickness = new Thickness(0.6),
			BorderBrush = Ui.Brush(DS.Hex(2234520u))
		});
		base.Children.Add(new Border
		{
			CornerRadius = new CornerRadius(radius - 2.4),
			Margin = new Thickness(2.4),
			BorderThickness = new Thickness(0.6),
			BorderBrush = Ui.Brush(DS.White(0.6))
		});
	}
}
