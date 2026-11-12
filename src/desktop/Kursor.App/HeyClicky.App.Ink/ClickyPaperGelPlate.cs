using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Ink;

public sealed class ClickyPaperGelPlate : Grid
{
	public ClickyPaperGelPlate(ClickyPaperGel gel = ClickyPaperGel.Blue, bool showsShadow = true)
	{
		base.IsHitTestVisible = false;
		PaperSurface paperSurface = new PaperSurface(ClickyShape.Capsule, PaperFill.Vertical(gel.Stops()), null).WithWash(PaperFill.Vertical((0.0, PaperColor.White.Opacity(0.88)), (0.46, PaperColor.White.Opacity(0.0)))).WithOutline(new PaperStroke(gel.Rim()));
		if (showsShadow)
		{
			paperSurface.WithShadow(PaperColor.Hex("#141E3C").Opacity(0.16), 3.0, 0.0, 3.0);
		}
		base.Children.Add(paperSurface);
	}
}
