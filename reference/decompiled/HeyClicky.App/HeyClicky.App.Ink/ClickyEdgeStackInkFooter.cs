using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyEdgeStackInkFooter
{
	public static FrameworkElement? Make(ClickyEdgeStackInkReading reading, string? liveStep, ClickyInkStyle style, int waitingLineLimit = 2, bool omitsWaiting = false)
	{
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 5.0
		};
		if (!reading.ChangeCounts.IsEmpty)
		{
			stackPanel.Children.Add(Changes(reading, style));
		}
		if (reading.CurrentState == ClickyEdgeStackInkReading.State.Writing)
		{
			stackPanel.Children.Add(Live(reading, liveStep, style));
		}
		string waitingLine = reading.WaitingLine;
		if (waitingLine != null && !omitsWaiting)
		{
			stackPanel.Children.Add(Waiting(waitingLine, (reading.CurrentState == ClickyEdgeStackInkReading.State.Writing) ? 1 : waitingLineLimit));
		}
		else
		{
			string pendingLine = reading.PendingLine;
			if (pendingLine != null && !omitsWaiting)
			{
				stackPanel.Children.Add(InkTextView.Plain("↳ " + pendingLine, ClickyPaperFonts.Hand(15.0), style.Fresh).With(delegate(InkTextView t)
				{
					t.LineLimit = 1;
				}));
			}
		}
		if (stackPanel.Children.Count != 0)
		{
			return stackPanel;
		}
		return null;
	}

	private static FrameworkElement Changes(ClickyEdgeStackInkReading reading, ClickyInkStyle style)
	{
		ClickyInkHStack clickyInkHStack = new ClickyInkHStack(5.0, InkVerticalAlignment.Center);
		ClickyEdgeStackInkReading.Changes changeCounts = reading.ChangeCounts;
		if (changeCounts.New > 0)
		{
			clickyInkHStack.Children.Add(Chip($"{changeCounts.New} new", ClickyPaperInk.DoneGreen));
		}
		if (changeCounts.Updated > 0)
		{
			clickyInkHStack.Children.Add(Chip($"{changeCounts.Updated} changed", style.Fresh));
		}
		if (changeCounts.Gone > 0)
		{
			clickyInkHStack.Children.Add(Chip($"{changeCounts.Gone} gone", PaperColor.Hex("#C0473A")));
		}
		clickyInkHStack.Children.Add(InkTextView.Plain("since you looked", ClickyPaperFonts.Hand(13.0), style.Quiet).With(delegate(InkTextView t)
		{
			t.LineLimit = 1;
		}));
		clickyInkHStack.HorizontalAlignment = HorizontalAlignment.Left;
		return clickyInkHStack;
	}

	private static FrameworkElement Live(ClickyEdgeStackInkReading reading, string? liveStep, ClickyInkStyle style)
	{
		FrameworkElement frameworkElement = (FrameworkElement)(((object)ClickyInkIcon.Glyph("pencil", 12.5, style.Fresh.Opacity(0.85), bold: true)) ?? ((object)new Grid()));
		frameworkElement.Transformed(0.0, 0.0, -14.0, 1.0, null);
		InkTextView e = InkTextView.Plain(liveStep ?? (reading.IsPenBusy ? "writing it down" : "working on it"), ClickyPaperFonts.Hand(15.0), style.Fresh).With(delegate(InkTextView t)
		{
			t.LineLimit = 1;
		});
		return new ClickyInkHStack(6.0, InkVerticalAlignment.Center, frameworkElement, ClickyInkHStack.Flexible(e))
		{
			HorizontalAlignment = HorizontalAlignment.Left
		};
	}

	private static FrameworkElement Waiting(string line, int lineLimit)
	{
		Grid grid = new Grid
		{
			Width = 15.0,
			Height = 15.0
		};
		grid.Children.Add(new Ellipse
		{
			Fill = Ui.Brush(ClickyEdgeStackInkLook.TapeColor(ClickyEdgeStackInkReading.State.NeedsYou))
		});
		TextBlock textBlock = PaperText.Line("?", ClickyPaperFonts.Rounded(10.0, FontWeights.ExtraBold), PaperColor.White);
		textBlock.HorizontalAlignment = HorizontalAlignment.Center;
		textBlock.VerticalAlignment = VerticalAlignment.Center;
		grid.Children.Add(textBlock);
		grid.BaselineFromBottom(3.5);
		InkTextView e = InkTextView.Plain(line, ClickyPaperFonts.Hand(15.0), ClickyEdgeStackInkLook.WaitingAmber).With(delegate(InkTextView t)
		{
			t.LineLimit = lineLimit;
		});
		return new ClickyInkHStack(6.0, InkVerticalAlignment.FirstBaseline, grid, ClickyInkHStack.Flexible(e));
	}

	private static FrameworkElement Chip(string words, Color color)
	{
		InkTextView inkTextView = InkTextView.Plain(words, ClickyPaperFonts.Hand(12.5), color).With(delegate(InkTextView t)
		{
			t.LineLimit = 1;
		});
		inkTextView.Margin = new Thickness(5.0, 0.0, 5.0, 0.0);
		ClickyShapeView clickyShapeView = ClickyShapeView.Stroke(ClickyShape.Capsule, color.Opacity(0.45), 0.8);
		return new ClickyInkOverlay(inkTextView, clickyShapeView)
		{
			HorizontalAlignment = HorizontalAlignment.Left
		};
	}
}
