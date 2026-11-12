using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyEdgeStackInkPaper : Grid
{
	public ClickyEdgeStackInkPaper(ClickyHomeSpreadPaper paper, bool isJob, bool isLifted = false, double cornerRadius = 9.0)
	{
		base.IsHitTestVisible = false;
		IClickyShape shape;
		if (!isJob)
		{
			shape = ClickyShape.RoundedRectangle(cornerRadius);
		}
		else
		{
			IClickyShape clickyShape = new ClickyNoteShape();
			shape = clickyShape;
		}
		PaperSurface paperSurface = new PaperSurface(shape, paper.Fill, 0.45).WithOutline(new PaperStroke(PaperColor.Black.Opacity(0.05), 0.5)).WithShadow(PaperColor.Hex("#0A101E").Opacity(0.16), 0.5, 0.0, 1.0).WithShadow(PaperColor.Hex("#0A1428").Opacity(isLifted ? 0.32 : 0.15), isLifted ? 22 : 6, 0.0, isLifted ? 16 : 7);
		if (isJob)
		{
			paperSurface.WithWash(PaperFill.Evenly(new Point(0.5, 0.0), new Point(0.5, 1.0), PaperColor.Hex("#78643C").Opacity(0.08), PaperColor.Clear), (Size s) => new Rect(0.0, 0.0, s.Width, 7.0));
		}
		base.Children.Add(paperSurface);
	}
}
