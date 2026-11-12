using System;
using System.Globalization;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace HeyClicky.App.Ink;

public static class ClickyInkList
{
	public static FrameworkElement Make(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style, ClickyInkActions actions, double? blockGoneAt = null)
	{
		double contentWidth = ClickyInkEnvironment.Current.ContentWidth;
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 7.0,
			Width = contentWidth,
			HorizontalAlignment = HorizontalAlignment.Left
		};
		for (int i = 0; i < block.Items.Count; i++)
		{
			ClickyInkLiveNote.Item item = block.Items[i];
			stackPanel.Children.Add(ClickyInkGrow.Around(Row(block, item, i, clock, style, actions, blockGoneAt), clock, item.LandsAt, item.VanishesAt));
		}
		return stackPanel;
	}

	private static FrameworkElement Row(ClickyInkLiveNote.Block block, ClickyInkLiveNote.Item item, int offset, double clock, ClickyInkStyle style, ClickyInkActions actions, double? blockGoneAt)
	{
		bool flag = item.Source.Mark == ClickyInkDocument.Mark.Gone;
		double? goneAt = item.GoneAt;
		double? num;
		if (goneAt.HasValue)
		{
			double valueOrDefault = goneAt.GetValueOrDefault();
			double value;
			if (blockGoneAt.HasValue)
			{
				double valueOrDefault2 = blockGoneAt.GetValueOrDefault();
				value = Math.Min(valueOrDefault, valueOrDefault2);
			}
			else
			{
				value = valueOrDefault;
			}
			num = value;
		}
		else
		{
			num = blockGoneAt;
		}
		double? num2 = num;
		int num3;
		if (num2.HasValue)
		{
			double valueOrDefault3 = num2.GetValueOrDefault();
			if (clock >= valueOrDefault3)
			{
				num3 = 1;
				goto IL_009b;
			}
		}
		num3 = ((flag && clock >= item.Words.FinishedAt) ? 1 : 0);
		goto IL_009b;
		IL_009b:
		bool flag2 = (byte)num3 != 0;
		ClickyInkLiveNote.Words words = item.Words;
		PaperFont font = ClickyPaperFonts.Hand(19.0);
		goneAt = (flag ? new double?(item.Words.FinishedAt) : num2);
		InkTextView inkTextView = ClickyInkLine.Make(words, clock, style, font, null, goneAt, null, fillsWidth: false);
		ClickyInkHStack clickyInkHStack = new ClickyInkHStack(6.0, InkVerticalAlignment.FirstBaseline, inkTextView);
		if (item.Source.Url != null && !flag2 && clock >= item.Words.FinishedAt && !item.Words.Current.Contains('\ue005'))
		{
			TextBlock textBlock = ClickyInkIcon.Glyph("arrow.up.right", 10.0, ClickyPaperInk.ClickyBlue.Opacity(0.8), bold: true);
			AutomationProperties.SetName(textBlock, "link");
			clickyInkHStack.Children.Add(textBlock.BaselineFromBottom(1.0));
		}
		ClickyInkDocument.Mark? mark = item.Source.Mark;
		if (mark.HasValue)
		{
			ClickyInkDocument.Mark valueOrDefault4 = mark.GetValueOrDefault();
			if ((!flag2 || flag) && clock >= item.Words.FinishedAt)
			{
				FrameworkElement frameworkElement = Tag(valueOrDefault4, style);
				if (valueOrDefault4 == ClickyInkDocument.Mark.Now)
				{
					goneAt = style.RestingSince(clock);
					if (goneAt.HasValue)
					{
						double valueOrDefault5 = goneAt.GetValueOrDefault();
						FrameworkElement frameworkElement2 = ClickyInkRestingPencil.Make(clock, valueOrDefault5, style.Ink);
						frameworkElement2.HorizontalAlignment = HorizontalAlignment.Right;
						frameworkElement2.VerticalAlignment = VerticalAlignment.Center;
						frameworkElement2.Transformed(42.0, 0.0, 0.0, 1.0, null);
						frameworkElement = new ClickyInkOverlay(frameworkElement, frameworkElement2);
					}
				}
				clickyInkHStack.Children.Add(frameworkElement);
			}
		}
		clickyInkHStack.Children.Add(new ClickyInkSpacer());
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 2.0
		};
		stackPanel.Children.Add(clickyInkHStack);
		ClickyInkLiveNote.Words detail = item.Detail;
		if ((object)detail != null && ClickyInkWords.IsStarted(detail, clock))
		{
			stackPanel.Children.Add(new InkTextView(ClickyInkWords.Text(detail, clock, style, pencil: false, style.Quiet, null), ClickyPaperFonts.Typed(12.0, null), style.Ink)
			{
				LineLimit = 3
			});
		}
		ClickyInkHStack clickyInkHStack2 = new ClickyInkHStack(9.0, InkVerticalAlignment.FirstBaseline, Marker(block, item, offset, clock, style), ClickyInkHStack.Flexible(stackPanel));
		string url = item.Source.Url;
		if (url != null)
		{
			Pressable pressable = new Pressable();
			pressable.Children.Add(clickyInkHStack2);
			pressable.Click += delegate
			{
				actions.Open(url);
			};
			return pressable;
		}
		return clickyInkHStack2;
	}

	private static FrameworkElement Marker(ClickyInkLiveNote.Block block, ClickyInkLiveNote.Item item, int offset, double clock, ClickyInkStyle style)
	{
		switch (block.Source.Kind)
		{
		case ClickyInkDocument.Kind.Checklist:
		{
			Grid grid = new Grid
			{
				Width = 16.0,
				Height = 16.0
			};
			ClickyShapeView clickyShapeView2 = ClickyShapeView.Stroke(new ClickyInkBox(offset), style.Ink.Opacity(0.55), 1.3);
			clickyShapeView2.Width = 14.0;
			clickyShapeView2.Height = 14.0;
			grid.Children.Add(clickyShapeView2);
			if (item.Source.IsChecked == true)
			{
				double num = item.Since((ClickyInkDocument.Item i) => i.IsChecked == true) ?? item.LandsAt;
				ClickyShapeView clickyShapeView3 = ClickyShapeView.Stroke(new ClickyInkTick().Trim(0.0, ClickyInkClock.EaseInOut(ClickyInkClock.Progress(clock, num, num + 0.3))), new PaperStroke(ClickyPaperInk.DoneGreen, 2.2, PenLineCap.Round, PenLineJoin.Round));
				clickyShapeView3.Width = 15.0;
				clickyShapeView3.Height = 14.0;
				clickyShapeView3.Transformed(2.0, -2.0, 0.0, 1.0, null);
				grid.Children.Add(clickyShapeView3);
			}
			else if (item.Source.Mark == ClickyInkDocument.Mark.Now)
			{
				grid.Children.Add(new Ellipse
				{
					Width = 6.0,
					Height = 6.0,
					Fill = Ui.Brush(style.Fresh)
				});
			}
			return grid.BaselineFromBottom(2.0);
		}
		case ClickyInkDocument.Kind.Numbered:
		{
			InkTextView inkTextView = InkTextView.Plain((item.Source.Number ?? (offset + 1)).ToString(CultureInfo.InvariantCulture), ClickyPaperFonts.Hand(15.0), style.Fresh);
			inkTextView.HorizontalAlignment = HorizontalAlignment.Center;
			inkTextView.VerticalAlignment = VerticalAlignment.Center;
			ClickyShapeView clickyShapeView = ClickyShapeView.Stroke(new ClickyHandRing(offset), style.Fresh.Opacity(0.6), 1.2);
			return new ClickyInkOverlay(new ClickyInkCentered(inkTextView, 20.0, 20.0), clickyShapeView);
		}
		default:
			if (item.Source.Mark != ClickyInkDocument.Mark.Pick)
			{
				return InkTextView.Plain("–", ClickyPaperFonts.Hand(19.0), style.Quiet);
			}
			return InkTextView.Plain("★", ClickyPaperFonts.Typed(13.0, null), style.Fresh);
		}
	}

	public static FrameworkElement Tag(ClickyInkDocument.Mark mark, ClickyInkStyle style)
	{
		return mark switch
		{
			ClickyInkDocument.Mark.New => ClickyInkTag.Make("new", ClickyPaperInk.DoneGreen), 
			ClickyInkDocument.Mark.Needs => ClickyInkTag.Make("needs you", PaperColor.Hex("#B86A00")), 
			ClickyInkDocument.Mark.Pick => ClickyInkTag.Make("the pick", style.Fresh), 
			ClickyInkDocument.Mark.Done => ClickyInkTag.Make("done", ClickyPaperInk.DoneGreen), 
			ClickyInkDocument.Mark.Now => ClickyInkTag.Make("now", style.Fresh), 
			_ => ClickyInkTag.Make("ruled out", PaperColor.Hex("#C2410C")), 
		};
	}
}
