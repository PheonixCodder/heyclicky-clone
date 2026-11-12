using System;
using System.Runtime.CompilerServices;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class InkViews
{
	private static readonly ConditionalWeakTable<UIElement, Panel> holders = new ConditionalWeakTable<UIElement, Panel>();

	public static void Detach(UIElement element)
	{
		if (holders.TryGetValue(element, out Panel value))
		{
			int num = value.Children.IndexOf(element);
			if (num >= 0)
			{
				value.Children.RemoveAt(num);
			}
			holders.Remove(element);
		}
		DependencyObject dependencyObject = (element as FrameworkElement)?.Parent ?? VisualTreeHelper.GetParent(element);
		if (!(dependencyObject is Panel panel))
		{
			if (!(dependencyObject is Border border))
			{
				if (dependencyObject is ContentControl contentControl && contentControl.Content == element)
				{
					contentControl.Content = null;
				}
			}
			else if (border.Child == element)
			{
				border.Child = null;
			}
		}
		else
		{
			panel.Children.Remove(element);
		}
	}

	public static T Hold<T>(Panel panel, T element) where T : UIElement
	{
		Detach(element);
		panel.Children.Add(element);
		holders.AddOrUpdate(element, panel);
		return element;
	}

	public static T Width<T>(this T e, double width) where T : FrameworkElement
	{
		e.Width = Math.Max(0.0, width);
		e.HorizontalAlignment = HorizontalAlignment.Left;
		return e;
	}

	public static StackPanel VStack(double spacing, params UIElement?[] children)
	{
		StackPanel stackPanel = new StackPanel
		{
			Spacing = spacing,
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		foreach (UIElement uIElement in children)
		{
			if ((object)uIElement != null)
			{
				stackPanel.Children.Add(uIElement);
			}
		}
		return stackPanel;
	}

	public static InkTextView Words(ClickyInkLiveNote.Words words, double clock, ClickyInkStyle style, PaperFont font, Color? color = null, bool pencil = true, Action<InkTextView>? configure = null)
	{
		InkTextView inkTextView = ClickyInkWordsView.Make(words, clock, style, font, pencil, color, null);
		if (configure != null)
		{
			inkTextView.With(configure);
		}
		return inkTextView;
	}

	public static InkTextView Plain(string text, PaperFont font, Color color, Action<InkTextView>? configure = null)
	{
		InkTextView inkTextView = InkTextView.Plain(text, font, color);
		if (configure != null)
		{
			inkTextView.With(configure);
		}
		return inkTextView;
	}

	public static Pressable PlainButton(UIElement label, Action action, string? accessibilityName = null)
	{
		Pressable pressable = new Pressable
		{
			Background = Ui.Transparent,
			HorizontalAlignment = HorizontalAlignment.Left,
			Children = { label }
		};
		pressable.Click += action;
		if (accessibilityName != null)
		{
			AutomationProperties.SetName(pressable, accessibilityName);
		}
		return pressable;
	}

	public static ClickyPaperGelButton Gel(string title, ClickyPaperGel gel, double height, double fontSize, Action action, double horizontalPadding = 13.0, string? systemImage = null, bool isDisabled = false, double? disabledOpacity = null)
	{
		ClickyPaperGelButton clickyPaperGelButton = new ClickyPaperGelButton(title, action, gel, height, horizontalPadding, fontSize, systemImage);
		if (isDisabled)
		{
			clickyPaperGelButton.IsHitTestVisible = false;
			if (disabledOpacity.HasValue)
			{
				double valueOrDefault = disabledOpacity.GetValueOrDefault();
				clickyPaperGelButton.Opacity = valueOrDefault;
			}
		}
		return clickyPaperGelButton;
	}

	public static FrameworkElement Spinner(double size)
	{
		return new ProgressRing
		{
			Width = size,
			Height = size,
			IsActive = true,
			Foreground = Ui.Brush(PaperColor.Black.Opacity(0.5)),
			VerticalAlignment = VerticalAlignment.Center
		};
	}

	public static FrameworkElement? Symbol(string name, double size, Color color, bool bold = false)
	{
		TextBlock textBlock = ClickyInkIcon.Glyph(name, size, color, bold);
		if ((object)textBlock != null)
		{
			return textBlock;
		}
		char? c;
		switch (name)
		{
		case "film.fill":
			c = '\ue714';
			break;
		case "waveform":
			c = '\ue8d6';
			break;
		case "curlybraces":
		case "chevron.left.forwardslash.chevron.right":
			c = '\ue943';
			break;
		case "doc.zipper":
			c = '\uf012';
			break;
		case "app.badge.fill":
			c = '\ue71d';
			break;
		case "text.alignleft":
			c = '\ue8e4';
			break;
		default:
			c = null;
			break;
		}
		char? c2 = c;
		if (c2.HasValue)
		{
			char valueOrDefault = c2.GetValueOrDefault();
			return new TextBlock
			{
				Text = valueOrDefault.ToString(),
				FontFamily = DS.Fonts.Symbols,
				FontSize = size,
				FontWeight = (bold ? FontWeights.Bold : FontWeights.Normal),
				Foreground = Ui.Brush(color),
				IsTextScaleFactorEnabled = false,
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Center,
				IsHitTestVisible = false
			};
		}
		return null;
	}
}
