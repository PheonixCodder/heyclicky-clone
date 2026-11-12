using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkCountdown
{
	public static Color Color(ClickyInkCountdownReading reading, ClickyInkStyle style)
	{
		if (!reading.IsPast)
		{
			if (!reading.IsClose)
			{
				return style.Ink;
			}
			return ClickyInkDashboardPalette.Word(ClickyInkStatusState.Watch);
		}
		return ClickyInkDashboardPalette.Word(ClickyInkStatusState.Bad);
	}

	public static FrameworkElement Create(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style, double width)
	{
		ClickyInkDocument.Block source = block.Source;
		ClickyInkCountdownReading clickyInkCountdownReading = new ClickyInkCountdownReading(source);
		double to = ClickyInkClock.Progress(clock, block.LandsAt + 0.15, block.LandsAt + 0.75);
		Color color = Color(clickyInkCountdownReading, style);
		InkTextView inkTextView = InkTextView.Plain((clickyInkCountdownReading.Big.Length == 0) ? "?" : clickyInkCountdownReading.Big, ClickyPaperFonts.BigNumber((clickyInkCountdownReading.Big.CharacterCount() > 2) ? 22 : 28), color).With(delegate(InkTextView t)
		{
			t.MonospacedDigit = true;
		});
		inkTextView.HorizontalAlignment = HorizontalAlignment.Center;
		InkTextView inkTextView2 = InkTextView.Plain(clickyInkCountdownReading.Unit, ClickyPaperFonts.Hand(13.0), style.Quiet).With(delegate(InkTextView t)
		{
			t.LineLimit = 1;
			t.MinimumScale = 0.7;
		});
		inkTextView2.HorizontalAlignment = HorizontalAlignment.Center;
		inkTextView2.Margin = new Thickness(0.0, -2.0, 0.0, 0.0);
		StackPanel item = new StackPanel
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Children = 
			{
				(UIElement)inkTextView,
				(UIElement)inkTextView2
			}
		};
		Grid grid = new Grid
		{
			Width = 64.0,
			Height = 64.0,
			Children = { (UIElement)item }
		};
		grid.Children.Add(ClickyShapeView.Stroke(new ClickyHandRing(block.Id.CharacterCount()).Trim(0.0, to), new PaperStroke((clickyInkCountdownReading.IsPast || clickyInkCountdownReading.IsClose) ? color : style.Fresh.Opacity(0.75), 2.0, PenLineCap.Round)));
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 2.0,
			Children = { (UIElement)ClickyInkWordsView.Make(block.Words, clock, style, ClickyPaperFonts.Hand(20.0), pencil: true, null, null).With(delegate(InkTextView t)
			{
				t.LineLimit = 2;
			}) }
		};
		string dateLine = clickyInkCountdownReading.DateLine;
		if (dateLine != null)
		{
			stackPanel.Children.Add(InkTextView.Plain(dateLine, ClickyPaperFonts.Typed(12.0, FontWeights.Medium), style.Quiet).With(delegate(InkTextView t)
			{
				t.LineLimit = 1;
			}));
		}
		if (clickyInkCountdownReading.IsPast)
		{
			stackPanel.Children.Add(ClickyInkTag.Make("past due", ClickyInkDashboardPalette.Word(ClickyInkStatusState.Bad)));
		}
		else
		{
			string text = source["note"];
			if (text != null && text.Length > 0)
			{
				stackPanel.Children.Add(InkTextView.Plain(text, ClickyPaperFonts.Hand(15.0), style.Fresh).With(delegate(InkTextView t)
				{
					t.LineLimit = 1;
				}));
			}
		}
		return InkDashboardLayout.Width(InkDashboardLayout.HStack(14.0, InkVerticalAlignment.Center, grid, ClickyInkHStack.Flexible(stackPanel), new ClickyInkSpacer()), width);
	}
}
