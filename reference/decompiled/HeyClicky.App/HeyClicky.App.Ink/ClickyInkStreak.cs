using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Ink;

public static class ClickyInkStreak
{
	public static FrameworkElement Create(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style, double width)
	{
		ClickyInkDocument.Block source = block.Source;
		ClickyInkStreakReading clickyInkStreakReading = new ClickyInkStreakReading(source);
		double num = ClickyInkClock.Progress(clock, block.LandsAt + 0.1, block.LandsAt + 0.9);
		ClickyInkHStack clickyInkHStack = InkDashboardLayout.HStack(8.0, InkVerticalAlignment.FirstBaseline, ClickyInkWordsView.Make(block.Words, clock, style, ClickyPaperFonts.Hand(19.0), pencil: true, null, null).With(delegate(InkTextView t)
		{
			t.LineLimit = 1;
		}), new ClickyInkSpacer(), InkDashboardLayout.Fixed(ClickyInkTickingNumber.Create(source, "_streak", clock, style, 22.0, 0.4, clickyInkStreakReading.Shown, clickyInkStreakReading.PreviousShown, showsWas: false)), InkDashboardLayout.Fixed(InkTextView.Plain(clickyInkStreakReading.Unit, ClickyPaperFonts.Hand(15.0), style.Quiet)));
		clickyInkHStack.Width = width;
		StackPanel stackPanel = InkDashboardLayout.Width(new StackPanel
		{
			Spacing = 6.0,
			Children = { (UIElement)clickyInkHStack }
		}, width);
		stackPanel.Children.Add(ClickyInkStreakBoxes.Create(clickyInkStreakReading.Days, ClickyInkChange.IsLatest(source, style.LatestEdit) ? clickyInkStreakReading.PreviousCount : ((int?)null), ClickyInkChange.Progress(source, clock, 0.2, 0.6), num, 17.0, 14, style, block.Id.CharacterCount()));
		string text = source["note"];
		if (text != null && text.Length > 0)
		{
			InkTextView inkTextView = InkTextView.Plain(text, ClickyPaperFonts.Hand(15.0), style.Quiet).With(delegate(InkTextView t)
			{
				t.LineLimit = 1;
			});
			inkTextView.Opacity = num;
			stackPanel.Children.Add(inkTextView);
		}
		return stackPanel;
	}
}
