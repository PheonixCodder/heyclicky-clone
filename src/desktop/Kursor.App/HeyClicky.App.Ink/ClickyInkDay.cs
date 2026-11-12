using System.Linq;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkDay
{
	private static readonly Color Red = PaperColor.Hex("#E5484D");

	private static readonly Color DayInk = PaperColor.Hex("#2B2620");

	public static FrameworkElement Create(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style)
	{
		double contentWidth = ClickyInkEnvironment.Current.ContentWidth;
		ClickyInkDayLabel clickyInkDayLabel = new ClickyInkDayLabel(block.Words.Current);
		if (clickyInkDayLabel.Day == null)
		{
			ClickyInkHStack clickyInkHStack = new ClickyInkHStack(7.0, InkVerticalAlignment.Center);
			TextBlock textBlock = ClickyInkIcon.Glyph("calendar", 13.0, Red, bold: true);
			if ((object)textBlock != null)
			{
				clickyInkHStack.Children.Add(textBlock);
			}
			clickyInkHStack.Children.Add(ClickyInkWordsView.Make(block.Words, clock, style, ClickyPaperFonts.Hand(20.0), pencil: true, null, null).With(delegate(InkTextView t)
			{
				t.LineLimit = 1;
			}));
			return ClickyInkObjectKit.Leading(new StackPanel
			{
				Spacing = 8.0,
				Children = 
				{
					(UIElement)clickyInkHStack,
					(UIElement)Entries(block, clock, style)
				}
			}, contentWidth);
		}
		string text = clickyInkDayLabel.Weekday ?? clickyInkDayLabel.Month ?? "";
		string text2 = ((clickyInkDayLabel.Weekday != null) ? (clickyInkDayLabel.Month ?? "") : "");
		StackPanel stackPanel = new StackPanel
		{
			Width = 64.0
		};
		stackPanel.Children.Add(Band(text.ToUpperInvariant(), ClickyPaperFonts.Typed(10.0, FontWeights.Bold), PaperColor.White, 1.0, Red, 20.0));
		stackPanel.Children.Add(Band(clickyInkDayLabel.Day, ClickyInkObjectFonts.Rounded(30.0, FontWeights.ExtraBold), DayInk, 0.0, PaperColor.White, 44.0));
		stackPanel.Children.Add(Band(text2, ClickyPaperFonts.Typed(9.5, FontWeights.SemiBold), DayInk.Opacity(0.55), 0.8, PaperColor.White, 16.0));
		ClickyInkOverlay clickyInkOverlay = new ClickyInkOverlay(new Grid
		{
			Width = 64.0,
			CornerRadius = new CornerRadius(7.0),
			Children = { (UIElement)stackPanel }
		}).Behind(new ClickyInkShadowedShape(ClickyShape.RoundedRectangle(7.0), "rr7", PaperColor.Clear, new PaperShadow(PaperColor.Black.Opacity(0.12), 2.5, 0.0, 1.5)));
		clickyInkOverlay.Transformed(0.0, 0.0, -2.0, 1.0, null);
		UIElement uIElement = clickyInkOverlay;
		string caption = clickyInkDayLabel.Caption;
		if (caption != null)
		{
			InkTextView inkTextView = InkTextView.Plain(caption, ClickyPaperFonts.Hand(13.0), style.Quiet).With(delegate(InkTextView t)
			{
				t.LineLimit = 2;
				t.TextAlignment = TextAlignment.Center;
			});
			inkTextView.Width = 78.0;
			uIElement = new ClickyInkUnder(clickyInkOverlay, inkTextView, 34.0);
		}
		FrameworkElement frameworkElement = Entries(block, clock, style);
		frameworkElement.Margin = new Thickness(0.0, 2.0, 0.0, 0.0);
		return ClickyInkObjectKit.Leading(new ClickyInkHStack(14.0, InkVerticalAlignment.Top, uIElement, ClickyInkHStack.Flexible(frameworkElement)), contentWidth);
	}

	private static FrameworkElement Band(string text, PaperFont font, Color color, double tracking, Color fill, double height)
	{
		InkTextView inkTextView = InkTextView.Plain(text, font, color).With(delegate(InkTextView t)
		{
			t.Tracking = tracking;
			t.LineLimit = 1;
		});
		inkTextView.HorizontalAlignment = HorizontalAlignment.Center;
		inkTextView.VerticalAlignment = VerticalAlignment.Center;
		return new Grid
		{
			Width = 64.0,
			Height = height,
			Background = Ui.Brush(fill),
			Children = { (UIElement)inkTextView }
		};
	}

	private static FrameworkElement Entries(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style)
	{
		bool flag = block.Source.Items.Any((ClickyInkDocument.Item i) => ((i.Cells.Count > 0) ? i.Cells[0] : "").Length > 0);
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 7.0
		};
		foreach (ClickyInkLiveNote.Item item in block.Items)
		{
			if (clock < item.LandsAt)
			{
				continue;
			}
			FrameworkElement frameworkElement;
			if (flag)
			{
				InkTextView inkTextView = InkTextView.Plain((item.Source.Cells.Count > 0) ? item.Source.Cells[0] : "", ClickyInkObjectFonts.Rounded(11.5, FontWeights.SemiBold), style.Fresh).With(delegate(InkTextView t)
				{
					t.LineLimit = 1;
					t.TextAlignment = TextAlignment.Right;
					t.FillsWidth = true;
				});
				inkTextView.Width = 58.0;
				frameworkElement = inkTextView;
			}
			else
			{
				frameworkElement = InkTextView.Plain("–", ClickyPaperFonts.Hand(17.0), style.Quiet);
			}
			StackPanel stackPanel2 = new StackPanel
			{
				Spacing = 1.0
			};
			UIElementCollection children = stackPanel2.Children;
			ClickyInkLiveNote.Words words = item.Words;
			PaperFont font = ClickyPaperFonts.Hand(18.0);
			double? goneAt = item.GoneAt;
			children.Add(ClickyInkLine.Make(words, clock, style, font, null, goneAt, null));
			ClickyInkLiveNote.Words detail = item.Detail;
			if ((object)detail != null && ClickyInkWords.IsStarted(detail, clock))
			{
				stackPanel2.Children.Add(new InkTextView(ClickyInkWords.Text(detail, clock, style, pencil: false, style.Quiet, null), ClickyPaperFonts.Typed(11.5, null), style.Ink)
				{
					LineLimit = 2
				});
			}
			stackPanel.Children.Add(new ClickyInkHStack(10.0, InkVerticalAlignment.FirstBaseline, frameworkElement, ClickyInkHStack.Flexible(stackPanel2)));
		}
		return stackPanel;
	}
}
