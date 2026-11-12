using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Ink;

public sealed class PaperShadowView : Grid
{
	private readonly PaperSurface surface;

	public PaperShadowView(IClickyShape shape, params PaperShadow[] shadows)
	{
		base.IsHitTestVisible = false;
		surface = new PaperSurface(shape, PaperFill.Of(Colors.Transparent), null);
		surface.Shadows.AddRange(shadows);
		base.Children.Add(surface);
	}
}
