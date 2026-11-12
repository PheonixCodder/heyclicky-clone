using System;
using HeyClicky.App.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.System;
using Windows.UI;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkBlankLine : Grid
{
	private readonly TextBox box;

	public string Text
	{
		get
		{
			return box.Text;
		}
		set
		{
			box.Text = value;
		}
	}

	public event Action<string>? TextChanged;

	public event Action? Submitted;

	public ClickyInkBlankLine(string placeholder, Color ink, PaperFont? font = null)
	{
		base.Height = 24.0;
		PaperFont paperFont = font ?? ClickyPaperFonts.Typed(13.0, null);
		box = new TextBox
		{
			PlaceholderText = placeholder,
			FontFamily = paperFont.Family,
			FontSize = paperFont.Size,
			FontWeight = paperFont.Weight,
			IsTextScaleFactorEnabled = false,
			Foreground = Ui.Brush(ink),
			PlaceholderForeground = Ui.Brush(PaperColor.Black.Opacity(0.25)),
			Padding = new Thickness(0.0),
			MinHeight = 0.0,
			MinWidth = 0.0,
			BorderThickness = new Thickness(0.0),
			Background = Ui.Transparent,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(0.0, 0.0, 0.0, 3.0),
			AcceptsReturn = false,
			TextWrapping = TextWrapping.NoWrap
		};
		ResourceDictionary resources = box.Resources;
		string[] array = new string[8] { "TextControlBackground", "TextControlBackgroundPointerOver", "TextControlBackgroundFocused", "TextControlBackgroundDisabled", "TextControlBorderBrush", "TextControlBorderBrushPointerOver", "TextControlBorderBrushFocused", "TextControlBorderBrushDisabled" };
		foreach (string key in array)
		{
			resources[key] = Ui.Transparent;
		}
		array = new string[3] { "TextControlForeground", "TextControlForegroundPointerOver", "TextControlForegroundFocused" };
		foreach (string key2 in array)
		{
			resources[key2] = Ui.Brush(ink);
		}
		array = new string[3] { "TextControlPlaceholderForeground", "TextControlPlaceholderForegroundPointerOver", "TextControlPlaceholderForegroundFocused" };
		foreach (string key3 in array)
		{
			resources[key3] = Ui.Brush(PaperColor.Black.Opacity(0.25));
		}
		resources["TextControlBorderThemeThickness"] = new Thickness(0.0);
		resources["TextControlBorderThemeThicknessFocused"] = new Thickness(0.0);
		resources["TextControlThemePadding"] = new Thickness(0.0);
		resources["TextControlThemeMinHeight"] = 0.0;
		box.TextChanged += delegate
		{
			this.TextChanged?.Invoke(box.Text);
		};
		box.KeyDown += delegate(object _, KeyRoutedEventArgs e)
		{
			if (e.Key == VirtualKey.Enter)
			{
				e.Handled = true;
				this.Submitted?.Invoke();
			}
		};
		base.Children.Add(box);
		ClickyShapeView clickyShapeView = ClickyShapeView.Stroke(new ClickyInkBlankLeader(), new PaperStroke(ink.Opacity(0.4), 1.2, PenLineCap.Round, PenLineJoin.Miter, new double[2] { 0.1, 3.5 }));
		clickyShapeView.Height = 1.5;
		clickyShapeView.VerticalAlignment = VerticalAlignment.Bottom;
		base.Children.Add(clickyShapeView);
		AutomationProperties.SetName(box, placeholder);
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		Size size = base.MeasureOverride(availableSize);
		return new Size(double.IsInfinity(availableSize.Width) ? size.Width : availableSize.Width, size.Height);
	}
}
