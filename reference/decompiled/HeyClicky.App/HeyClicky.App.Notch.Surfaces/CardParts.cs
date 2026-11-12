using System;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace HeyClicky.App.Notch.Surfaces;

public static class CardParts
{
	public const double ContentTopClearance = 34.0;

	public static void PlaceInEar(FrameworkElement close)
	{
		close.HorizontalAlignment = HorizontalAlignment.Right;
		close.VerticalAlignment = VerticalAlignment.Top;
		close.Margin = new Thickness(0.0, 2.0, 10.0, 0.0);
	}

	public static Grid Tile(UIElement content, double size = 34.0)
	{
		Grid obj = new Grid
		{
			Width = size,
			Height = size,
			VerticalAlignment = VerticalAlignment.Center,
			Children = 
			{
				(UIElement)new ShadowLayer(8.0, DS.Black(0.24), 8.0, 4.0),
				(UIElement)new Border
				{
					CornerRadius = new CornerRadius(8.0),
					Background = Ui.Brush(DS.White(0.94)),
					BorderBrush = Ui.Brush(DS.Black(0.08)),
					BorderThickness = new Thickness(0.5)
				}
			}
		};
		if (content is FrameworkElement frameworkElement)
		{
			frameworkElement.HorizontalAlignment = HorizontalAlignment.Center;
			frameworkElement.VerticalAlignment = VerticalAlignment.Center;
		}
		obj.Children.Add(content);
		return obj;
	}

	public static Grid IconTile(string glyph, double glyphSize, Color color)
	{
		TextBlock textBlock = Icons.Glyph(glyph, glyphSize, color);
		textBlock.FontWeight = FontWeights.SemiBold;
		return Tile(textBlock);
	}

	public static Pressable PlainClose(Action onClick, string tip = "Dismiss")
	{
		Pressable obj = new Pressable
		{
			Width = 20.0,
			Height = 20.0,
			Children = { (UIElement)new Ellipse
			{
				Width = 20.0,
				Height = 20.0,
				Fill = Ui.Brush(DS.White(0.1)),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			} }
		};
		TextBlock textBlock = Icons.Glyph("\ue711", 8.5, DS.Colors.TextSecondary);
		textBlock.FontWeight = FontWeights.Bold;
		obj.Children.Add(textBlock);
		obj.Click += onClick;
		obj.WithIconHover(20.0);
		return obj.Tip(tip);
	}

	public static StackPanel TwoLines(string small, string bold, bool smallShrinks = false)
	{
		TextBlock textBlock = Ui.Text(small, 10.5, FontWeights.Medium, DS.Colors.TextSecondary, TextAlignment.Left, 1);
		TextBlock t = Ui.Text(bold, 12.5, FontWeights.SemiBold, DS.Colors.TextPrimary, TextAlignment.Left, 1);
		return new StackPanel
		{
			Spacing = 3.0,
			VerticalAlignment = VerticalAlignment.Center,
			Children = 
			{
				(UIElement)(smallShrinks ? Ui.Shrink(textBlock) : textBlock),
				(UIElement)Ui.Shrink(t)
			}
		};
	}

	public static Grid HeadsUpRow(UIElement tile, UIElement lines, UIElement? action, UIElement? close)
	{
		if (close is FrameworkElement frameworkElement && NotchGeometry.HasHardwareNotchEar)
		{
			PlaceInEar(frameworkElement);
			return Stack(Row(tile, lines, action, null), frameworkElement);
		}
		return Row(tile, lines, action, close);
	}

	private static Grid Row(UIElement tile, UIElement lines, UIElement? action, UIElement? close)
	{
		Grid grid = new Grid
		{
			ColumnSpacing = 11.0,
			Margin = new Thickness(10.0, 34.0, 10.0, 0.0),
			VerticalAlignment = VerticalAlignment.Top
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		Grid.SetColumn((FrameworkElement)lines, 1);
		grid.Children.Add(tile);
		grid.Children.Add(lines);
		if (action is FrameworkElement frameworkElement)
		{
			Grid.SetColumn(frameworkElement, 2);
			frameworkElement.Margin = new Thickness(21.0, 0.0, 0.0, 0.0);
			frameworkElement.VerticalAlignment = VerticalAlignment.Center;
			grid.Children.Add(frameworkElement);
		}
		if (close is FrameworkElement frameworkElement2)
		{
			Grid.SetColumn(frameworkElement2, 3);
			frameworkElement2.VerticalAlignment = VerticalAlignment.Center;
			if ((object)action == null)
			{
				Grid.SetColumn(frameworkElement2, 2);
				frameworkElement2.Margin = new Thickness(21.0, 0.0, 0.0, 0.0);
			}
			grid.Children.Add(frameworkElement2);
		}
		grid.MinHeight = 34.0;
		return grid;
	}

	public static StackPanel CenteredTitle(string title, string subtitle, double spacing = 3.0)
	{
		TextBlock t = Ui.Text(title, 14.0, FontWeights.SemiBold, DS.Colors.TextPrimary, TextAlignment.Center, 1);
		TextBlock t2 = Ui.Text(subtitle, 12.0, FontWeights.Medium, DS.Colors.TextSecondary, TextAlignment.Center, 1);
		return new StackPanel
		{
			Spacing = spacing,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			Children = 
			{
				(UIElement)Ui.Shrink(t, HorizontalAlignment.Center),
				(UIElement)Ui.Shrink(t2, HorizontalAlignment.Center)
			}
		};
	}

	public static Grid Stack(params UIElement[] layers)
	{
		Grid grid = new Grid();
		foreach (UIElement item in layers)
		{
			grid.Children.Add(item);
		}
		return grid;
	}
}
