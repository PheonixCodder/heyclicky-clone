using HeyClicky.App.Design;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;

namespace HeyClicky.App.Ink;

public static class InkObjectFonts
{
	private static readonly FontFamily MonoFamily = DS.Fonts.Mono;

	private static readonly FontFamily SerifFamily = new FontFamily("Cambria, Georgia");

	public static PaperFont Mono(double size, FontWeight? weight = null)
	{
		return new PaperFont(MonoFamily, "Cascadia Mono", size, weight ?? FontWeights.Normal);
	}

	public static PaperFont Serif(double size, FontWeight? weight = null)
	{
		return new PaperFont(SerifFamily, "Cambria", size, weight ?? FontWeights.Normal);
	}

	public static PaperFont Rounded(double size, FontWeight weight)
	{
		return ClickyPaperFonts.Rounded(size, weight);
	}
}
