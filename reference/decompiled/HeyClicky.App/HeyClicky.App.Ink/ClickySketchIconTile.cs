using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace HeyClicky.App.Ink;

public sealed class ClickySketchIconTile : Grid
{
	public ClickySketchIconTile(Func<Color, UIElement> glyph, ClickySketchTone tone = ClickySketchTone.BluePale, long seed = 20L, double size = 26.0)
	{
		base.Width = (base.Height = size);
		base.Children.Add(new ClickySketchShapeView(tone.Fill(), tone.Pencil(), 5.0, seed, 1.1, isDoubled: false));
		UIElement uIElement = glyph(tone.Words());
		if (uIElement is FrameworkElement frameworkElement)
		{
			frameworkElement.HorizontalAlignment = HorizontalAlignment.Center;
			frameworkElement.VerticalAlignment = VerticalAlignment.Center;
		}
		base.Children.Add(uIElement);
	}
}
