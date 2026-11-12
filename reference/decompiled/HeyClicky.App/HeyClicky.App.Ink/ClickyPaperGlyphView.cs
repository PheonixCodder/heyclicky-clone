using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace HeyClicky.App.Ink;

public sealed class ClickyPaperGlyphView : Grid
{
	public ClickyPaperGlyphView(ClickyPaperGlyph glyph, Color color, double size = 17.0, double lineWidth = 1.8)
	{
		base.Width = (base.Height = size);
		base.IsHitTestVisible = false;
		double num2 = size / 24.0;
		base.Children.Add(new PaperDrawing().Stroke(glyph.Path().Transformed(num2, num2), new PaperStroke(color, lineWidth, PenLineCap.Round, PenLineJoin.Round)));
	}
}
