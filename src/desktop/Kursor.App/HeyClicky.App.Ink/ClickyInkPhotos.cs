using System;
using System.Collections.Generic;
using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Ink;

public static class ClickyInkPhotos
{
	private static readonly double[] Tilts = new double[3] { -2.5, 1.8, -1.2 };

	public static FrameworkElement Create(List<ClickyInkLiveNote.Block> blocks, double clock, ClickyInkStyle style)
	{
		double contentWidth = ClickyInkEnvironment.Current.ContentWidth;
		int num = Math.Min(blocks.Count, 3);
		double num2 = num switch
		{
			2 => 172, 
			1 => 230, 
			_ => 116, 
		};
		ClickyInkHStack clickyInkHStack = new ClickyInkHStack((num == 3) ? 6 : 14, InkVerticalAlignment.Top)
		{
			Margin = new Thickness(0.0, 4.0, 0.0, 4.0)
		};
		for (int i = 0; i < num; i++)
		{
			ClickyInkLiveNote.Block block = blocks[i];
			double develop = ClickyInkClock.Progress(clock, block.LandsAt + 0.2, block.LandsAt + 1.6);
			FrameworkElement item = ClickyInkPolaroidPicture.Make(block.Source["src"] ?? block.Source["path"] ?? block.Source["url"] ?? block.Source["query"], block.Source["query"], develop, num2 - 14.0, (num2 - 14.0) * 0.78);
			InkTextView inkTextView = ClickyInkWordsView.Make(block.Words, clock, style, ClickyPaperFonts.Hand((num == 3) ? 13 : 16), pencil: true, PaperColor.Hex("#2B2620"), null).With(delegate(InkTextView t)
			{
				t.LineLimit = 2;
				t.TextAlignment = TextAlignment.Center;
			});
			inkTextView.HorizontalAlignment = HorizontalAlignment.Center;
			inkTextView.VerticalAlignment = VerticalAlignment.Center;
			Grid item2 = new Grid
			{
				Width = num2 - 14.0,
				Height = ((num == 3) ? 30 : 36),
				Children = { (UIElement)inkTextView }
			};
			ClickyInkOverlay clickyInkOverlay = new ClickyInkOverlay(new StackPanel
			{
				Spacing = 6.0,
				Padding = new Thickness(7.0, 7.0, 7.0, 0.0),
				Children = 
				{
					(UIElement)item,
					(UIElement)item2
				}
			}).Behind(new ClickyInkShadowedShape(ClickyShape.Rectangle, "rect", PaperColor.Hex("#FBFAF6"), new PaperShadow(PaperColor.Black.Opacity(0.16), 3.0, 0.0, 2.0)));
			clickyInkOverlay.Transformed(0.0, 0.0, Tilts[i % 3], 1.0, null);
			clickyInkOverlay.Opacity = ((clock >= block.LandsAt) ? 1 : 0);
			clickyInkHStack.Children.Add(clickyInkOverlay);
		}
		clickyInkHStack.HorizontalAlignment = ((num == 1) ? HorizontalAlignment.Center : HorizontalAlignment.Left);
		return new Grid
		{
			Width = contentWidth,
			HorizontalAlignment = HorizontalAlignment.Left,
			Children = { (UIElement)clickyInkHStack }
		};
	}
}
