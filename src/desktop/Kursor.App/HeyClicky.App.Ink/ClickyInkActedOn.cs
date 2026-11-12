using System.Collections.Generic;
using System.Linq;
using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace HeyClicky.App.Ink;

public static class ClickyInkActedOn
{
	private static readonly HashSet<ClickyInkDocument.Kind> OperableKinds = new HashSet<ClickyInkDocument.Kind>
	{
		ClickyInkDocument.Kind.Ask,
		ClickyInkDocument.Kind.Question,
		ClickyInkDocument.Kind.Letter,
		ClickyInkDocument.Kind.Form,
		ClickyInkDocument.Kind.Connect,
		ClickyInkDocument.Kind.Gallery,
		ClickyInkDocument.Kind.Criteria
	};

	public static FrameworkElement Wrap(FrameworkElement content, ClickyInkGroup group, double clock, ClickyInkStyle style, ClickyInkActions actions)
	{
		if (!OperableKinds.Contains(group.Kind))
		{
			return content;
		}
		ClickyInkActionFeedback feedback = actions.Feedback;
		List<ClickyInkDocument.Block> list = (from b in @group.Blocks
			where !b.GoneAt.HasValue
			select b.Source).ToList();
		ClickyInkDocument.Block block = ((list.Count > 0) ? list[0] : null);
		(string, bool)? tuple = null;
		ClickyInkSentAction pending = feedback.Pending;
		bool flag;
		if ((object)pending != null && list.Any((ClickyInkDocument.Block s) => ClickyInkObjectKey.Of(s) == pending.ObjectKey))
		{
			tuple = (pending.MarginWords, true);
		}
		else
		{
			switch (group.Kind)
			{
			case ClickyInkDocument.Kind.Letter:
			case ClickyInkDocument.Kind.Question:
			case ClickyInkDocument.Kind.Form:
			case ClickyInkDocument.Kind.Gallery:
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			if (flag && (object)block != null)
			{
				ClickyInkSentAction clickyInkSentAction = feedback.AnswerFor(block);
				if ((object)clickyInkSentAction != null)
				{
					tuple = (clickyInkSentAction.MarginWords, false);
				}
			}
		}
		bool flag2 = group.Kind == ClickyInkDocument.Kind.Question && (object)block != null && (object)feedback.AnswerFor(block) != null;
		ClickyInkDocument.Kind kind = group.Kind;
		flag = ((kind == ClickyInkDocument.Kind.Question || kind == ClickyInkDocument.Kind.Form || kind == ClickyInkDocument.Kind.Gallery) ? true : false);
		bool flag3 = flag && (object)block != null && (object)feedback.AnswerFor(block) != null;
		bool flag4 = group.Kind == ClickyInkDocument.Kind.Letter && (object)block != null && (object)feedback.AnswerFor(block) != null;
		double num;
		switch (group.Kind)
		{
		case ClickyInkDocument.Kind.Ask:
			num = (feedback.IsLocked ? 0.45 : 1.0);
			break;
		case ClickyInkDocument.Kind.Question:
			num = (flag2 ? 0.55 : (feedback.IsLocked ? 0.7 : 1.0));
			break;
		case ClickyInkDocument.Kind.Form:
		case ClickyInkDocument.Kind.Gallery:
			num = (flag3 ? 0.6 : (feedback.IsLocked ? 0.7 : 1.0));
			break;
		case ClickyInkDocument.Kind.Connect:
		case ClickyInkDocument.Kind.Criteria:
			num = (feedback.IsLocked ? 0.7 : 1.0);
			break;
		default:
			num = 1.0;
			break;
		}
		double num2 = num;
		if (group.Kind != ClickyInkDocument.Kind.Letter && (feedback.IsLocked || flag3))
		{
			content.IsHitTestVisible = false;
			if (content is Control control)
			{
				control.IsEnabled = false;
			}
		}
		content.Opacity *= num2;
		List<UIElement> list2 = new List<UIElement>();
		if (flag2)
		{
			ClickyShapeView clickyShapeView = ClickyShapeView.Stroke(new ClickyInkCrossOut(group.Id.CharacterCount()), new PaperStroke(style.Fresh.Opacity(0.8), 2.0, PenLineCap.Round));
			clickyShapeView.Margin = new Thickness(10.0, 8.0, 10.0, 8.0);
			list2.Add(clickyShapeView);
		}
		if (flag4)
		{
			FrameworkElement frameworkElement = ClickyInkApprovedStamp.Make("OK TO SEND");
			frameworkElement.HorizontalAlignment = HorizontalAlignment.Right;
			frameworkElement.VerticalAlignment = VerticalAlignment.Top;
			frameworkElement.Transformed(-14.0, 30.0, 0.0, 1.0, null);
			frameworkElement.IsHitTestVisible = false;
			list2.Add(frameworkElement);
		}
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 5.0
		};
		stackPanel.Children.Add((list2.Count > 0) ? new ClickyInkOverlay(content, list2.ToArray()) : content);
		if (tuple.HasValue)
		{
			(string, bool) valueOrDefault = tuple.GetValueOrDefault();
			stackPanel.Children.Add(ClickyInkYouSaidSlip.Make(valueOrDefault.Item1, valueOrDefault.Item2, clock, style));
		}
		return stackPanel;
	}
}
