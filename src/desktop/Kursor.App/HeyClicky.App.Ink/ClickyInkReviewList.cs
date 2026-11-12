using System;
using System.Collections.Generic;
using System.Linq;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkReviewList
{
	private static readonly Color PadYellow = PaperColor.Hex("#FFF6CF");

	private static readonly Color PadEdge = PaperColor.Hex("#E7D58E");

	private static readonly Color MarginRed = PaperColor.Hex("#E58A8A");

	private static readonly Color Rule = PaperColor.Hex("#7A93C8").Opacity(0.26);

	private static readonly Color WaitingAmber = PaperColor.Hex("#8A5300");

	private static readonly Color Pencil = PaperColor.Hex("#3A3833");

	private const double MarginInset = 34.0;

	public static bool IsPathLike(string text)
	{
		return ClickyInkReviewListRules.IsPathLike(text);
	}

	public static FrameworkElement Create(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style, ClickyInkActions actions, double? blockGoneAt = null)
	{
		ClickyInkDocument.Block source = block.Source;
		string text = ClickyInkObjectKey.Of(source);
		ClickyInkActionFeedback feedback = actions.Feedback;
		bool isPending = feedback.Pending?.ObjectKey == text;
		ClickyInkSentAction valueOrDefault = feedback.Answered.GetValueOrDefault(text);
		IReadOnlyList<string> alreadySent = valueOrDefault?.ReviewedItems ?? Array.Empty<string>();
		int num;
		if ((object)valueOrDefault != null)
		{
			IReadOnlyList<string> reviewedItems = valueOrDefault.ReviewedItems;
			num = ((reviewedItems != null && reviewedItems.Count == 0) ? 1 : 0);
		}
		else
		{
			num = 0;
		}
		bool flag = (byte)num != 0;
		List<string> list = ClickyInkReviewRules.ItemsToSend(source, alreadySent);
		int openCount = source.Items.Count(ClickyInkReviewRules.IsOpen);
		bool isLocked = feedback.IsLocked || flag;
		double val = ((block.Items.Count == 0) ? 0.0 : block.Items.Max((ClickyInkLiveNote.Item i) => Math.Max(Math.Max(i.LandsAt, i.Words.FinishedAt), i.Detail?.FinishedAt ?? 0.0)));
		bool flag2 = clock >= Math.Max(val, block.Words.FinishedAt);
		bool isCompact = ClickyInkReviewListRules.IsCompact(block.Items.Count);
		StackPanel stackPanel = InkViews.VStack(0.0, Header(block, clock, style, isPending, openCount, flag2, flag));
		stackPanel.Children[0].SetValue(FrameworkElement.MarginProperty, new Thickness(0.0, 0.0, 0.0, 4.0));
		for (int j = 0; j < block.Items.Count; j++)
		{
			ClickyInkLiveNote.Item item = block.Items[j];
			FrameworkElement view = Row(block, item, j, clock, style, actions, blockGoneAt, isCompact, isLocked, alreadySent);
			stackPanel.Children.Add(ClickyInkGrow.Around(view, clock, item.LandsAt, item.VanishesAt));
		}
		FrameworkElement frameworkElement = Footer(source, clock, style, actions, feedback, isPending, flag, valueOrDefault, openCount, list.Count, isLocked, list);
		frameworkElement.Margin = new Thickness(0.0, 10.0, 0.0, 0.0);
		frameworkElement.Opacity = (flag2 ? 1 : 0);
		if (!flag2)
		{
			frameworkElement.IsHitTestVisible = false;
		}
		stackPanel.Children.Add(frameworkElement);
		stackPanel.Margin = new Thickness(34.0, 12.0, 14.0, 12.0);
		ClickyInkOverlay clickyInkOverlay = new ClickyInkOverlay(stackPanel).Behind(Pad());
		clickyInkOverlay.Width = 372.0;
		clickyInkOverlay.HorizontalAlignment = HorizontalAlignment.Left;
		clickyInkOverlay.Transformed(0.0, 0.0, -0.35, 1.0, null);
		return new Grid
		{
			Width = 378.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			Padding = new Thickness(3.0, 0.0, 0.0, 0.0),
			Children = { (UIElement)clickyInkOverlay }
		};
	}

	private static FrameworkElement Header(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style, bool isPending, int openCount, bool isWritten, bool wasSkipped)
	{
		FrameworkElement e = ((block.Words.Current.Length == 0) ? InkViews.Plain("Your OK", ClickyPaperFonts.Hand(21.0), style.Ink) : InkViews.Words(block.Words, clock, style, ClickyPaperFonts.Hand(21.0), null, pencil: true, delegate(InkTextView t)
		{
			t.LineLimit = 2;
		}));
		ClickyInkHStack clickyInkHStack = new ClickyInkHStack(8.0, InkVerticalAlignment.FirstBaseline, ClickyInkHStack.Flexible(e), new ClickyInkSpacer());
		if (openCount > 0 && isWritten && !wasSkipped)
		{
			clickyInkHStack.Children.Add(ClickyInkTag.Make(isPending ? "on its way" : "waiting on you", isPending ? style.Fresh : WaitingAmber));
		}
		return clickyInkHStack;
	}

	private static FrameworkElement Row(ClickyInkLiveNote.Block block, ClickyInkLiveNote.Item item, int offset, double clock, ClickyInkStyle style, ClickyInkActions actions, double? blockGoneAt, bool isCompact, bool isLocked, IReadOnlyList<string> alreadySent)
	{
		ClickyInkDocument.Item source = item.Source;
		bool flag = source.Mark == ClickyInkDocument.Mark.Done;
		bool flag2 = source.Mark == ClickyInkDocument.Mark.Gone;
		bool flag3 = ClickyInkReviewRules.WasSent(source, alreadySent);
		bool flag4 = source.IsChecked == true || flag;
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
			num3 = ((clock >= valueOrDefault3) ? 1 : 0);
		}
		else
		{
			num3 = 0;
		}
		bool flag5 = (byte)num3 != 0;
		bool canTick = !isLocked && !flag && !flag2 && !flag5 && !(flag3 && flag4);
		(ClickyInkLiveNote.Words, ClickyInkLiveNote.Words)? tuple = item.Words.Split(" — ") ?? item.Words.Split(" – ");
		ClickyInkLiveNote.Words words = (flag2 ? (item.Detail ?? tuple?.Item2) : (tuple?.Item2 ?? item.Detail));
		ClickyInkLiveNote.Words words2 = tuple?.Item1 ?? item.Words;
		double? goneAt2 = (flag2 ? new double?(item.Words.FinishedAt) : num2);
		ClickyInkHStack clickyInkHStack = new ClickyInkHStack(isCompact ? 8 : 10, InkVerticalAlignment.FirstBaseline, Box(flag4, flag, offset, item, clock));
		FrameworkElement head;
		if (isCompact)
		{
			if (!IsPathLike(words2.Current) || !(clock >= words2.FinishedAt))
			{
				goto IL_0264;
			}
			if (goneAt2.HasValue)
			{
				double valueOrDefault4 = goneAt2.GetValueOrDefault();
				if (clock >= valueOrDefault4)
				{
					goto IL_0264;
				}
			}
			head = new InkMiddleTruncatedText(ClickyInkParser.Emphasis.Strip(words2.Current), ClickyPaperFonts.Hand(17.0), flag2 ? style.Quiet : style.Ink);
			goto IL_02a2;
		}
		clickyInkHStack.Children.Add(ClickyInkHStack.Flexible(ClickyInkLine.Make(words2, clock, style, ClickyPaperFonts.Hand(19.0), flag2 ? new Color?(style.Quiet) : ((Color?)null), goneAt2, null, fillsWidth: false)));
		goto IL_03d6;
		IL_0264:
		head = ClickyInkLine.Make(words2, clock, style, ClickyPaperFonts.Hand(17.0), flag2 ? new Color?(style.Quiet) : ((Color?)null), goneAt2, 1, fillsWidth: false);
		goto IL_02a2;
		IL_02a2:
		FrameworkElement frameworkElement = null;
		if ((object)words != null)
		{
			if (ClickyInkWords.IsStarted(words, clock))
			{
				frameworkElement = InkViews.Words(words, clock, style, ClickyPaperFonts.Typed(11.0, null), style.Quiet, pencil: false, delegate(InkTextView t)
				{
					t.LineLimit = 1;
				});
			}
			else
			{
				frameworkElement = InkViews.Plain(ClickyInkParser.Emphasis.Strip(words.Current), ClickyPaperFonts.Typed(11.0, null), style.Quiet, delegate(InkTextView t)
				{
					t.LineLimit = 1;
				});
				frameworkElement.Opacity = 0.0;
			}
		}
		clickyInkHStack.Children.Add(ClickyInkHStack.Flexible(new ClickyInkReviewCompactLine(head, frameworkElement)));
		goto IL_03d6;
		IL_03d6:
		if (clock >= item.Words.FinishedAt)
		{
			FrameworkElement frameworkElement2 = null;
			if (flag)
			{
				frameworkElement2 = ClickyInkTag.Make("done", ClickyPaperInk.DoneGreen);
			}
			else if (flag2)
			{
				frameworkElement2 = ClickyInkTag.Make("couldn't", PaperColor.Hex("#C2410C"));
			}
			else if (flag3 && flag4)
			{
				frameworkElement2 = ClickyInkTag.Make("sent", style.Fresh);
			}
			else if (source.Mark == ClickyInkDocument.Mark.New)
			{
				frameworkElement2 = ClickyInkTag.Make("new", ClickyPaperInk.DoneGreen);
			}
			if ((object)frameworkElement2 != null)
			{
				clickyInkHStack.Children.Add(frameworkElement2);
			}
		}
		if (!isCompact)
		{
			clickyInkHStack.Children.Add(new ClickyInkSpacer());
		}
		StackPanel stackPanel = InkViews.VStack(1.0, clickyInkHStack);
		if (!isCompact && (object)words != null && ClickyInkWords.IsStarted(words, clock))
		{
			InkTextView inkTextView = InkViews.Words(words, clock, style, ClickyPaperFonts.Typed(11.5, null), style.Quiet, pencil: false, delegate(InkTextView t)
			{
				t.LineLimit = 2;
			});
			inkTextView.Margin = new Thickness(28.0, 0.0, 0.0, 0.0);
			stackPanel.Children.Add(inkTextView);
		}
		stackPanel.Margin = new Thickness(0.0, isCompact ? 2.5 : 4.0, 0.0, isCompact ? 2.5 : 4.0);
		Border border = new Border
		{
			Height = 1.0,
			Background = Ui.Brush(Rule),
			VerticalAlignment = VerticalAlignment.Bottom,
			IsHitTestVisible = false,
			Margin = new Thickness(-34.0, 0.0, -14.0, 0.0)
		};
		Pressable pressable = InkViews.PlainButton(new ClickyInkOverlay(stackPanel, border), delegate
		{
			if (canTick)
			{
				actions.Toggle(block.Id, item.Id);
			}
		}, ClickyInkNoteActions.Plain(source.Text));
		pressable.HorizontalAlignment = HorizontalAlignment.Stretch;
		if (!canTick)
		{
			pressable.IsHitTestVisible = false;
		}
		AutomationProperties.SetItemStatus(pressable, flag4 ? "ticked" : "not ticked");
		return pressable;
	}

	private static FrameworkElement Box(bool isChecked, bool isDone, int offset, ClickyInkLiveNote.Item item, double clock)
	{
		ClickyShapeView clickyShapeView = ClickyShapeView.Stroke(new ClickyInkBox(offset + 7), new PaperStroke(Pencil.Opacity(0.6), 1.4));
		clickyShapeView.Width = 16.0;
		clickyShapeView.Height = 16.0;
		clickyShapeView.HorizontalAlignment = HorizontalAlignment.Center;
		clickyShapeView.VerticalAlignment = VerticalAlignment.Center;
		Grid grid = new Grid
		{
			Width = 18.0,
			Height = 18.0,
			Children = { (UIElement)clickyShapeView }
		};
		if (isChecked)
		{
			double num = 1.0;
			double? num2 = item.Since((ClickyInkDocument.Item i) => i.IsChecked == true || i.Mark == ClickyInkDocument.Mark.Done);
			if (num2.HasValue)
			{
				double valueOrDefault = num2.GetValueOrDefault();
				if (valueOrDefault > item.LandsAt)
				{
					num = ClickyInkClock.EaseOut(ClickyInkClock.Progress(clock, valueOrDefault, valueOrDefault + 0.22));
				}
			}
			if (num > 0.0)
			{
				ClickyShapeView clickyShapeView2 = ClickyShapeView.Stroke(new ClickyInkTick().Trim(0.0, num), new PaperStroke(isDone ? ClickyPaperInk.DoneGreen : Pencil, 2.4, PenLineCap.Round, PenLineJoin.Round));
				clickyShapeView2.Width = 17.0;
				clickyShapeView2.Height = 16.0;
				clickyShapeView2.HorizontalAlignment = HorizontalAlignment.Center;
				clickyShapeView2.VerticalAlignment = VerticalAlignment.Center;
				clickyShapeView2.Transformed(2.0, -3.0, 0.0, 1.0, null);
				grid.Children.Add(clickyShapeView2);
			}
		}
		return grid.BaselineFromBottom(3.0);
	}

	private static FrameworkElement Footer(ClickyInkDocument.Block source, double clock, ClickyInkStyle style, ClickyInkActions actions, ClickyInkActionFeedback feedback, bool isPending, bool wasSkipped, ClickyInkSentAction? answered, int openCount, int count, bool isLocked, List<string> toSend)
	{
		if (isPending)
		{
			ClickyInkSentAction pending = feedback.Pending;
			if ((object)pending != null)
			{
				return ClickyInkYouSaidSlip.Make(pending.MarginWords, isPending: true, clock, style).Padding(-18.0, 0.0, 0.0, 0.0);
			}
		}
		if (wasSkipped && (object)answered != null)
		{
			return ClickyInkYouSaidSlip.Make(answered.MarginWords, isPending: false, clock, style).Padding(-18.0, 0.0, 0.0, 0.0);
		}
		if (openCount == 0)
		{
			ClickyShapeView clickyShapeView = ClickyShapeView.Stroke(new ClickyInkTick(), new PaperStroke(ClickyPaperInk.DoneGreen, 2.2, PenLineCap.Round, PenLineJoin.Round));
			clickyShapeView.Width = 14.0;
			clickyShapeView.Height = 12.0;
			return new ClickyInkHStack(6.0, InkVerticalAlignment.Center, clickyShapeView, InkViews.Plain("all done", ClickyPaperFonts.Hand(17.0), ClickyPaperInk.DoneGreen));
		}
		return new InkViewThatFits(new ClickyInkHStack(8.0, InkVerticalAlignment.Center, Count(), new ClickyInkSpacer
		{
			MinWidth = 4.0
		}, Buttons()), new ClickyInkHStack(8.0, InkVerticalAlignment.Center, new ClickyInkSpacer(), Buttons()), InkViews.VStack(8.0, Count(), Buttons()));
		FrameworkElement Buttons()
		{
			StackPanel stackPanel = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 8.0,
				VerticalAlignment = VerticalAlignment.Center
			};
			if (source["skip"] != null)
			{
				ClickyPaperGelButton clickyPaperGelButton = new ClickyPaperGelButton(ClickyInkNoteActions.Plain(source["skip_label"] ?? "Not now"), delegate
				{
					if (!isLocked)
					{
						ClickyInkNoteAction clickyInkNoteAction = ClickyInkNoteActions.SkipReview(source);
						if ((object)clickyInkNoteAction != null)
						{
							actions.Perform(clickyInkNoteAction);
						}
					}
				}, ClickyPaperGel.White, 30.0, 13.0, 12.5);
				if (isLocked)
				{
					clickyPaperGelButton.IsHitTestVisible = false;
				}
				stackPanel.Children.Add(clickyPaperGelButton);
			}
			ClickyPaperGelButton clickyPaperGelButton2 = new ClickyPaperGelButton(ClickyInkReviewRules.GoLabel(source, count), delegate
			{
				if (!(count <= 0 || isLocked))
				{
					ClickyInkNoteAction clickyInkNoteAction2 = ClickyInkNoteActions.Review(source, toSend, openCount);
					if ((object)clickyInkNoteAction2 != null)
					{
						actions.Perform(clickyInkNoteAction2);
					}
				}
			}, ClickyPaperGel.Blue, 30.0, 15.0, 12.5);
			if (count == 0 || isLocked)
			{
				clickyPaperGelButton2.IsHitTestVisible = false;
				clickyPaperGelButton2.Opacity = 0.45;
			}
			stackPanel.Children.Add(clickyPaperGelButton2);
			return stackPanel;
		}
		FrameworkElement Count()
		{
			return InkViews.Plain(ClickyInkReviewListRules.Count(count, openCount), ClickyPaperFonts.Hand(16.0), style.Quiet, delegate(InkTextView t)
			{
				t.LineLimit = 1;
			});
		}
	}

	private static FrameworkElement Pad()
	{
		InkPaperSheet item = new InkPaperSheet("review-pad", ClickyShape.Rectangle, PaperFill.Of(PadYellow), 0.35, new PaperShadow[2]
		{
			new PaperShadow(PaperColor.Black.Opacity(0.1), 0.6, 0.0, 0.5),
			new PaperShadow(PaperColor.Hex("#141E3C").Opacity(0.14), 6.0, 0.0, 4.0)
		});
		StackPanel item2 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 3.0,
			Margin = new Thickness(23.0, 0.0, 0.0, 0.0),
			HorizontalAlignment = HorizontalAlignment.Left,
			IsHitTestVisible = false,
			Children = 
			{
				(UIElement)new Border
				{
					Width = 1.0,
					Background = Ui.Brush(MarginRed.Opacity(0.7))
				},
				(UIElement)new Border
				{
					Width = 1.0,
					Background = Ui.Brush(MarginRed.Opacity(0.45))
				}
			}
		};
		ClickyShapeView item3 = ClickyShapeView.StrokeBorder(ClickyShape.Rectangle, new PaperStroke(PadEdge, 0.8));
		return new Grid
		{
			IsHitTestVisible = false,
			Children = 
			{
				(UIElement)item,
				(UIElement)item2,
				(UIElement)item3
			}
		};
	}
}
