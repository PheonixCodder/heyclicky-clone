using System.Runtime.CompilerServices;
using HeyClicky.App.Design;
using HeyClicky.App.Home;
using Windows.UI;

namespace HeyClicky.App.Documents;

public sealed record MarkdownViewerPalette(bool IsDark, Color PageBackground, Color SecondaryTextColor, Color RaisedChipBackgroundColor, Color HeadingText, Color BodyText, Color SecondaryText, Color LinkText, Color InlineCodeBackground, Color CodeBlockBackground, Color Hairline, Color QuotePlateFill, Color QuotePlateStroke, Color QuotePlateBar, Color RaisedChipBackground)
{
	public static MarkdownViewerPalette Current => For(MessagesPalette.Current);

	public static readonly MarkdownViewerPalette Dark = new MarkdownViewerPalette(IsDark: true, Grey(0.105), Grey(0.56), Grey(0.17), Grey(0.97), Grey(0.88), Grey(0.56), DS.Rgb(0.42, 0.63, 0.98), Grey(0.2), Grey(0.155), Grey(0.24), Grey(7.0 / 51.0), Grey(13.0 / 51.0), Grey(0.6), Grey(0.17));

	public static readonly MarkdownViewerPalette Light = new MarkdownViewerPalette(IsDark: false, Grey(0.995), Grey(0.45), Grey(0.93), Grey(0.08), Grey(0.16), Grey(0.45), DS.Rgb(0.0, 0.4, 0.86), Grey(0.925), Grey(0.955), Grey(0.86), Grey(0.965), Grey(0.86), Grey(0.55), Grey(0.93));

	private static Color Grey(double white)
	{
		return DS.Rgb(white, white, white);
	}

	public static MarkdownViewerPalette For(MessagesPalette look)
	{
		if (!look.IsDark)
		{
			return Light;
		}
		return Dark;
	}

	[CompilerGenerated]
	private MarkdownViewerPalette(MarkdownViewerPalette original)
	{
		IsDark = original.IsDark;
		PageBackground = original.PageBackground;
		SecondaryTextColor = original.SecondaryTextColor;
		RaisedChipBackgroundColor = original.RaisedChipBackgroundColor;
		HeadingText = original.HeadingText;
		BodyText = original.BodyText;
		SecondaryText = original.SecondaryText;
		LinkText = original.LinkText;
		InlineCodeBackground = original.InlineCodeBackground;
		CodeBlockBackground = original.CodeBlockBackground;
		Hairline = original.Hairline;
		QuotePlateFill = original.QuotePlateFill;
		QuotePlateStroke = original.QuotePlateStroke;
		QuotePlateBar = original.QuotePlateBar;
		RaisedChipBackground = original.RaisedChipBackground;
	}
}
