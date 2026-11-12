using System;
using System.Collections.Generic;
using System.Linq;
using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace HeyClicky.App.Ink;

public static class ClickyEdgeStackInkLead
{
	public static FrameworkElement? Make(ClickyEdgeStackInkReading reading, double clock, ClickyInkStyle style, bool isLarge, bool showsSupport = true)
	{
		ClickyInkLiveNote.Block lead = reading.Lead;
		if (lead == null)
		{
			return null;
		}
		int num = ((!reading.ChangeCounts.IsEmpty) ? 1 : 0) + ((reading.CurrentState == ClickyEdgeStackInkReading.State.Writing) ? 1 : 0) + ((reading.WaitingLine != null || reading.PendingLine != null) ? 1 : 0);
		int num2 = (isLarge ? 9 : Math.Max(2, 5 - num));
		StackPanel stackPanel = new StackPanel
		{
			Spacing = (isLarge ? 8 : 6)
		};
		stackPanel.Children.Add(LeadContent(reading, lead, clock, style, isLarge, num2));
		if (showsSupport)
		{
			ClickyInkLiveNote.Block support = reading.Support;
			if (support != null && lead.Source.Kind == ClickyInkDocument.Kind.Answer)
			{
				int num3 = ((lead.Source["big"] != null) ? (isLarge ? 38 : 34) : (isLarge ? 46 : 42));
				int num4 = Math.Max(1, Math.Min(3, (lead.Words.Current.CharacterCount() + num3 - 1) / num3));
				int num5 = num2 - num4;
				if (num5 > 0)
				{
					stackPanel.Children.Add(SupportContent(reading, support, num5, clock, style, isLarge));
				}
			}
		}
		return stackPanel;
	}

	private static FrameworkElement LeadContent(ClickyEdgeStackInkReading reading, ClickyInkLiveNote.Block lead, double clock, ClickyInkStyle style, bool isLarge, int lineBudget)
	{
		PaperFont font = ClickyPaperFonts.Hand(isLarge ? 19 : 16);
		if (lead.Source.Kind == ClickyInkDocument.Kind.Answer)
		{
			string text = lead.Source["big"];
			if (text != null && text.Length > 0)
			{
				InkTextView content = ClickyInkWordsView.Make(lead.Words, clock, style, font, pencil: true, null, null).With(delegate(InkTextView t)
				{
					t.LineLimit = 3;
				});
				return new ClickyInkHStack(isLarge ? 14 : 11, InkVerticalAlignment.Center, BigNumber(text, lead, clock, style, isLarge), ClickyInkHStack.Flexible(Left(ClickyEdgeStackHighlight.Wrap(content, reading.LeadChanged, 5))))
				{
					Margin = new Thickness(3.0, 0.0, 0.0, 0.0)
				};
			}
		}
		if (lead.Items.Count > 0)
		{
			return Items(reading, lead, Math.Min(lineBudget, isLarge ? 6 : 4), clock, style, isLarge);
		}
		int limit = ((lead.Source.Kind == ClickyInkDocument.Kind.Answer) ? 3 : Math.Min(lineBudget, 5));
		return Left(ClickyEdgeStackHighlight.Wrap(ClickyInkWordsView.Make(lead.Words, clock, style, font, pencil: true, null, null).With(delegate(InkTextView t)
		{
			t.LineLimit = limit;
		}), reading.LeadChanged, 5));
	}

	private static FrameworkElement SupportContent(ClickyEdgeStackInkReading reading, ClickyInkLiveNote.Block support, int lines, double clock, ClickyInkStyle style, bool isLarge)
	{
		if (support.Items.Count > 0)
		{
			return Items(reading, support, Math.Min(lines, isLarge ? 5 : 3), clock, style, isLarge);
		}
		return ClickyInkWordsView.Make(support.Words, clock, style, ClickyPaperFonts.Hand(isLarge ? 17.0 : 14.5), pencil: true, style.Quiet, null).With(delegate(InkTextView t)
		{
			t.LineLimit = Math.Min(lines, 3);
		});
	}

