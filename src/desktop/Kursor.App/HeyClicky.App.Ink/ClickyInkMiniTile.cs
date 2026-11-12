using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Ink;

public static class ClickyInkMiniTile
{
	public static FrameworkElement Make(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style, int index = 0)
	{
		ClickyInkDocument.Block source = block.Source;
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 1.0,
			Padding = new Thickness(7.0, 0.0, 7.0, 0.0)
		};
		stackPanel.Children.Add(new InkTextView(ClickyInkWords.Text(block.Words, clock, style, pencil: false, style.Quiet, null), ClickyPaperFonts.Typed(7.5, FontWeights.Bold), style.Ink).With(delegate(InkTextView t)
		{
			t.Tracking = 0.5;
			t.Uppercase = true;
			t.LineLimit = 1;
			t.MinimumScale = 0.7;
		}));
		switch (source.Kind)
		{
		case ClickyInkDocument.Kind.Status:
		{
			ClickyInkStatusState state = ClickyInkStatus.State(source["state"] ?? source["status"]);
			string text = ClickyInkDashboardFormNeeds.TileStateWord(ClickyInkStatus.Word(source["state"] ?? source["status"], state));
			stackPanel.Children.Add(new ClickyInkHStack(4.0, InkVerticalAlignment.Center, ClickyInkDashboardFormNeeds.LampView(state, 11.0, index + 1), ClickyInkFormKit.Text(text, ClickyPaperFonts.BigNumber(15.0), ClickyInkDashboardFormNeeds.Word(state), 0.0, 0.6, 1))
			{
				HorizontalAlignment = HorizontalAlignment.Left
			});
			break;
		}
		case ClickyInkDocument.Kind.Spark:
		{
			ClickyInkSparkReading clickyInkSparkReading = new ClickyInkSparkReading(source);
			stackPanel.Children.Add(ClickyInkDashboardFormNeeds.TickingNumber(source, "values", clock, style, 18.0, (double)index * 0.2, clickyInkSparkReading.Last, clickyInkSparkReading.PreviousLast, showsWas: false));
			stackPanel.Children.Add(ClickyInkDashboardFormNeeds.SparkLine(clickyInkSparkReading.Values, style, index));
			break;
		}
		case ClickyInkDocument.Kind.Progress:
		{
			ClickyInkProgress clickyInkProgress = new ClickyInkProgress(source.Attributes);
			stackPanel.Children.Add(ClickyInkFormKit.Text(clickyInkProgress.Shown, ClickyPaperFonts.BigNumber(16.0), style.Ink, 0.0, 0.6, 1));
			stackPanel.Children.Add(ClickyInkDashboardFormNeeds.ProgressBar(clickyInkProgress.Fill, 6.0, style, index).Padding(0.0, 2.0, 0.0, 0.0));
			break;
		}
		case ClickyInkDocument.Kind.Countdown:
		{
			ClickyInkCountdownReading clickyInkCountdownReading = new ClickyInkCountdownReading(source);
			stackPanel.Children.Add(new ClickyInkHStack(3.0, InkVerticalAlignment.FirstBaseline, ClickyInkFormKit.Text((clickyInkCountdownReading.Big.Length == 0) ? "–" : clickyInkCountdownReading.Big, ClickyPaperFonts.BigNumber(18.0), ClickyInkDashboardFormNeeds.CountdownColor(clickyInkCountdownReading, style), 0.0, 1.0, 1), ClickyInkFormKit.Text(clickyInkCountdownReading.Unit, ClickyPaperFonts.Hand(12.0), style.Quiet, 0.0, 1.0, 1))
			{
				HorizontalAlignment = HorizontalAlignment.Left
			});
			break;
		}
		case ClickyInkDocument.Kind.Streak:
		{
			ClickyInkStreakReading clickyInkStreakReading = new ClickyInkStreakReading(source);
			stackPanel.Children.Add(new ClickyInkHStack(3.0, InkVerticalAlignment.FirstBaseline, ClickyInkFormKit.Text(clickyInkStreakReading.Shown, ClickyPaperFonts.BigNumber(18.0), style.Ink, 0.0, 1.0, 1), ClickyInkFormKit.Text("in a row", ClickyPaperFonts.Hand(12.0), style.Quiet, 0.0, 1.0, 1))
			{
				HorizontalAlignment = HorizontalAlignment.Left
			});
			break;
		}
		default:
		{
			string key = ((source["value"] != null) ? "value" : ((source["count"] != null) ? "count" : "big"));
			stackPanel.Children.Add(ClickyInkDashboardFormNeeds.TickingNumber(source, key, clock, style, 18.0, (double)index * 0.2, null, null, showsWas: false));
			break;
		}
		}
		return stackPanel;
	}
}
