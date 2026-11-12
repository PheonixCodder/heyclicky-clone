using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Ink;

public static class ClickyInkProgressView
{
	public static FrameworkElement Create(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style, double width)
	{
		ClickyInkDocument.Block source = block.Source;
		ClickyInkProgress clickyInkProgress = new ClickyInkProgress(source.Attributes);
		double? previous = (ClickyInkChange.IsLatest(source, style.LatestEdit) ? PreviousFraction(source) : ((double?)null));
		double num = ClickyInkClock.Progress(clock, block.LandsAt + 0.1, block.LandsAt + 0.8);
		ClickyInkHStack clickyInkHStack = InkDashboardLayout.HStack(8.0, InkVerticalAlignment.FirstBaseline, ClickyInkWordsView.Make(block.Words, clock, style, ClickyPaperFonts.Hand(19.0), pencil: true, null, null).With(delegate(InkTextView t)
		{
			t.LineLimit = 1;
		}), new ClickyInkSpacer(), InkDashboardLayout.Fixed(ClickyInkTickingNumber.Create(source, ValueKey(source), clock, style, 18.0, 0.2, clickyInkProgress.Shown, previous.HasValue ? PreviousShown(source) : null, showsWas: false)));
		clickyInkHStack.Width = width;
		StackPanel stackPanel = InkDashboardLayout.Width(new StackPanel
		{
			Spacing = 6.0,
			Children = { (UIElement)clickyInkHStack }
		}, width);
		stackPanel.Children.Add(ClickyInkProgressBar.Create(clickyInkProgress.Fill, previous, ClickyInkChange.Progress(source, clock, 0.2), num, 15.0, style, block.Id.CharacterCount(), width));
		string text = source["note"];
		if (text != null && text.Length > 0)
		{
			InkTextView inkTextView = InkTextView.Plain(text, ClickyPaperFonts.Hand(15.0), style.Quiet).With(delegate(InkTextView t)
			{
				t.LineLimit = 1;
			});
			inkTextView.Margin = new Thickness(0.0, 3.0, 0.0, 0.0);
			inkTextView.Opacity = num;
			stackPanel.Children.Add(inkTextView);
		}
		return stackPanel;
	}

	public static string ValueKey(ClickyInkDocument.Block block)
	{
		return ClickyInkDashboardViewMath.ProgressValueKey(block);
	}

	public static double? PreviousFraction(ClickyInkDocument.Block block)
	{
		return ClickyInkDashboardViewMath.ProgressPreviousFraction(block);
	}

	public static string? PreviousShown(ClickyInkDocument.Block block)
	{
		return ClickyInkDashboardViewMath.ProgressPreviousShown(block);
	}
}
