using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace HeyClicky.App.Ink;

public sealed record PaperStroke(Color Color, double Width = 1.0, PenLineCap Cap = PenLineCap.Flat, PenLineJoin Join = PenLineJoin.Miter, double[]? Dash = null)
{
	public static PaperStroke Round(Color color, double width)
	{
		return new PaperStroke(color, width, PenLineCap.Round, PenLineJoin.Round);
	}
}
