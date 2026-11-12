using System;
using System.Runtime.CompilerServices;
using HeyClicky.App.Design;
using Windows.UI;

namespace HeyClicky.App.Home;

public sealed record MessagesPalette(bool IsDark, Color PaneFill, Color SidebarFill, Color SearchFieldFill, Color RowDivider, Color SelectionBlue, Color OutgoingBubble, Color IncomingBubble, Color IncomingBubbleText, Color ComposerFieldFill, Color ComposerFieldStroke, Color ComposerButtonFill, Color ComposerButtonStroke, Color NamePillFill, Color AvatarPlate, Color PrimaryText, Color SecondaryText, Color TertiaryText, Color FieldGlyph, Color ToolbarGlyph, Color WindowControlGlyph, Color FailureRed, Color Ink, Color ThemeTextPrimary, Color ThemeTextSecondary, Color ThemeTextTertiary, Color ChatDots, Color WindowBackground, Color LinkBlue)
{
	public readonly struct Scope : IDisposable
	{
		private readonly MessagesPalette? _before;

		public Scope(MessagesPalette? before)
		{
			_before = before;
		}

		public void Dispose()
		{
			current = _before;
		}
	}

	public static MessagesPalette Current => current ?? Dark;

	public static readonly MessagesPalette Dark = new MessagesPalette(IsDark: true, HomeTheme.PaneFill, HomeTheme.SidebarFill, HomeTheme.SearchFieldFill, HomeTheme.RowDivider, HomeTheme.SelectionBlue, DS.Rgb(41.0 / 255.0, 151.0 / 255.0, 1.0), HomeTheme.IncomingBubble, DS.Hex(16777215u), HomeTheme.ComposerFieldFill, HomeTheme.ComposerFieldStroke, DS.Hex(2236962u), DS.Hex(4079166u), HomeTheme.NamePillFill, HomeTheme.AvatarPlate, HomeTheme.PrimaryText, HomeTheme.SecondaryText, HomeTheme.TertiaryText, DS.Hex(8750469u), HomeTheme.ToolbarGlyph, HomeTheme.WindowControlGlyph, HomeTheme.FailureRed, DS.Hex(16777215u), HomeTheme.ThemeTextPrimary, HomeTheme.ThemeTextSecondary, HomeTheme.ThemeTextTertiary, DS.White(0.07), DS.Hex(0u), DS.Colors.Blue300);

	public static readonly MessagesPalette Light = new MessagesPalette(IsDark: false, DS.Hex(16777215u), DS.Hex(15987698u), DS.Hex(15132390u), DS.Hex(14869218u), DS.Hex(31487u), DS.Rgb(41.0 / 255.0, 151.0 / 255.0, 1.0), DS.Hex(15329771u), DS.Hex(1315862u), DS.Hex(16777215u), DS.Hex(14079702u), DS.Hex(15921906u), DS.Hex(14342874u), DS.Hex(15527148u), DS.Hex(13816534u), DS.Hex(1579034u), DS.Hex(7237235u), DS.Hex(8421509u), DS.Hex(9211025u), DS.Hex(3947584u), DS.Hex(9211025u), DS.Hex(16726832u), DS.Hex(0u), DS.Rgb(0.106, 0.11, 0.122), DS.Rgb(0.353, 0.365, 0.4), DS.Rgb(0.557, 0.565, 0.6), DS.Black(0.11), DS.Rgb(0.956, 0.956, 0.964), DS.Rgb(0.0, 0.4, 0.86));

	[ThreadStatic]
	private static MessagesPalette? current;

	public Color InkAt(double alpha)
	{
		return Ink.WithAlpha(alpha);
	}

	public static Scope Use(MessagesPalette palette)
	{
		MessagesPalette? before = current;
		current = palette;
		return new Scope(before);
	}

	[CompilerGenerated]
	private MessagesPalette(MessagesPalette original)
	{
		IsDark = original.IsDark;
		PaneFill = original.PaneFill;
		SidebarFill = original.SidebarFill;
		SearchFieldFill = original.SearchFieldFill;
		RowDivider = original.RowDivider;
		SelectionBlue = original.SelectionBlue;
		OutgoingBubble = original.OutgoingBubble;
		IncomingBubble = original.IncomingBubble;
		IncomingBubbleText = original.IncomingBubbleText;
		ComposerFieldFill = original.ComposerFieldFill;
		ComposerFieldStroke = original.ComposerFieldStroke;
		ComposerButtonFill = original.ComposerButtonFill;
		ComposerButtonStroke = original.ComposerButtonStroke;
		NamePillFill = original.NamePillFill;
		AvatarPlate = original.AvatarPlate;
		PrimaryText = original.PrimaryText;
		SecondaryText = original.SecondaryText;
		TertiaryText = original.TertiaryText;
		FieldGlyph = original.FieldGlyph;
		ToolbarGlyph = original.ToolbarGlyph;
		WindowControlGlyph = original.WindowControlGlyph;
		FailureRed = original.FailureRed;
		Ink = original.Ink;
		ThemeTextPrimary = original.ThemeTextPrimary;
		ThemeTextSecondary = original.ThemeTextSecondary;
		ThemeTextTertiary = original.ThemeTextTertiary;
		ChatDots = original.ChatDots;
		WindowBackground = original.WindowBackground;
		LinkBlue = original.LinkBlue;
	}
}
