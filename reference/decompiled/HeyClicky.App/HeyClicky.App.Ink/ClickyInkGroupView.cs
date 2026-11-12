using System;
using System.Collections.Generic;
using System.Linq;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace HeyClicky.App.Ink;

public static class ClickyInkGroupView
{
	private static readonly HashSet<ClickyInkDocument.Kind> ObjectKinds = new HashSet<ClickyInkDocument.Kind>
	{
		ClickyInkDocument.Kind.Sticky,
		ClickyInkDocument.Kind.Ticket,
		ClickyInkDocument.Kind.Receipt,
		ClickyInkDocument.Kind.Photo,
		ClickyInkDocument.Kind.Place,
		ClickyInkDocument.Kind.Day,
		ClickyInkDocument.Kind.Letter,
		ClickyInkDocument.Kind.Clip,
		ClickyInkDocument.Kind.File,
		ClickyInkDocument.Kind.Chart,
		ClickyInkDocument.Kind.Question,
		ClickyInkDocument.Kind.Figure,
		ClickyInkDocument.Kind.Table,
		ClickyInkDocument.Kind.Spark,
		ClickyInkDocument.Kind.Status,
		ClickyInkDocument.Kind.Progress,
		ClickyInkDocument.Kind.Tally,
		ClickyInkDocument.Kind.Streak,
		ClickyInkDocument.Kind.Countdown,
		ClickyInkDocument.Kind.Pipeline,
		ClickyInkDocument.Kind.Run,
		ClickyInkDocument.Kind.Quiet,
		ClickyInkDocument.Kind.Review,
		ClickyInkDocument.Kind.Tiles,
		ClickyInkDocument.Kind.Form,
		ClickyInkDocument.Kind.Connect,
		ClickyInkDocument.Kind.Items,
		ClickyInkDocument.Kind.Quiz,
		ClickyInkDocument.Kind.Todo,
		ClickyInkDocument.Kind.Gallery,
		ClickyInkDocument.Kind.Event,
		ClickyInkDocument.Kind.Criteria
	};

	public static bool IsObject(ClickyInkGroup group)
	{
		if (!ObjectKinds.Contains(group.Kind))
		{
			return group.IsMadeLink;
		}
		return true;
	}

	private static double EaseOut(double t)
	{
		return 1.0 - Math.Pow(1.0 - t, 3.0);
	}

	private static double EaseOutBack(double t)
	{
		return 1.0 + 2.70158 * Math.Pow(t - 1.0, 3.0) + 1.70158 * Math.Pow(t - 1.0, 2.0);
	}

	public static FrameworkElement Make(ClickyInkGroup group, double clock, ClickyInkStyle style, ClickyHomeSpreadPaper paper, ClickyInkActions actions)
	{
		ClickyInkLiveNote.Block block = group.Blocks[0];
		bool flag = IsObject(group);
		double num = ClickyInkClock.Progress(clock, block.LandsAt + 0.18, block.LandsAt + 0.55);
		double? num2 = (group.Blocks.All((ClickyInkLiveNote.Block b) => b.GoneAt.HasValue) ? new double?(group.Blocks.Max((ClickyInkLiveNote.Block b) => b.GoneAt.Value)) : ((double?)null));
		double num3;
		if (num2.HasValue)
		{
			double valueOrDefault = num2.GetValueOrDefault();
			num3 = ClickyInkClock.Progress(clock, valueOrDefault, valueOrDefault + 0.48);
		}
		else
		{
			num3 = 0.0;
		}
		double num4 = num3;
		bool flag2 = flag;
		if (flag2)
		{
			ClickyInkDocument.Kind kind = group.Kind;
			bool flag3 = ((kind == ClickyInkDocument.Kind.Table || kind == ClickyInkDocument.Kind.Status || kind == ClickyInkDocument.Kind.Tiles) ? true : false);
			flag2 = !flag3;
		}
		bool num5 = flag2;
		FrameworkElement frameworkElement = ClickyInkActedOn.Wrap(Content(group, clock, style, paper, actions), group, clock, style, actions);
		if (num4 > 0.0 && flag)
		{
			frameworkElement.Opacity *= 1.0 - 0.45 * num4;
		}
		FrameworkElement frameworkElement2 = frameworkElement;
		if (num5 && num4 > 0.0)
		{
			ClickyShapeView clickyShapeView = ClickyShapeView.Stroke(new ClickyInkCrossOut(block.Id.CharacterCount()).Trim(0.0, num4), new PaperStroke(style.Fresh.Opacity(0.85), 2.2, PenLineCap.Round));
			clickyShapeView.Margin = new Thickness(8.0, 6.0, 8.0, 6.0);
			frameworkElement2 = new ClickyInkOverlay(frameworkElement, clickyShapeView);
		}
		if (!flag)
		{
			return frameworkElement2;
		}
		frameworkElement2.Transformed(0.0, -6.0 * (1.0 - EaseOut(num)), 0.0, 1.0 + 0.05 * (1.0 - EaseOutBack(num)), null);
		frameworkElement2.Opacity *= Math.Max(0.0, Math.Min(1.0, num * 2.2));
		return frameworkElement2;
	}

