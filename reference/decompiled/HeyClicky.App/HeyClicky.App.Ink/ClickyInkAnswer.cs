using System;
using System.Collections.Generic;
using System.Linq;
using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkAnswer
{
	public static FrameworkElement Make(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style, bool isStruck = false)
	{
		ClickyInkEnvironment current = ClickyInkEnvironment.Current;
		bool isWindowPreview = current.IsWindowPreview;
		double? num = null;
		if (block.ShownSince() > block.LandsAt + 0.001)
		{
			List<double> list = (from s in block.Words.Segments.Where(delegate(ClickyInkLiveNote.Segment s)
				{
					InkRange? written = s.Written;
					return written.HasValue && written.GetValueOrDefault().LowerBound < block.ShownSince();
				})
				select s.Written.Value.UpperBound).ToList();
			num = ((list.Count == 0) ? ((double?)null) : new double?(list.Max()));
		}
		int num2 = block.Words.Current.CharacterCount();
		bool flag = !isWindowPreview && num2 > 46;
		List<FrameworkElement> list2 = new List<FrameworkElement>();
		if (flag)
		{
			list2.Add(WordsView((num2 > 90) ? 20 : 23));
		}
		string text = block.Source["big"];
		if (text != null && text.Length > 0)
		{
			string text2 = block.Source["_was"];
			double? num3 = ((text2 != null) ? new double?(block.NumberShownSince ?? block.ShownSince()) : ((double?)null));
			double to;
			if (num3.HasValue)
			{
				double num4 = block.NumberLineWrittenAt ?? block.Words.FinishedAt;
				to = ClickyInkClock.Progress(clock, num4, num4 + 0.5);
			}
			else
			{
				double num5;
				if (num.HasValue)
				{
					double valueOrDefault = num.GetValueOrDefault();
					num5 = ClickyInkClock.Progress(clock, valueOrDefault, valueOrDefault + 0.5);
				}
				else
				{
					num5 = 0.0;
				}
				double val = num5;
				to = Math.Max(ClickyInkClock.Progress(clock, block.Words.FinishedAt, block.Words.FinishedAt + 0.5), val);
			}
			double num6 = num3 ?? (block.LandsAt + 0.13999999999999999);
			double num7 = ClickyInkClock.EaseOut(ClickyInkClock.Progress(clock, num6, num6 + 0.45));
			int num8 = text.CharacterCount();
			double size2 = ((!isWindowPreview) ? ((num8 > 6) ? 34 : 44) : ((num8 > 6) ? 28 : 38));
			InkTextView inkTextView = InkTextView.Plain(text, ClickyPaperFonts.BigNumber(size2), style.Ink).With(delegate(InkTextView t)
			{
				t.Tracking = -1.4;
				t.MonospacedDigit = true;
				t.LineLimit = 1;
				t.MinimumScale = 0.5;
				if (isStruck)
				{
					t.Strikethrough = style.Fresh;
				}
			});
			inkTextView.Margin = new Thickness(10.0, 4.0, 10.0, 4.0);
			ClickyShapeView clickyShapeView = ClickyShapeView.Stroke(new ClickyHandRing(num8).Trim(0.0, to), new PaperStroke(style.Fresh.Opacity(0.8), 2.4, PenLineCap.Round));
			clickyShapeView.Margin = new Thickness(-4.0);
			FrameworkElement frameworkElement = new ClickyInkOverlay(inkTextView, clickyShapeView);
			frameworkElement.Margin = new Thickness(6.0, 0.0, 0.0, 0.0);
			frameworkElement.Opacity = ((ClickyInkWords.IsStarted(block.Words, clock) || text2 != null) ? num7 : 0.0);
			frameworkElement.Transformed(0.0, 0.0, 0.0, 1.12 - 0.12 * num7, null);
			frameworkElement.HorizontalAlignment = HorizontalAlignment.Left;
			if (text2 != null)
			{
				FrameworkElement frameworkElement2 = ClickyInkWasValue.Make(text2, style);
				frameworkElement2.Opacity = num7;
				if (flag)
				{
					list2.Add(new ClickyInkHStack(14.0, InkVerticalAlignment.Center, frameworkElement, frameworkElement2));
				}
				else
				{
					frameworkElement2.Margin = new Thickness(8.0, frameworkElement2.Margin.Top, 0.0, frameworkElement2.Margin.Bottom);
					list2.Add(new StackPanel
					{
						Spacing = 0.0,
						Children = 
						{
							(UIElement)frameworkElement2,
							(UIElement)frameworkElement
						}
					});
				}
			}
			else
			{
				list2.Add(frameworkElement);
			}
		}
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 4.0
		};
		if (!flag)
		{
			stackPanel.Children.Add(WordsView(isWindowPreview ? 22 : 25));
		}
		string text3 = block.Source["note"];
		InkTextView inkTextView2;
		int num9;
		if (text3 != null && text3.Length > 0)
		{
			string text4 = ClickyInkIcon.SymbolName(block.Source["icon"], block.Source.Text, inferring: false);
			inkTextView2 = new InkTextView((text4 != null) ? ClickyInkTextContent.Symbol(text4, null).Append(ClickyInkTextContent.Plain(" " + text3, null)) : ClickyInkTextContent.Plain(text3, null), ClickyPaperFonts.Hand(16.0), style.Fresh);
			if (clock >= block.Words.FinishedAt)
			{
				goto IL_0650;
			}
			if (num.HasValue)
			{
				double valueOrDefault2 = num.GetValueOrDefault();
				if (clock >= valueOrDefault2)
				{
					goto IL_0650;
				}
			}
			num9 = 0;
			goto IL_0651;
		}
		goto IL_0665;
		IL_0651:
		inkTextView2.Opacity = num9;
		stackPanel.Children.Add(inkTextView2);
		goto IL_0665;
		IL_0665:
		if (stackPanel.Children.Count > 0)
		{
			list2.Add(stackPanel);
		}
		FrameworkElement frameworkElement3;
		if (flag)
		{
			StackPanel stackPanel2 = new StackPanel
			{
				Spacing = 10.0
			};
			foreach (FrameworkElement item in list2)
			{
				item.HorizontalAlignment = HorizontalAlignment.Left;
				stackPanel2.Children.Add(item);
			}
			frameworkElement3 = stackPanel2;
		}
		else
		{
			ClickyInkHStack clickyInkHStack = new ClickyInkHStack(16.0, InkVerticalAlignment.Center);
			foreach (FrameworkElement item2 in list2)
			{
				if (item2 is StackPanel)
				{
					ClickyInkHStack.Flexible(item2);
				}
				clickyInkHStack.Children.Add(item2);
			}
			frameworkElement3 = clickyInkHStack;
		}
		frameworkElement3.Width = current.ContentWidth;
		frameworkElement3.HorizontalAlignment = HorizontalAlignment.Left;
		frameworkElement3.Margin = new Thickness(0.0, 0.0, 0.0, 2.0);
		return frameworkElement3;
		IL_0650:
		num9 = 1;
		goto IL_0651;
		InkTextView WordsView(double size)
		{
			InkTextView inkTextView3 = ClickyInkWordsView.Make(block.Words, clock, style, ClickyPaperFonts.Hand(size), pencil: true, isStruck ? new Color?(style.Quiet) : ((Color?)null), null);
			if (isStruck)
			{
				inkTextView3.Strikethrough = style.Fresh;
			}
			return inkTextView3;
		}
	}
}
