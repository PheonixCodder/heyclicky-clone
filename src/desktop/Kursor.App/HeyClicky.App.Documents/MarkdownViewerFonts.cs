using HeyClicky.App.Design;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;

namespace HeyClicky.App.Documents;

public static class MarkdownViewerFonts
{
	public static readonly FontFamily Symbol = new FontFamily("Segoe UI Symbol");

	public static FontFamily Mono => DS.Fonts.Mono;

	public static FontFamily System(double size)
	{
		return DS.Fonts.For(size);
	}

	public static FontWeight Weight(MarkdownFontWeight weight)
	{
		return weight switch
		{
			MarkdownFontWeight.Bold => FontWeights.Bold, 
			MarkdownFontWeight.Semibold => FontWeights.SemiBold, 
			MarkdownFontWeight.Medium => FontWeights.Medium, 
			_ => FontWeights.Normal, 
		};
	}
}
