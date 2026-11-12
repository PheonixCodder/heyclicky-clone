using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;

namespace HeyClicky.App.Ink;

public sealed record PaperFont(FontFamily Family, string FamilyName, double Size, FontWeight Weight)
{
	public TextBlock Apply(TextBlock text)
	{
		text.FontFamily = Family;
		text.FontSize = Size;
		text.FontWeight = Weight;
		text.IsTextScaleFactorEnabled = false;
		return text;
	}

	public CanvasTextFormat TextFormat()
	{
		return new CanvasTextFormat
		{
			FontFamily = FamilyName,
			FontSize = (float)Size,
			FontWeight = Weight,
			WordWrapping = CanvasWordWrapping.NoWrap
		};
	}
}
