using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkPencilGlyph
{
	public static FrameworkElement Make(double size, Color tint, bool bold = false)
	{
		TextBlock item = ClickyInkIcon.Glyph("pencil", size * 0.92, tint, bold) ?? new TextBlock();
		return new Grid
		{
			Width = size,
			Height = size,
			IsHitTestVisible = false,
			Children = { (UIElement)item }
		};
	}
}
