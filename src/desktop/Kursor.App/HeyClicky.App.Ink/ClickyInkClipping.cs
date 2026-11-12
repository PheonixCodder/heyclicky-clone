using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkClipping
{
	private static readonly Color Newsprint = PaperColor.Hex("#26231E");

	public static FrameworkElement Create(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style)
	{
		double contentWidth = ClickyInkEnvironment.Current.ContentWidth;
		StackPanel obj = new StackPanel
		{
			Spacing = 8.0,
			Padding = new Thickness(14.0),
			Width = contentWidth - 40.0,
			Children = { (UIElement)ClickyInkWordsView.Make(block.Words, clock, style, ClickyInkObjectFonts.SerifFont(14.5, null), pencil: false, Newsprint, null).With(delegate(InkTextView t)
			{
				t.Italic = true;
				t.LineSpacing = 2.0;
			}) }
		};
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 6.0
		};
		string text = block.Source["by"];
		if (text != null)
		{
			stackPanel.Children.Add(InkTextView.Plain("— " + text, ClickyInkObjectFonts.SerifFont(11.0, FontWeights.SemiBold), Newsprint));
		}
		string text2 = block.Source["source"] ?? block.Source["url"];
		if (text2 != null)
		{
			stackPanel.Children.Add(InkTextView.Plain(Domain(text2), ClickyInkObjectFonts.SerifFont(10.5, null), Newsprint.Opacity(0.55)));
		}
		obj.Children.Add(stackPanel);
		ClickyInkOverlay clickyInkOverlay = new ClickyInkOverlay(obj).Behind(new ClickyInkShadowedShape(ClickyShape.Rectangle, "rect", PaperColor.Hex("#F1ECE1"), new PaperShadow(PaperColor.Black.Opacity(0.12), 2.5, 0.0, 1.5)));
		clickyInkOverlay.Transformed(0.0, 0.0, -1.3, 1.0, null);
		clickyInkOverlay.Margin = new Thickness(12.0, 0.0, 0.0, 0.0);
		clickyInkOverlay.HorizontalAlignment = HorizontalAlignment.Left;
		return new Grid
		{
			Width = contentWidth,
			HorizontalAlignment = HorizontalAlignment.Left,
			Children = { (UIElement)clickyInkOverlay }
		};
	}

	public static string Domain(string source)
	{
		return ClickyInkMadeLink.Domain(source);
	}
}
