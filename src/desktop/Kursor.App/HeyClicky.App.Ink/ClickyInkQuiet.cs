using System;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;

namespace HeyClicky.App.Ink;

public static class ClickyInkQuiet
{
	public static FrameworkElement Create(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style, double width, bool isHeadline = false)
	{
		ClickyInkQuietReading clickyInkQuietReading = new ClickyInkQuietReading(block.Source);
		double num = ClickyInkClock.Progress(clock, block.LandsAt + 0.05, block.LandsAt + 0.35);
		Grid grid = new Grid
		{
			Children = { (UIElement)new ClickyPaperStamp(ClickyThingStatus.Ready, "ALL QUIET", isHeadline ? 13.0 : 10.5) }
		};
		grid.Transformed(0.0, 0.0, 0.0, 1.0 + 0.35 * (1.0 - num), null);
		grid.Opacity = ((num > 0.0) ? (0.3 + 0.7 * num) : 0.0);
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 2.0,
			Children = { (UIElement)ClickyInkWordsView.Make(block.Words, clock, style, ClickyPaperFonts.Hand(isHeadline ? 26 : 19), pencil: true, null, null).With(delegate(InkTextView t)
			{
				t.LineLimit = 2;
			}) }
		};
		string checkedLine = clickyInkQuietReading.CheckedLine;
		if (checkedLine != null)
		{
			InkTextView inkTextView = InkTextView.Plain(checkedLine, ClickyPaperFonts.Typed(isHeadline ? 12.5 : 11.5, FontWeights.Medium), style.Quiet).With(delegate(InkTextView t)
			{
				t.LineLimit = 1;
			});
			inkTextView.Opacity = ClickyInkClock.Progress(clock, block.Words.FinishedAt, block.Words.FinishedAt + 0.3);
			stackPanel.Children.Add(inkTextView);
		}
		if (clickyInkQuietReading.Runs > 1)
		{
			int num2 = (int)Math.Min(clickyInkQuietReading.Runs, 12L);
			ClickyInkHStack clickyInkHStack = new ClickyInkHStack(4.0, InkVerticalAlignment.Center);
			for (int i = 0; i < num2; i++)
			{
				clickyInkHStack.Children.Add(new Ellipse
				{
					Width = 5.0,
					Height = 5.0,
					Fill = Ui.Brush(ClickyPaperInk.DoneGreen.Opacity((i == num2 - 1) ? 0.95 : 0.45))
				});
			}
			clickyInkHStack.Children.Add(InkTextView.Plain($"{clickyInkQuietReading.Runs} quiet runs in a row", ClickyPaperFonts.Hand(14.0), style.Quiet));
			clickyInkHStack.HorizontalAlignment = HorizontalAlignment.Left;
			clickyInkHStack.Opacity = ClickyInkClock.Progress(clock, block.Words.FinishedAt + 0.1, block.Words.FinishedAt + 0.4);
			stackPanel.Children.Add(clickyInkHStack);
		}
		return InkDashboardLayout.Width(InkDashboardLayout.HStack(14.0, InkVerticalAlignment.Center, grid, ClickyInkHStack.Flexible(stackPanel), new ClickyInkSpacer()), width);
	}
}
