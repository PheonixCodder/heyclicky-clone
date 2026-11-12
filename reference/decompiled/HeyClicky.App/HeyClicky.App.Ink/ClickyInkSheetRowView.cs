using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Ink;

public static class ClickyInkSheetRowView
{
	public static FrameworkElement Make(ClickyInkSheetRowKind kind, double clock, ClickyInkStyle style, ClickyHomeSpreadPaper paper, ClickyInkActions actions)
	{
		ClickyInkEnvironment current = ClickyInkEnvironment.Current;
		bool isWindowPreview = current.IsWindowPreview;
		double contentWidth = current.ContentWidth;
		if (!(kind is ClickyInkSheetRowKind.Title title))
		{
			if (!(kind is ClickyInkSheetRowKind.PageName pageName))
			{
				if (kind is ClickyInkSheetRowKind.Group group)
				{
					ClickyInkSheetRowKind.Resize resized = group.Resized;
					if ((object)resized != null)
					{
						FrameworkElement now = GroupView(group.Value, clock, style, paper, actions, isWindowPreview, contentWidth);
						FrameworkElement frameworkElement = GroupView(resized.Before, resized.At - 0.001, style, paper, actions, isWindowPreview, contentWidth);
						frameworkElement.Opacity = 0.0;
						frameworkElement.IsHitTestVisible = false;
						ClickyInkHeightMorph clickyInkHeightMorph = new ClickyInkHeightMorph(ClickyInkClock.Progress(clock, resized.At, resized.At + 0.4), now, frameworkElement);
						ClickyInkBottomClip.Apply(clickyInkHeightMorph);
						return ClickyInkGrow.Around(clickyInkHeightMorph, clock, group.Value.LandsAt, group.Value.VanishesAt);
					}
					return ClickyInkGrow.Around(GroupView(group.Value, clock, style, paper, actions, isWindowPreview, contentWidth), clock, group.Value.LandsAt, group.Value.VanishesAt);
				}
				return new Grid();
			}
			StackPanel stackPanel = new StackPanel
			{
				Spacing = 9.0,
				Margin = new Thickness(0.0, (pageName.Index != 0) ? 6 : 0, 0.0, 9.0)
			};
			if (pageName.Index > 0)
			{
				ClickyShapeView clickyShapeView = ClickyShapeView.Stroke(new ClickyInkSquiggle(pageName.PageID.CharacterCount()), new PaperStroke(style.Ink.Opacity(0.16), 1.1));
				clickyShapeView.Width = contentWidth;
				clickyShapeView.Height = 6.0;
				stackPanel.Children.Add(clickyShapeView);
			}
			stackPanel.Children.Add(ClickyInkWordsView.Make(pageName.Name, clock, style, ClickyPaperFonts.Typed(10.5, FontWeights.Bold), pencil: true, style.Quiet, null).With(delegate(InkTextView t)
			{
				t.Tracking = 1.1;
				t.Uppercase = true;
				t.LineLimit = 1;
			}));
			return ClickyInkGrow.Around(stackPanel.Column(contentWidth), clock, pageName.LandsAt, pageName.VanishesAt);
		}
		InkTextView inkTextView = ClickyInkWordsView.Make(title.Words, clock, style, ClickyPaperFonts.Hand(isWindowPreview ? 24 : 31), pencil: true, null, null);
		inkTextView.Margin = new Thickness(0.0, 0.0, 0.0, isWindowPreview ? 4 : 14);
		return ClickyInkGrow.Around(inkTextView.Column(contentWidth), clock, title.Words.StartsAt.GetValueOrDefault() - 0.3, null);
	}

	private static FrameworkElement GroupView(ClickyInkGroup group, double clock, ClickyInkStyle style, ClickyHomeSpreadPaper paper, ClickyInkActions actions, bool isWindowPreview, double inkWidth)
	{
		bool num = group.Kind == ClickyInkDocument.Kind.Answer;
		FrameworkElement item = ClickyInkGroupView.Make(group, clock, style, paper, actions);
		double num2 = (num ? (isWindowPreview ? 8 : 10) : 0);
		FrameworkElement frameworkElement = new Grid
		{
			Padding = new Thickness(0.0, num2, 0.0, num2),
			Children = { (UIElement)item }
		};
		if (num)
		{
			ClickyShapeView clickyShapeView = ClickyShapeView.Fill(ClickyShape.RoundedRectangle(10.0), PaperColor.Hex("#E7DBBE").Opacity(0.42));
			clickyShapeView.Margin = new Thickness(-12.0, 0.0, -12.0, 0.0);
			frameworkElement = new ClickyInkOverlay(frameworkElement).Behind(clickyShapeView);
		}
		double bottom = ((!num) ? (isWindowPreview ? 10 : 14) : (isWindowPreview ? 6 : 18));
		return new Grid
		{
			Width = inkWidth,
			HorizontalAlignment = HorizontalAlignment.Left,
			Padding = new Thickness(0.0, 0.0, 0.0, bottom),
			Children = { (UIElement)frameworkElement }
		};
	}
}
