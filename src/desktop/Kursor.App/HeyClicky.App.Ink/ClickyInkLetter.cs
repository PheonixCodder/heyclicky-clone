using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkLetter
{
	private static readonly Color LetterInk = PaperColor.Hex("#2B2620");

	private static readonly Dictionary<string, DateTimeOffset> CopiedAt = new Dictionary<string, DateTimeOffset>();

	private const double CopiedShowsForSeconds = 1.6;

	private static List<string> Choices(string? text)
	{
		return (from c in (text ?? "").Split('|')
			select c.TrimWs() into c
			where c.Length > 0
			select c).ToList();
	}

	public static FrameworkElement Create(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style, ClickyInkActions actions)
	{
		double contentWidth = ClickyInkEnvironment.Current.ContentWidth;
		ClickyInkActionFeedback feedback = actions.Feedback;
		int num;
		if (feedback.IsPending(block.Source))
		{
			ClickyInkSentAction? pending = feedback.Pending;
			num = (((object)pending != null && pending.Kind == ClickyInkNoteAction.ActionKind.Send) ? 1 : 0);
		}
		else
		{
			num = 0;
		}
		bool flag = (byte)num != 0;
		bool flag2 = flag || (object)feedback.AnswerFor(block.Source) != null;
		int num2;
		if (feedback.IsPending(block.Source))
		{
			ClickyInkSentAction? pending2 = feedback.Pending;
			num2 = (((object)pending2 != null && pending2.Kind == ClickyInkNoteAction.ActionKind.Ask) ? 1 : 0);
		}
		else
		{
			num2 = 0;
		}
		bool flag3 = (byte)num2 != 0;
		List<string> list = Choices(block.Source["tones"]);
		List<string> list2;
		if (list.Count != 0)
		{
			list2 = list.Take(3).ToList();
		}
		else
		{
			int num3 = 3;
			list2 = new List<string>(num3);
			CollectionsMarshal.SetCount(list2, num3);
			Span<string> span = CollectionsMarshal.AsSpan(list2);
			int num4 = 0;
			span[num4] = "Shorter";
			num4++;
			span[num4] = "Warmer";
			num4++;
			span[num4] = "More formal";
		}
		List<string> list3 = list2;
		Grid grid = new Grid
		{
			Height = 26.0
		};
		grid.Children.Add(ClickyShapeView.Fill(new ClickyInkFlap(), PaperColor.Hex("#E9DCC3")));
		grid.Children.Add(ClickyShapeView.Stroke(new ClickyInkFlap(), PaperColor.Hex("#C9B791"), 1.0));
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 6.0,
			Padding = new Thickness(14.0),
			Width = contentWidth - 10.0
		};
		string text = block.Source["to"];
		if (text != null)
		{
			stackPanel.Children.Add(Header("To", text, style));
		}
		string text2 = block.Source["subject"];
		if (text2 != null)
		{
			stackPanel.Children.Add(Header("Subject", text2, style));
		}
		stackPanel.Children.Add(new Border
		{
			Height = 1.0,
			Margin = new Thickness(0.0, 2.0, 0.0, 2.0),
			Background = Ui.Brush(style.Ink.Opacity(0.1))
		});
		bool isLong = block.Words.Current.CharacterCount() > 1100;
		stackPanel.Children.Add(ClickyInkWordsView.Make(block.Words, clock, style, ClickyInkObjectFonts.SerifFont(isLong ? 11.5 : 12.5, null), pencil: true, LetterInk, null).With(delegate(InkTextView t)
		{
			t.LineSpacing = (isLong ? 2 : 3);
		}));
		ClickyInkHStack clickyInkHStack = new ClickyInkHStack(8.0, InkVerticalAlignment.Center)
		{
			Margin = new Thickness(0.0, 4.0, 0.0, 0.0)
		};
		if ((object)feedback.AnswerFor(block.Source) == null)
		{
			foreach (string tone in list3)
			{
				InkTextView item = InkTextView.Plain(tone, ClickyPaperFonts.Typed(11.5, FontWeights.Medium), LetterInk.Opacity(flag3 ? 0.3 : 0.55));
				Pressable pressable = new Pressable
				{
					Children = { (UIElement)item }
				};
				pressable.Enabled = !flag3 && !flag;
				pressable.Click += delegate
				{
					actions.Perform(ClickyInkNoteActions.Retone(block.Source, tone));
				};
				AutomationProperties.SetName(pressable, tone);
				clickyInkHStack.Children.Add(pressable);
			}
		}
		clickyInkHStack.Children.Add(new ClickyInkSpacer());
		clickyInkHStack.Children.Add(CopyButton(block, actions));
		string send = block.Source["send"] ?? block.Source["ask"];
		if (send != null)
		{
			ClickyPaperGelButton clickyPaperGelButton = new ClickyPaperGelButton(flag ? "Sending…" : "Send", delegate
			{
				actions.Send(send);
			});
			clickyPaperGelButton.Enabled = !flag2;
			clickyPaperGelButton.Opacity = ((flag2 && !feedback.IsPending(block.Source)) ? 0.45 : 1.0);
			clickyInkHStack.Children.Add(clickyPaperGelButton);
		}
		clickyInkHStack.Opacity = ((clock >= block.Words.FinishedAt) ? 1 : 0);
		stackPanel.Children.Add(clickyInkHStack);
		ClickyInkOverlay item2 = new ClickyInkOverlay(stackPanel).Behind(new ClickyInkShadowedShape(ClickyShape.Rectangle, "rect", PaperColor.Hex("#FFFEFA"), new PaperShadow(PaperColor.Black.Opacity(0.12), 3.0, 0.0, 2.0)));
		return new StackPanel
		{
			Width = contentWidth - 10.0,
			Margin = new Thickness(4.0, 0.0, 0.0, 0.0),
			HorizontalAlignment = HorizontalAlignment.Left,
			Children = 
			{
				(UIElement)grid,
				(UIElement)item2
			}
		};
	}

	private static FrameworkElement CopyButton(ClickyInkLiveNote.Block block, ClickyInkActions actions)
	{
		DateTimeOffset value;
		bool flag = CopiedAt.TryGetValue(block.Id, out value) && (DateTimeOffset.Now - value).TotalSeconds < 1.6;
		ClickyPaperGelButton button = new ClickyPaperGelButton(flag ? "Copied" : "Copy", null, ClickyPaperGel.White, 26.0, 13.0, 12.0, flag ? "checkmark" : null);
		button.Click += delegate
		{
			DataPackage dataPackage = new DataPackage();
			dataPackage.SetText(block.Words.Current);
			Clipboard.SetContent(dataPackage);
			actions.Track("letter_copied", ClickyInkDocument.Kind.Letter, new JsonObject((JsonNodeOptions?)null));
			DateTimeOffset pressedAt = DateTimeOffset.Now;
			CopiedAt[block.Id] = pressedAt;
			button.AnimateTitle("Copied", "checkmark", spring: true);
			Once.After(1.6, delegate
			{
				if (CopiedAt.TryGetValue(block.Id, out var value2) && !(value2 != pressedAt))
				{
					CopiedAt.Remove(block.Id);
					button.AnimateTitle("Copy", null, spring: false);
				}
			}, DispatcherQueue.GetForCurrentThread());
		};
		return button;
	}

	private static FrameworkElement Header(string label, string value, ClickyInkStyle style)
	{
		InkTextView inkTextView = InkTextView.Plain(label, ClickyPaperFonts.Typed(10.5, FontWeights.SemiBold), style.Quiet).With(delegate(InkTextView t)
		{
			t.LineLimit = 1;
		});
		inkTextView.Width = 52.0;
		return new ClickyInkHStack(8.0, InkVerticalAlignment.Center, inkTextView, InkTextView.Plain(value, ClickyPaperFonts.Typed(12.5, FontWeights.Medium), style.Ink).With(delegate(InkTextView t)
		{
			t.LineLimit = 1;
		}));
	}
}
