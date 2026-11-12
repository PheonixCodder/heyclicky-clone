using Microsoft.UI.Text;
using Microsoft.UI.Xaml;

namespace HeyClicky.App.Ink;

public static class ClickyInkApprovedStamp
{
	public static FrameworkElement Make(string words)
	{
		InkTextView inkTextView = InkTextView.Plain(words, ClickyPaperFonts.Rounded(10.5, FontWeights.ExtraBold), ClickyPaperInk.DoneGreen.Opacity(0.85)).With(delegate(InkTextView t)
		{
			t.Tracking = 1.2;
		});
		inkTextView.Margin = new Thickness(8.0, 3.0, 8.0, 3.0);
		ClickyShapeView clickyShapeView = ClickyShapeView.StrokeBorder(ClickyShape.RoundedRectangle(4.0), new PaperStroke(ClickyPaperInk.DoneGreen.Opacity(0.7), 1.6));
		ClickyInkOverlay clickyInkOverlay = new ClickyInkOverlay(inkTextView, clickyShapeView);
		clickyInkOverlay.Transformed(0.0, 0.0, -8.0, 1.0, null);
		return clickyInkOverlay;
	}
}
