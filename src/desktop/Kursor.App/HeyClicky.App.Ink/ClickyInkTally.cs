using System;
using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Ink;

public static class ClickyInkTally
{
	public static FrameworkElement Create(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style, double width)
	{
		ClickyInkDocument.Block source = block.Source;
		ClickyInkTallyReading clickyInkTallyReading = new ClickyInkTallyReading(source, style.LatestEdit);
		double appear = ClickyInkClock.Progress(clock, block.LandsAt + 0.1, block.LandsAt + 1.0);
		UIElement[] obj = new UIElement[2]
		{
			ClickyInkTickingNumber.Create(source, clickyInkTallyReading.Key, clock, style, 28.0, 0.3),
			null
		};
		string text = source["goal"] ?? source["of"];
		obj[1] = ((text != null) ? InkDashboardLayout.Fixed(InkTextView.Plain("of " + text, ClickyPaperFonts.Hand(15.0), style.Quiet)) : null);
		ClickyInkHStack item = InkDashboardLayout.HStack(5.0, InkVerticalAlignment.FirstBaseline, obj);
		StackPanel stackPanel = InkDashboardLayout.Width(new StackPanel
		{
			Children = 
			{
				(UIElement)ClickyInkWordsView.Make(block.Words, clock, style, ClickyPaperFonts.Hand(18.0), pencil: true, null, null).With(delegate(InkTextView t)
				{
					t.LineLimit = 2;
				}),
				(UIElement)item
			}
		}, width * 0.32);
		int num = (int)Math.Min(2147483647L, clickyInkTallyReading.Fresh);
		FrameworkElement frameworkElement = ClickyInkTallyMarks.Create((int)Math.Min(2147483647L, clickyInkTallyReading.Count), num, ClickyInkChange.Progress(source, clock, 0.3, Math.Max(0.6, (double)num * 0.16)), appear, 20.0, style, block.Id.CharacterCount(), Math.Max(0.0, width - width * 0.32 - 16.0), 48.0);
		frameworkElement.Margin = new Thickness(0.0, 4.0, 0.0, 0.0);
		ClickyInkHStack clickyInkHStack = InkDashboardLayout.Width(InkDashboardLayout.HStack(16.0, InkVerticalAlignment.Top, stackPanel, frameworkElement), width);
		string text2 = source["note"];
		if (text2 == null)
		{
			return clickyInkHStack;
		}
		FrameworkElement frameworkElement2 = clickyInkHStack;
		if (text2.Length > 0)
		{
			InkTextView inkTextView = InkTextView.Plain(text2, ClickyPaperFonts.Hand(14.0), style.Fresh).With(delegate(InkTextView t)
			{
				t.LineLimit = 1;
			});
			inkTextView.VerticalAlignment = VerticalAlignment.Bottom;
			inkTextView.HorizontalAlignment = HorizontalAlignment.Left;
			inkTextView.Transformed(0.0, 16.0, 0.0, 1.0, null);
			frameworkElement2 = new ClickyInkOverlay(clickyInkHStack, inkTextView);
		}
		frameworkElement2.Margin = new Thickness(0.0, 0.0, 0.0, 16.0);
		return frameworkElement2;
	}
}
