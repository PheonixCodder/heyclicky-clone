using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Ink;

public static class ClickyInkMarginNote
{
	public static FrameworkElement Make(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style)
	{
		double contentWidth = ClickyInkEnvironment.Current.ContentWidth;
		InkTextView inkTextView = InkTextView.Plain("↳", ClickyPaperFonts.Hand(20.0), style.Fresh);
		InkTextView inkTextView2 = ClickyInkWordsView.Make(block.Words, clock, style, ClickyPaperFonts.Hand(18.0), pencil: true, style.Fresh, null);
		ClickyInkHStack clickyInkHStack = new ClickyInkHStack(6.0, InkVerticalAlignment.FirstBaseline, inkTextView, inkTextView2);
		clickyInkHStack.Transformed(0.0, 0.0, -1.2, 1.0, null);
		clickyInkHStack.HorizontalAlignment = HorizontalAlignment.Left;
		clickyInkHStack.Margin = new Thickness(18.0, 0.0, 0.0, 0.0);
		return new Grid
		{
			Width = contentWidth,
			HorizontalAlignment = HorizontalAlignment.Left,
			Children = { (UIElement)clickyInkHStack },
			Opacity = (ClickyInkWords.IsStarted(block.Words, clock) ? 1 : 0)
		};
	}
}
