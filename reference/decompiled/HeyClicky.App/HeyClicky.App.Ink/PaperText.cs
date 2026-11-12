using System.Globalization;
using HeyClicky.App.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class PaperText
{
	public static T CssLineBox<T>(this T element, PaperFont font, double lineHeight) where T : FrameworkElement
	{
		double num = (lineHeight - ClickyHandLineBreaker.NaturalLineHeight(font)) / 2.0;
		Thickness margin = element.Margin;
		element.Margin = new Thickness(margin.Left, margin.Top + num, margin.Right, margin.Bottom + num);
		return element;
	}

	public static TextBlock Line(string text, PaperFont font, Color color, TextAlignment alignment = TextAlignment.Left)
	{
		return font.Apply(new TextBlock
		{
			Text = text,
			Foreground = Ui.Brush(color),
			TextWrapping = TextWrapping.NoWrap,
			TextTrimming = TextTrimming.CharacterEllipsis,
			TextAlignment = alignment
		});
	}

	public static int CharacterCount(string text)
	{
		return new StringInfo(text).LengthInTextElements;
	}
}
