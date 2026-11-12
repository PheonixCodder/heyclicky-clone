using System;
using HeyClicky.App.Design;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace HeyClicky.App.UI;

public sealed class GelActionButton : Pressable
{
	private readonly GelSurface surface;

	public GelActionButton(string title, string? iconGlyph, GelAppearance appearance, double height, double horizontalPadding, double minWidth = 0.0, double fontSize = 12.0, double iconSize = 10.0, double spacing = 5.0, bool iconTrailing = false)
	{
		surface = new GelSurface(appearance, height / 2.0);
		base.Height = height;
		base.MinWidth = minWidth;
		Color color = DS.Black(0.85);
		TextBlock ink = Ui.Text(title, fontSize, FontWeights.SemiBold, color);
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = spacing,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Padding = new Thickness(horizontalPadding, 0.0, horizontalPadding, 0.0)
		};
		UIElement uIElement = (string.IsNullOrEmpty(iconGlyph) ? null : Ui.Embossed(Icons.Glyph(iconGlyph, iconSize, color), 0.65, 1.0, 0.8));
		if ((object)uIElement != null && !iconTrailing)
		{
			stackPanel.Children.Add(uIElement);
		}
		stackPanel.Children.Add(Ui.Embossed(ink, 0.65, 1.0, 0.8));
		if ((object)uIElement != null && iconTrailing)
		{
			stackPanel.Children.Add(uIElement);
		}
		base.Children.Add(surface);
		base.Children.Add(stackPanel);
		base.HoverChanged += delegate(bool h)
		{
			surface.Hovered = h;
		};
		WithPressScale(0.97f, 0.12, easeOut: true);
	}

	public static GelActionButton WithIcon(string title, Func<Color, FrameworkElement> icon, GelAppearance appearance, double height, double horizontalPadding, double minWidth = 0.0, double fontSize = 12.0, double spacing = 5.0, bool iconTrailing = false)
	{
		GelActionButton gelActionButton = new GelActionButton(title, null, appearance, height, horizontalPadding, minWidth, fontSize, 10.0, spacing, iconTrailing);
		StackPanel stackPanel = (StackPanel)gelActionButton.Children[1];
		FrameworkElement frameworkElement = icon(DS.White(0.65));
		frameworkElement.RenderTransform = new TranslateTransform
		{
			Y = 1.0
		};
		Grid item = new Grid
		{
			VerticalAlignment = VerticalAlignment.Center,
			Children = 
			{
				(UIElement)frameworkElement,
				(UIElement)icon(DS.Black(0.85))
			}
		};
		if (iconTrailing)
		{
			stackPanel.Children.Add(item);
			return gelActionButton;
		}
		stackPanel.Children.Insert(0, item);
		return gelActionButton;
	}
}
