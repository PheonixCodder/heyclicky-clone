using System;
using HeyClicky.App.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Ink;

public sealed class ClickySketchButton : Pressable
{
	private readonly ClickySketchShapeView normalPlate;

	private readonly ClickySketchShapeView hoverPlate;

	public bool IsActionEnabled
	{
		get
		{
			return base.Enabled;
		}
		set
		{
			base.Enabled = value;
			base.Opacity = (value ? 1.0 : 0.5);
		}
	}

	public static double ButtonWidth(string title, double fontSize, double horizontalPadding, bool hasImage)
	{
		double val = Math.Floor((double)(PaperText.CharacterCount(title) + (hasImage ? 3 : 0)) * fontSize * 0.46 + 2.0 * horizontalPadding);
		double num = ClickyHandLineBreaker.Width(title, ClickyPaperFonts.Hand(fontSize)) + (hasImage ? (fontSize * 1.1) : 0.0);
		return Math.Max(val, Math.Ceiling(num + 2.0 * horizontalPadding));
	}

	public ClickySketchButton(string title, Action action, ClickySketchButtonKind kind = ClickySketchButtonKind.Plain, double height = 34.0, double horizontalPadding = 16.0, double fontSize = 18.0, long seed = 4L, string? systemImage = null)
	{
		base.Width = ButtonWidth(title, fontSize, horizontalPadding, systemImage != null);
		base.Height = height;
		base.HorizontalAlignment = HorizontalAlignment.Left;
		base.VerticalAlignment = VerticalAlignment.Top;
		normalPlate = new ClickySketchShapeView(kind.Fill(), kind.Pencil(), 2.0, seed, 1.2, kind == ClickySketchButtonKind.Teal);
		hoverPlate = new ClickySketchShapeView(kind.Fill().Opacity(0.75), kind.Pencil(), 2.0, seed, 1.2, kind == ClickySketchButtonKind.Teal)
		{
			Opacity = 0.0
		};
		base.Children.Add(normalPlate);
		base.Children.Add(hoverPlate);
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = fontSize * 0.42,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		if (systemImage != null)
		{
			TextBlock textBlock = ClickyInkIcon.Glyph(systemImage, fontSize * 0.62, kind.Words(), bold: true);
			if ((object)textBlock != null)
			{
				stackPanel.Children.Add(textBlock);
			}
		}
		stackPanel.Children.Add(new ClickyHandText(title, fontSize, 1.0, 1)
		{
			Margin = new Thickness(0.0, 2.0, 0.0, 0.0)
		}.WithForeground(kind.Words()));
		base.Children.Add(stackPanel);
		base.HoverChanged += delegate(bool h)
		{
			normalPlate.Opacity = ((!h) ? 1 : 0);
			hoverPlate.Opacity = (h ? 1 : 0);
		};
		base.Click += action;
		ClickySketchPressStyle.Apply(this);
		AutomationProperties.SetName(this, title);
	}
}
