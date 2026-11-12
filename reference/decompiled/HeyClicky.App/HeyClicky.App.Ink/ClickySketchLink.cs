using System;
using HeyClicky.App.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;
using Windows.UI.Text;

namespace HeyClicky.App.Ink;

public sealed class ClickySketchLink : Pressable
{
	public ClickySketchLink(string title, Action action, Color? color = null, double fontSize = 17.0)
	{
		Color c = color ?? PaperColor.Hex("#6E7FA6");
		PaperFont paperFont = ClickyPaperFonts.Hand(fontSize);
		TextBlock textBlock = paperFont.Apply(new TextBlock
		{
			Text = title,
			Foreground = Ui.Brush(c),
			TextWrapping = TextWrapping.NoWrap,
			TextDecorations = TextDecorations.Underline
		});
		double halfLeading = ClickyHandTextMetrics.For(paperFont, fontSize, 1.0).HalfLeading;
		textBlock.Margin = new Thickness(0.0, halfLeading, 0.0, halfLeading);
		base.Children.Add(textBlock);
		base.HorizontalAlignment = HorizontalAlignment.Left;
		base.VerticalAlignment = VerticalAlignment.Top;
		base.Click += action;
		ClickySketchPressStyle.Apply(this);
		AutomationProperties.SetName(this, title);
	}
}
