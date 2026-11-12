using System;
using System.Collections.Generic;
using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Ink;

public static class ClickyInkSpark
{
	public static FrameworkElement Create(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style, double width)
	{
		ClickyInkDocument.Block source = block.Source;
		ClickyInkSparkReading clickyInkSparkReading = new ClickyInkSparkReading(source);
		double num = ClickyInkClock.Progress(clock, block.LandsAt + 0.1, block.LandsAt + 0.9);
		InkTextView item = ClickyInkWordsView.Make(block.Words, clock, style, ClickyPaperFonts.Hand(19.0), pencil: true, null, null).With(delegate(InkTextView t)
		{
			t.LineLimit = 2;
		});
		UIElement[] obj = new UIElement[3]
		{
			ClickyInkTickingNumber.Create(source, "values", clock, style, 26.0, 0.5, clickyInkSparkReading.Last, clickyInkSparkReading.PreviousLast),
			null,
			null
		};
		string text = source["unit"];
		obj[1] = ((text != null && text.Length > 0) ? InkDashboardLayout.Fixed(InkTextView.Plain(text, ClickyPaperFonts.Hand(15.0), style.Quiet)) : null);
		obj[2] = ClickyInkDeltaTag.Create(source, clock, source["delta"], clickyInkSparkReading.Step);
		ClickyInkHStack item2 = InkDashboardLayout.HStack(6.0, InkVerticalAlignment.FirstBaseline, obj);
		StackPanel stackPanel = InkDashboardLayout.Width(new StackPanel
		{
			Spacing = 2.0,
			Children = 
			{
				(UIElement)item,
				(UIElement)item2
			}
		}, width * 0.4);
		List<double> values = clickyInkSparkReading.Values;
		List<double>? previous = (ClickyInkChange.Animates(source, style.LatestEdit) ? clickyInkSparkReading.PreviousValues : null);
		double progress = ClickyInkChange.Progress(source, clock, 0.0, 0.8);
		string text2 = source["goal"];
		double? goal = ((text2 == null) ? ((double?)null) : ClickyInkNumber.Parse(text2)?.Value);
		int seed = block.Id.CharacterCount();
		bool marksNew = ClickyInkChange.IsLatest(source, style.LatestEdit);
		double? width2 = Math.Max(0.0, width - width * 0.4 - 14.0);
		double? height = 50.0;
		FrameworkElement frameworkElement = ClickyInkSparkLine.Create(values, previous, progress, num, goal, style, null, seed, marksNew, width2, height);
		ClickyInkHStack item3 = InkDashboardLayout.HStack(14.0, InkVerticalAlignment.Center, stackPanel, frameworkElement);
		StackPanel stackPanel2 = InkDashboardLayout.Width(new StackPanel
		{
			Spacing = 4.0,
			Children = { (UIElement)item3 }
		}, width);
		if (clickyInkSparkReading.Labels.Count > 0 || source["note"] != null)
		{
			ClickyInkHStack clickyInkHStack = new ClickyInkHStack(8.0, InkVerticalAlignment.Center);
			string text3 = source["note"];
			if (text3 != null && text3.Length > 0)
			{
				clickyInkHStack.Children.Add(InkTextView.Plain(text3, ClickyPaperFonts.Hand(15.0), style.Fresh).With(delegate(InkTextView t)
				{
					t.LineLimit = 1;
				}));
			}
			clickyInkHStack.Children.Add(new ClickyInkSpacer());
			if (clickyInkSparkReading.Labels.Count > 1)
			{
				UIElementCollection children = clickyInkHStack.Children;
				string text4 = clickyInkSparkReading.Labels[0];
				List<string> labels = clickyInkSparkReading.Labels;
				children.Add(InkDashboardLayout.Fixed(InkTextView.Plain(text4 + " → " + labels[labels.Count - 1], ClickyPaperFonts.Typed(10.5, null), style.Quiet).With(delegate(InkTextView t)
				{
					t.LineLimit = 1;
				})));
			}
			clickyInkHStack.Width = width;
			clickyInkHStack.Opacity = num;
			stackPanel2.Children.Add(clickyInkHStack);
		}
		return stackPanel2;
	}
}