	private static FrameworkElement Items(ClickyEdgeStackInkReading reading, ClickyInkLiveNote.Block block, int limit, double clock, ClickyInkStyle style, bool isLarge)
	{
		StackPanel stackPanel = new StackPanel
		{
			Spacing = (isLarge ? 5 : 3)
		};
		foreach (ClickyInkLiveNote.Item item in block.Items.Where((ClickyInkLiveNote.Item i) => !i.IsStruck(clock)).Take(limit))
		{
			FrameworkElement e = Left(ClickyEdgeStackHighlight.Wrap(ClickyInkWordsView.Make(item.Words, clock, style, ClickyPaperFonts.Hand(isLarge ? 17.5 : 15.0), pencil: true, null, null).With(delegate(InkTextView t)
			{
				t.LineLimit = 1;
			}), reading.ChangedItemIDs.Contains(item.Id), item.Id.Length));
			stackPanel.Children.Add(new ClickyInkHStack(7.0, InkVerticalAlignment.FirstBaseline, Marker(item, block, style, isLarge), ClickyInkHStack.Flexible(e)));
		}
		return stackPanel;
	}

	private static FrameworkElement BigNumber(string big, ClickyInkLiveNote.Block lead, double clock, ClickyInkStyle style, bool isLarge)
	{
		double to = ClickyInkClock.Progress(clock, lead.Words.FinishedAt, lead.Words.FinishedAt + 0.5);
		string text = lead.Source["_was"];
		double num = ((text != null) ? lead.ShownSince() : (lead.Words.StartsAt ?? lead.LandsAt));
		double num2 = ClickyInkClock.EaseOut(ClickyInkClock.Progress(clock, num, num + 0.45));
		int num3 = big.CharacterCount();
		double size = ((!isLarge) ? ((num3 > 6) ? 21 : 26) : ((num3 > 6) ? 28 : 34));
		InkTextView inkTextView = InkTextView.Plain(big, ClickyPaperFonts.BigNumber(size), style.Ink).With(delegate(InkTextView t)
		{
			t.Tracking = -0.8;
			t.MonospacedDigit = true;
			t.LineLimit = 1;
			t.MinimumScale = 0.55;
		});
		inkTextView.Margin = new Thickness(7.0, 2.0, 7.0, 2.0);
		ClickyShapeView clickyShapeView = ClickyShapeView.Stroke(new ClickyHandRing(num3).Trim(0.0, to), new PaperStroke(style.Fresh.Opacity(0.78), 1.9, PenLineCap.Round));
		clickyShapeView.Margin = new Thickness(-3.0);
		List<UIElement> list = new List<UIElement> { clickyShapeView };
		if (text != null)
		{
			InkTextView inkTextView2 = InkTextView.Plain(text, ClickyPaperFonts.BigNumber(isLarge ? 14.0 : 11.5), style.Quiet).With(delegate(InkTextView t)
			{
				t.Strikethrough = style.Fresh;
			});
			inkTextView2.HorizontalAlignment = HorizontalAlignment.Left;
			inkTextView2.VerticalAlignment = VerticalAlignment.Top;
			inkTextView2.Transformed(4.0, -14.0, -6.0, 1.0, null);
			list.Add(inkTextView2);
		}
		return new ClickyInkOverlay(inkTextView, list.ToArray())
		{
			Opacity = ((ClickyInkWords.IsStarted(lead.Words, clock) || text != null) ? num2 : 0.0),
			MaxWidth = (isLarge ? 150 : 112),
			HorizontalAlignment = HorizontalAlignment.Left
		};
	}

	private static FrameworkElement Marker(ClickyInkLiveNote.Item item, ClickyInkLiveNote.Block lead, ClickyInkStyle style, bool isLarge)
	{
		if (lead.Source.Kind == ClickyInkDocument.Kind.Checklist)
		{
			bool valueOrDefault = item.Source.IsChecked == true;
			return ((FrameworkElement)(((object)ClickyInkIcon.Glyph(valueOrDefault ? "checkmark.square" : "square", isLarge ? 12.0 : 10.5, valueOrDefault ? ClickyPaperInk.DoneGreen : style.Quiet)) ?? ((object)new Grid()))).BaselineFromBottom(isLarge ? 2.4 : 2.1);
		}
		return InkTextView.Plain("•", ClickyPaperFonts.Hand(isLarge ? 17 : 15), style.Quiet);
	}

	private static FrameworkElement Left(FrameworkElement view)
	{
		view.HorizontalAlignment = HorizontalAlignment.Left;
		return view;
	}
}
