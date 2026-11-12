using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkMadeLinkCard
{
	public static FrameworkElement Create(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style, ClickyInkActions actions, double? width = null)
	{
		double width2 = width ?? 378.0;
		string url = block.Source["url"] ?? "";
		ClickyInkMadeLink.Kind kind = ClickyInkMadeLink.KindOf(url, block.Words.Current);
		Color c = PaperColor.Hex(kind.TintHex);
		bool flag = block.IsStruck(clock);
		Grid grid = new Grid
		{
			Width = 86.0,
			Height = 68.0
		};
		grid.Children.Add(ClickyShapeView.Fill(ClickyShape.RoundedRectangle(6.0), PaperColor.White.Opacity(0.85)));
		grid.Children.Add(ClickyShapeView.Fill(ClickyShape.RoundedRectangle(6.0), c.Opacity(0.08)));
		TextBlock textBlock = ClickyInkIcon.Glyph(kind.Symbol, 26.0, c.Opacity(0.85));
		if ((object)textBlock != null)
		{
			grid.Children.Add(textBlock);
		}
		grid.Children.Add(ClickyShapeView.Stroke(ClickyShape.RoundedRectangle(6.0), style.Ink.Opacity(0.12), 1.0));
		ClickyInkOverlay clickyInkOverlay = new ClickyInkOverlay(grid).Behind(new ClickyInkShadowedShape(ClickyShape.RoundedRectangle(6.0), "rr6", PaperColor.Clear, new PaperShadow(PaperColor.Black.Opacity(0.1), 2.0, 0.0, 1.0)));
		clickyInkOverlay.Transformed(0.0, 0.0, -1.5, 1.0, null);
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 4.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		stackPanel.Children.Add(InkTextView.Plain(kind.Name.ToUpperInvariant(), ClickyPaperFonts.Typed(9.5, FontWeights.SemiBold), style.Quiet).With(delegate(InkTextView t)
		{
			t.Tracking = 0.6;
			t.LineLimit = 1;
		}));
		InkTextView inkTextView = ClickyInkWordsView.Make(block.Words, clock, style, ClickyPaperFonts.Hand(18.0), pencil: true, flag ? new Color?(style.Quiet) : ((Color?)null), null).With(delegate(InkTextView t)
		{
			t.LineLimit = 2;
		});
		if (flag)
		{
			inkTextView.Strikethrough = style.Fresh;
		}
		stackPanel.Children.Add(inkTextView);
		ClickyPaperGelButton clickyPaperGelButton = new ClickyPaperGelButton("Open", delegate
		{
			if (url.Length > 0)
			{
				actions.Open(url);
			}
		})
		{
			Enabled = (!flag && url.Length != 0)
		};
		ClickyInkHStack clickyInkHStack = new ClickyInkHStack(8.0, InkVerticalAlignment.Center, clickyPaperGelButton);
		if (url.Length > 0)
		{
			clickyInkHStack.Children.Add(InkTextView.Plain(ClickyInkClipping.Domain(url), ClickyPaperFonts.Typed(11.0, null), style.Quiet).With(delegate(InkTextView t)
			{
				t.LineLimit = 1;
			}));
		}
		clickyInkHStack.Opacity = ((clock >= block.Words.FinishedAt) ? 1 : 0);
		clickyInkHStack.HorizontalAlignment = HorizontalAlignment.Left;
		stackPanel.Children.Add(clickyInkHStack);
		return ClickyInkObjectKit.Leading(new ClickyInkHStack(12.0, InkVerticalAlignment.Center, clickyInkOverlay, ClickyInkHStack.Flexible(stackPanel), new ClickyInkSpacer()), width2);
	}
}
