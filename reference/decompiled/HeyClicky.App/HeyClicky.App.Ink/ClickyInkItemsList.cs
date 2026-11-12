using System.Collections.Generic;
using System.Globalization;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkItemsList
{
	private static readonly Color Red = PaperColor.Hex("#C0392B");

	public static FrameworkElement Create(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style, ClickyInkActions actions)
	{
		double contentWidth = ClickyInkEnvironment.Current.ContentWidth;
		List<string> buttons = ClickyInkItemsRules.Buttons(block.Source);
		InkTextView e = ((block.Words.Current.Length == 0) ? InkViews.Plain(" ", ClickyPaperFonts.Hand(19.0), style.Ink) : InkViews.Words(block.Words, clock, style, ClickyPaperFonts.Hand(19.0), null));
		ClickyInkOverlay clickyInkOverlay = new ClickyInkOverlay(new ClickyInkCentered(InkViews.Plain(block.Items.Count.ToString(CultureInfo.InvariantCulture), InkObjectFonts.Rounded(12.0, FontWeights.ExtraBold), Red), 26.0, 20.0), ClickyShapeView.Stroke(new ClickyHandRing(block.Items.Count + 3), new PaperStroke(Red.Opacity(0.65), 1.3)));
		ClickyInkHStack clickyInkHStack = new ClickyInkHStack(8.0, InkVerticalAlignment.FirstBaseline, ClickyInkHStack.Flexible(e), new ClickyInkSpacer(), clickyInkOverlay)
		{
			Margin = new Thickness(0.0, 0.0, 0.0, 7.0)
		};
		StackPanel stackPanel = InkViews.VStack(0.0, clickyInkHStack, new Border
		{
			Height = 1.2,
			Background = Ui.Brush(PaperColor.Hex("#E5484D").Opacity(0.4))
		});
		for (int i = 0; i < block.Items.Count; i++)
		{
			stackPanel.Children.Add(Row(block, block.Items[i], buttons, clock, style, actions));
			if (i < block.Items.Count - 1)
			{
				stackPanel.Children.Add(new Border
				{
					Height = 1.0,
					Background = Ui.Brush(PaperColor.Hex("#6E9BD6").Opacity(0.25))
				});
			}
		}
		stackPanel.Margin = new Thickness(16.0, 15.0, 16.0, 9.0);
		InkPaperSheet background = new InkPaperSheet("items-card", ClickyShape.RoundedRectangle(3.0), PaperFill.Of(PaperColor.Hex("#FFFEF9")), 0.22, new PaperShadow[1]
		{
			new PaperShadow(PaperColor.Black.Opacity(0.07), 2.0, 0.0, 1.0)
		});
		FrameworkElement frameworkElement = ClickyInkMaskingTape.Make(60.0, -2.5, null);
		frameworkElement.HorizontalAlignment = HorizontalAlignment.Center;
		frameworkElement.VerticalAlignment = VerticalAlignment.Top;
		frameworkElement.Transformed(0.0, -7.0, 0.0, 1.0, null);
		ClickyInkOverlay clickyInkOverlay2 = new ClickyInkOverlay(stackPanel, frameworkElement).Behind(background);
		clickyInkOverlay2.Width = contentWidth - 8.0;
		clickyInkOverlay2.HorizontalAlignment = HorizontalAlignment.Left;
		clickyInkOverlay2.Transformed(0.0, 0.0, 0.15, 1.0, null);
		return new Grid
		{
			Width = contentWidth,
			HorizontalAlignment = HorizontalAlignment.Left,
			Padding = new Thickness(0.0, 6.0, 0.0, 0.0),
			Children = { (UIElement)clickyInkOverlay2 }
		};
	}

	private static FrameworkElement Row(ClickyInkLiveNote.Block block, ClickyInkLiveNote.Item item, List<string> buttons, double clock, ClickyInkStyle style, ClickyInkActions actions)
	{
		ClickyInkDocument.Item source = item.Source;
		bool flag = source.Mark == ClickyInkDocument.Mark.Done;
		bool isOut = source.Mark == ClickyInkDocument.Mark.Gone;
		string text = ClickyInkNoteActions.ItemKey(block.Source, source.Text);
		ClickyInkSentAction valueOrDefault = actions.Feedback.Answered.GetValueOrDefault(text);
		bool flag2 = actions.Feedback.Pending?.ObjectKey == text;
		InkTextView inkTextView = InkViews.Words(item.Words, clock, style, ClickyPaperFonts.Typed(13.0, (source.Mark == ClickyInkDocument.Mark.Pick) ? FontWeights.SemiBold : FontWeights.Normal), isOut ? new Color?(style.Quiet) : ((Color?)null), pencil: true, delegate(InkTextView t)
		{
			t.LineLimit = 2;
			if (isOut)
			{
				t.Strikethrough = style.Quiet;
			}
		});
		StackPanel stackPanel = InkViews.VStack(2.0, inkTextView);
		ClickyInkLiveNote.Words detail = item.Detail;
		if ((object)detail != null && ClickyInkWords.IsStarted(detail, clock))
		{
			stackPanel.Children.Add(InkViews.Words(detail, clock, style, ClickyPaperFonts.Typed(11.0, null), style.Quiet, pencil: false, delegate(InkTextView t)
			{
				t.LineLimit = 2;
			}));
		}
		FrameworkElement e = stackPanel;
		string url = source.Url;
		if (url != null)
		{
			Pressable pressable = InkViews.PlainButton(stackPanel, delegate
			{
				actions.Open(url);
			});
			pressable.HorizontalAlignment = HorizontalAlignment.Stretch;
			e = pressable;
		}
		else
		{
			stackPanel.Background = Ui.Transparent;
		}
		FrameworkElement frameworkElement = null;
		if (flag)
		{
			frameworkElement = ClickyInkTag.Make("done", ClickyPaperInk.DoneGreen);
		}
		else if (isOut)
		{
			frameworkElement = ClickyInkTag.Make("skipped", style.Quiet);
		}
		else if ((object)valueOrDefault != null)
		{
			ClickyInkHStack clickyInkHStack = new ClickyInkHStack(4.0, InkVerticalAlignment.Center);
			if (flag2)
			{
				clickyInkHStack.Children.Add(InkViews.Spinner(12.0));
			}
			clickyInkHStack.Children.Add(InkViews.Plain(valueOrDefault.MarginWords, ClickyPaperFonts.Hand(14.0), style.Fresh, delegate(InkTextView t)
			{
				t.LineLimit = 1;
			}));
			frameworkElement = clickyInkHStack;
		}
		else if (clock >= item.Words.FinishedAt)
		{
			StackPanel stackPanel2 = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 6.0,
				VerticalAlignment = VerticalAlignment.Center
			};
			for (int i = 0; i < buttons.Count; i++)
			{
				string title = buttons[i];
				stackPanel2.Children.Add(new ClickyPaperGelButton(title, delegate
				{
					ClickyInkNoteAction clickyInkNoteAction = ClickyInkNoteActions.ItemAction(block.Source, source.Text, title);
					if ((object)clickyInkNoteAction != null)
					{
						actions.Perform(clickyInkNoteAction);
					}
				}, (i != 0) ? ClickyPaperGel.White : ClickyPaperGel.Blue, 24.0, 10.0, 11.5));
			}
			frameworkElement = stackPanel2;
		}
		ClickyInkHStack clickyInkHStack2 = new ClickyInkHStack(10.0, InkVerticalAlignment.Center, ClickyInkHStack.Flexible(e), new ClickyInkSpacer())
		{
			Margin = new Thickness(0.0, 7.0, 0.0, 7.0)
		};
		if ((object)frameworkElement != null)
		{
			clickyInkHStack2.Children.Add(frameworkElement);
		}
		return clickyInkHStack2;
	}
}
