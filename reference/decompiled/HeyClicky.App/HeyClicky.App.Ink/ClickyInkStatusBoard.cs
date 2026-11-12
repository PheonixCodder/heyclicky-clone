using System.Collections.Generic;
using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkStatusBoard
{
	public static FrameworkElement Create(List<ClickyInkLiveNote.Block> blocks, double clock, ClickyInkStyle style, double width)
	{
		StackPanel stackPanel = InkDashboardLayout.Width(new StackPanel
		{
			Spacing = 8.0
		}, width);
		for (int i = 0; i < blocks.Count; i++)
		{
			ClickyInkLiveNote.Block block = blocks[i];
			if (!(clock < block.LandsAt))
			{
				stackPanel.Children.Add(ClickyInkGrow.Around(Row(block, i, clock, style), clock, block.LandsAt, block.VanishesAt));
			}
		}
		return stackPanel;
	}

	private static FrameworkElement Row(ClickyInkLiveNote.Block block, int index, double clock, ClickyInkStyle style)
	{
		ClickyInkDocument.Block source = block.Source;
		ClickyInkStatusState clickyInkStatusState = ClickyInkStatus.State(source["state"] ?? source["status"] ?? source["value"]);
		string text = ClickyInkChange.Previous(source, "state");
		ClickyInkStatusState? previous = ((text != null) ? new ClickyInkStatusState?(ClickyInkStatus.State(text)) : ((ClickyInkStatusState?)null));
		double progress = ClickyInkChange.Progress(source, clock, 0.1, 0.8);
		double appear = ClickyInkClock.Progress(clock, block.LandsAt, block.LandsAt + 0.5);
		bool flag = block.IsStruck(clock);
		int num;
		if (previous.HasValue)
		{
			ClickyInkStatusState valueOrDefault = previous.GetValueOrDefault();
			if (valueOrDefault != clickyInkStatusState && ClickyInkChange.ChangedEdit(source) == style.LatestEdit)
			{
				num = (style.LatestEdit.HasValue ? 1 : 0);
				goto IL_0101;
			}
		}
		num = 0;
		goto IL_0101;
		IL_0101:
		bool flag2 = (byte)num != 0;
		FrameworkElement frameworkElement = ClickyInkLamp.Create(clickyInkStatusState, previous, progress, 19.0, index + 1, appear);
		ClickyInkLiveNote.Words words = block.Words;
		PaperFont font = ClickyPaperFonts.Hand(19.0);
		double? goneAt = block.GoneAt;
		InkTextView inkTextView = ClickyInkLine.Make(words, clock, style, font, null, goneAt, null, fillsWidth: false);
		Color color = ClickyInkDashboardPalette.Word(clickyInkStatusState);
		InkTextView inkTextView2 = InkTextView.Plain(ClickyInkStatus.Word(source["state"] ?? source["status"], clickyInkStatusState), ClickyPaperFonts.Typed(10.0, FontWeights.ExtraBold), color).With(delegate(InkTextView t)
		{
			t.Tracking = 0.9;
		});
		inkTextView2.Margin = new Thickness(5.0, 1.5, 5.0, 1.5);
		ClickyInkOverlay clickyInkOverlay = new ClickyInkOverlay(inkTextView2, ClickyShapeView.Stroke(ClickyShape.RoundedRectangle(3.0), color.Opacity(0.55), 1.0));
		clickyInkOverlay.Transformed(0.0, 0.0, -2.0, 1.0, null);
		clickyInkOverlay.Opacity = ClickyInkClock.Progress(clock, block.Words.FinishedAt, block.Words.FinishedAt + 0.25);
		ClickyInkOverlay clickyInkOverlay2 = new ClickyInkOverlay(clickyInkOverlay);
		if (flag2)
		{
			ClickyShapeView clickyShapeView = ClickyShapeView.Fill(ClickyShape.RoundedRectangle(3.0), style.Highlighter);
			clickyShapeView.Margin = new Thickness(-2.0);
			clickyInkOverlay2.Behind(clickyShapeView);
		}
		ClickyInkHStack item = InkDashboardLayout.HStack(8.0, InkVerticalAlignment.FirstBaseline, inkTextView, InkDashboardLayout.Fixed(clickyInkOverlay2), new ClickyInkSpacer());
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 1.0,
			Children = { (UIElement)item }
		};
		string text2 = source["note"];
		if (text2 != null && text2.Length > 0)
		{
			InkTextView inkTextView3 = InkTextView.Plain(text2, ClickyPaperFonts.Typed(11.5, null), style.Quiet).With(delegate(InkTextView t)
			{
				t.LineLimit = 2;
			});
			inkTextView3.Opacity = ClickyInkClock.Progress(clock, block.Words.FinishedAt, block.Words.FinishedAt + 0.3);
			stackPanel.Children.Add(inkTextView3);
		}
		ClickyInkHStack clickyInkHStack = InkDashboardLayout.HStack(10.0, InkVerticalAlignment.Center, frameworkElement, ClickyInkHStack.Flexible(stackPanel));
		clickyInkHStack.Opacity = (flag ? 0.55 : 1.0);
		return clickyInkHStack;
	}
}