	private static FrameworkElement Content(ClickyInkGroup group, double clock, ClickyInkStyle style, ClickyHomeSpreadPaper paper, ClickyInkActions actions)
	{
		ClickyInkLiveNote.Block block = group.Blocks[0];
		double contentWidth = ClickyInkEnvironment.Current.ContentWidth;
		switch (group.Kind)
		{
		case ClickyInkDocument.Kind.Answer:
			return ClickyInkAnswer.Make(block, clock, style, block.IsStruck(clock));
		case ClickyInkDocument.Kind.Heading:
		{
			ClickyInkLiveNote.Words words = block.Words;
			PaperFont font = ClickyPaperFonts.Hand(21.0);
			double? goneAt = block.GoneAt;
			return ClickyInkLine.Make(words, clock, style, font, null, goneAt, null).Padding(0.0, 4.0, 0.0, 0.0);
		}
		case ClickyInkDocument.Kind.Paragraph:
		case ClickyInkDocument.Kind.Quote:
			return ClickyInkProse.Make(block, clock, style);
		case ClickyInkDocument.Kind.Note:
			return ClickyInkMarginNote.Make(block, clock, style);
		case ClickyInkDocument.Kind.List:
		case ClickyInkDocument.Kind.Checklist:
		case ClickyInkDocument.Kind.Numbered:
			return ClickyInkList.Make(block, clock, style, actions, block.GoneAt);
		case ClickyInkDocument.Kind.Table:
			return ClickyInkTable.Create(block, clock, style, contentWidth);
		case ClickyInkDocument.Kind.Figure:
			return ClickyInkFigures.Create(group.Blocks, clock, style);
		case ClickyInkDocument.Kind.Sticky:
			return ClickyInkStickies.Create(group.Blocks, clock, style);
		case ClickyInkDocument.Kind.Ticket:
			return ClickyInkTicket.Create(block, clock, style);
		case ClickyInkDocument.Kind.Receipt:
			return ClickyInkReceipt.Create(block, clock, style);
		case ClickyInkDocument.Kind.Photo:
			return ClickyInkPhotos.Create(group.Blocks, clock, style);
		case ClickyInkDocument.Kind.Place:
			return ClickyInkPlace.Create(block, clock, style);
		case ClickyInkDocument.Kind.Day:
			return ClickyInkDay.Create(block, clock, style);
		case ClickyInkDocument.Kind.Letter:
			return ClickyInkLetter.Create(block, clock, style, actions);
		case ClickyInkDocument.Kind.Clip:
			return ClickyInkClipping.Create(block, clock, style);
		case ClickyInkDocument.Kind.Chart:
			return ClickyInkChart.Create(block, clock, style);
		case ClickyInkDocument.Kind.Link:
			if (!group.IsMadeLink)
			{
				return ClickyInkSources.Create(group.Blocks, clock, style, actions);
			}
			return ClickyInkMadeLinkCard.Create(block, clock, style, actions, null);
		case ClickyInkDocument.Kind.Ask:
			return ClickyInkAsks.Create(group.Blocks, clock, actions);
		case ClickyInkDocument.Kind.Question:
			return ClickyInkQuestion.Create(block, clock, style, actions);
		case ClickyInkDocument.Kind.File:
			if (!group.IsSupportingFiles)
			{
				return ClickyInkFileCard.Create(block, clock, style, actions);
			}
			return ClickyInkSupportingFiles.Create(group.Blocks, clock, style, actions);
		case ClickyInkDocument.Kind.Spark:
			return ClickyInkSpark.Create(block, clock, style, contentWidth);
		case ClickyInkDocument.Kind.Status:
			return ClickyInkStatusBoard.Create(group.Blocks, clock, style, contentWidth);
		case ClickyInkDocument.Kind.Progress:
			return ClickyInkProgressView.Create(block, clock, style, contentWidth);
		case ClickyInkDocument.Kind.Tally:
			return ClickyInkTally.Create(block, clock, style, contentWidth);
		case ClickyInkDocument.Kind.Streak:
			return ClickyInkStreak.Create(block, clock, style, contentWidth);
		case ClickyInkDocument.Kind.Countdown:
			return ClickyInkCountdown.Create(block, clock, style, contentWidth);
		case ClickyInkDocument.Kind.Pipeline:
			return ClickyInkPipelineView.Create(block, clock, style, contentWidth);
		case ClickyInkDocument.Kind.Run:
			return ClickyInkRunStamp.Create(block, clock, style, contentWidth, actions);
		case ClickyInkDocument.Kind.Tiles:
			return ClickyInkTiles.Create(group.Blocks, clock, style, contentWidth, null);
		case ClickyInkDocument.Kind.Quiet:
			return ClickyInkQuiet.Create(block, clock, style, contentWidth);
		case ClickyInkDocument.Kind.Review:
			return ClickyInkReviewList.Create(block, clock, style, actions, block.GoneAt);
		case ClickyInkDocument.Kind.Form:
			return ClickyInkForm.Create(block, clock, style, actions);
		case ClickyInkDocument.Kind.Connect:
			return ClickyInkConnect.Create(block, clock, style, actions);
		case ClickyInkDocument.Kind.Items:
			return ClickyInkItemsList.Create(block, clock, style, actions);
		case ClickyInkDocument.Kind.Quiz:
			return ClickyInkQuiz.Create(block, clock, style, actions);
		case ClickyInkDocument.Kind.Todo:
			return ClickyInkTodo.Create(block, clock, style, actions);
		case ClickyInkDocument.Kind.Gallery:
			return ClickyInkGallery.Create(block, clock, style, actions);
		case ClickyInkDocument.Kind.Event:
			return ClickyInkEvent.Create(block, clock, style, actions);
		case ClickyInkDocument.Kind.Criteria:
			return ClickyInkCriteria.Create(block, clock, style, actions);
		case ClickyInkDocument.Kind.Drawing:
			return ClickyInkSketch.Make(block, clock, style);
		case ClickyInkDocument.Kind.Code:
		{
			InkTextView child = new InkTextView(ClickyInkTextContent.Plain(block.Source.Text, null), new PaperFont(DS.Fonts.Mono, "Cascadia Mono", 11.0, FontWeights.Normal), style.Ink.Opacity(0.85))
			{
				LineLimit = 10,
				FillsWidth = true
			};
			return new Border
			{
				Width = contentWidth,
				HorizontalAlignment = HorizontalAlignment.Left,
				Padding = new Thickness(10.0),
				CornerRadius = new CornerRadius(8.0),
				Background = Ui.Brush(style.Ink.Opacity(0.05)),
				Child = child
			};
		}
		case ClickyInkDocument.Kind.Rule:
		{
			ClickyShapeView clickyShapeView = ClickyShapeView.Stroke(new ClickyInkSquiggle(3), new PaperStroke(style.Ink.Opacity(0.25), 1.2, PenLineCap.Round));
			clickyShapeView.Width = contentWidth * 0.5;
			clickyShapeView.Height = 6.0;
			return new Grid
			{
				Width = contentWidth,
				HorizontalAlignment = HorizontalAlignment.Left,
				Children = { (UIElement)clickyShapeView }
			};
		}
		default:
			return ClickyInkProse.Make(block, clock, style);
		}
	}
}
