using HeyClicky.App.Design;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;

namespace HeyClicky.App.Ink;

public static class ClickyInkObjectFonts
{
	private static readonly FontFamily Serif = new FontFamily("Georgia");

	public static PaperFont System(double size, FontWeight? weight = null)
	{
		return ClickyPaperFonts.Typed(size, weight);
	}

	public static PaperFont Mono(double size, FontWeight? weight = null)
	{
		return new PaperFont(DS.Fonts.Mono, "Cascadia Mono", size, weight ?? FontWeights.Normal);
	}

	public static PaperFont SerifFont(double size, FontWeight? weight = null)
	{
		return new PaperFont(Serif, "Georgia", size, weight ?? FontWeights.Normal);
	}

	public static PaperFont Rounded(double size, FontWeight weight)
	{
		return ClickyPaperFonts.Rounded(size, weight);
	}
}
