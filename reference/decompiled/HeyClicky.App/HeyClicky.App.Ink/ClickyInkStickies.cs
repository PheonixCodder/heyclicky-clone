using System;
using System.Collections.Generic;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkStickies
{
	private static readonly Color StickyInk = PaperColor.Hex("#3A3226");

	public static FrameworkElement Create(List<ClickyInkLiveNote.Block> blocks, double clock, ClickyInkStyle style)
	{
		double contentWidth = ClickyInkEnvironment.Current.ContentWidth;
		int num = Math.Min(blocks.Count, 3);
		double width = ((num == 1) ? 210.0 : ((contentWidth - (double)((num - 1) * 12)) / (double)num));
		ClickyInkHStack clickyInkHStack = new ClickyInkHStack(12.0, InkVerticalAlignment.Top)
		{
			Margin = new Thickness(0.0, 6.0, 0.0, 6.0)
		};
		for (int i = 0; i < num; i++)
		{
			ClickyInkLiveNote.Block block = blocks[i];
			double num2 = ClickyInkClock.Progress(clock, block.LandsAt, block.LandsAt + 0.55);
			InkTextView inkTextView = ClickyInkWordsView.Make(block.Words, clock, style, ClickyPaperFonts.Hand((num == 1) ? 19.0 : 16.5), pencil: true, StickyInk, null);
			inkTextView.Margin = new Thickness(12.0, 16.0, 12.0, 14.0);
			inkTextView.HorizontalAlignment = HorizontalAlignment.Left;
			ClickyInkOverlay clickyInkOverlay = new ClickyInkOverlay(new Grid
			{
				Width = width,
				MinHeight = 92.0,
				VerticalAlignment = VerticalAlignment.Top,
				Children = { (UIElement)inkTextView }
			}).Behind(new ClickyInkShadowedShape(ClickyShape.Rectangle, "rect", Color(block.Source["color"], i), new PaperShadow(PaperColor.Black.Opacity(0.14), 3.0, 0.0, 2.0)));
			Border border = new Border
			{
				Width = 44.0,
				Height = 13.0,
				Background = Ui.Brush(PaperColor.White.Opacity(0.55)),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Top
			};
			border.Transformed(0.0, -6.0, (i % 2 == 0) ? (-3) : 4, 1.0, null);
			clickyInkOverlay.Children.Add(border);
			clickyInkOverlay.Transformed(0.0, 0.0, Tilt(i) * (1.0 + 1.5 * (1.0 - num2)), 1.0 + 0.12 * (1.0 - num2), null);
			clickyInkOverlay.Opacity = ((clock >= block.LandsAt) ? 1 : 0);
			clickyInkHStack.Children.Add(clickyInkOverlay);
		}
		return ClickyInkObjectKit.Leading(clickyInkHStack, contentWidth);
	}

	public static double Tilt(int index)
	{
		return (new double[3] { -2.2, 1.6, -1.1 })[index % 3];
	}

	public static Color Color(string? name, int index)
	{
		switch (name?.ToLowerInvariant())
		{
		case "pink":
			return PaperColor.Hex("#FFD3DE");
		case "blue":
			return PaperColor.Hex("#CFE6FF");
		case "mint":
		case "green":
			return PaperColor.Hex("#CDEFD6");
		case "peach":
		case "orange":
			return PaperColor.Hex("#FFD9BF");
		case "lilac":
		case "purple":
			return PaperColor.Hex("#E2DAFC");
		default:
			return (new Color[3]
			{
				PaperColor.Hex("#FFE98A"),
				PaperColor.Hex("#FFD3DE"),
				PaperColor.Hex("#CFE6FF")
			})[index % 3];
		}
	}
}
