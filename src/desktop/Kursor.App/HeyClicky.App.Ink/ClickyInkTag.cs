using Microsoft.UI.Xaml;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkTag
{
	public static FrameworkElement Make(string words, Color color)
	{
		InkTextView inkTextView = InkTextView.Plain(words, ClickyPaperFonts.Hand(14.0), color);
		inkTextView.Margin = new Thickness(6.0, 0.5, 6.0, 0.5);
		ClickyShapeView clickyShapeView = ClickyShapeView.Stroke(ClickyShape.Capsule, color.Opacity(0.55), 1.0);
		ClickyInkOverlay clickyInkOverlay = new ClickyInkOverlay(inkTextView, clickyShapeView);
		clickyInkOverlay.Transformed(0.0, 0.0, -2.0, 1.0, null);
		clickyInkOverlay.HorizontalAlignment = HorizontalAlignment.Left;
		return clickyInkOverlay;
	}
}
