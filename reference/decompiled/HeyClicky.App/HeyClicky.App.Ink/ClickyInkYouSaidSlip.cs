using System;
using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace HeyClicky.App.Ink;

public static class ClickyInkYouSaidSlip
{
	public static FrameworkElement Make(string words, bool isPending, double clock, ClickyInkStyle style)
	{
		ClickyInkHStack clickyInkHStack = new ClickyInkHStack(6.0, InkVerticalAlignment.FirstBaseline);
		clickyInkHStack.Children.Add(InkTextView.Plain("↳", ClickyPaperFonts.Hand(18.0), style.Fresh));
		clickyInkHStack.Children.Add(InkTextView.Plain(words, ClickyPaperFonts.Hand(17.0), style.Fresh).With(delegate(InkTextView t)
		{
			t.LineLimit = 2;
		}));
		if (isPending)
		{
			FrameworkElement frameworkElement = ClickyInkPencilGlyph.Make(13.0, style.Quiet);
			frameworkElement.RenderTransform = new RotateTransform
			{
				Angle = Math.Sin(clock * 7.0) * 6.0,
				CenterX = 0.0,
				CenterY = 13.0
			};
			ClickyInkHStack item = new ClickyInkHStack(3.0, InkVerticalAlignment.Center, frameworkElement, InkTextView.Plain("on it", ClickyPaperFonts.Hand(15.0), style.Quiet));
			clickyInkHStack.Children.Add(item);
		}
		clickyInkHStack.Transformed(0.0, 0.0, -1.0, 1.0, null);
		clickyInkHStack.HorizontalAlignment = HorizontalAlignment.Left;
		clickyInkHStack.Margin = new Thickness(16.0, 0.0, 0.0, 0.0);
		return new Grid
		{
			Width = 378.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			Children = { (UIElement)clickyInkHStack }
		};
	}
}
