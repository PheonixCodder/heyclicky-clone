using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.Home;
using HeyClicky.App.Onboarding;
using HeyClicky.App.UI;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.UI;
using Windows.UI.Text;

namespace HeyClicky.App.Settings;

public static class SK
{
	public static readonly Color Destructive = DS.Rgb(1.0, 0.47, 0.44);

	public static readonly Color Positive = DS.Rgb(0.3, 0.83, 0.55);

	public static readonly Color Warning = DS.Rgb(1.0, 0.75, 0.35);

	public const string Chevron = "chevron";

	public const string NoGlyph = "";

	public const string ArrowUpRight = "arrow.up.right";

	public const string PlayFill = "play.fill";

	public static Color Ink => MessagesPalette.Current.Ink;

	public static Color TextPrimary => MessagesPalette.Current.ThemeTextPrimary;

	public static Color TextSecondary => MessagesPalette.Current.ThemeTextSecondary;

	public static Color TextTertiary => MessagesPalette.Current.ThemeTextTertiary;

	public static Color WindowBackground => MessagesPalette.Current.WindowBackground;

	public static Grid Scaffold(string title, string description, IEnumerable<FrameworkElement?> sections, double sectionSpacing = 20.0)
	{
		StackPanel item = new StackPanel
		{
			Spacing = 5.0,
			Children = 
			{
				(UIElement)Ui.Text(title, 22.0, FontWeights.SemiBold, TextPrimary),
				(UIElement)Ui.Text(description, 12.0, FontWeights.Medium, TextTertiary, TextAlignment.Left, 0, wrap: true)
			}
		};
		StackPanel stackPanel = new StackPanel
		{
			Spacing = sectionSpacing,
			Margin = new Thickness(32.0, 10.0, 32.0, 32.0),
			Children = { (UIElement)item }
		};
		foreach (FrameworkElement section in sections)
		{
			if ((object)section != null)
			{
				stackPanel.Children.Add(section);
			}
		}
		return new Grid
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			ColumnDefinitions = 
			{
				new ColumnDefinition
				{
					Width = new GridLength(1.0, GridUnitType.Star),
					MaxWidth = 684.0
				},
				new ColumnDefinition
				{
					Width = new GridLength(0.0, GridUnitType.Pixel)
				}
			},
			Children = { (UIElement)stackPanel }
		};
	}

	public static StackPanel Group(string? label, string? footnote, IEnumerable<FrameworkElement?> rows, double dividerInset = 14.0, string? caption = null)
	{
		StackPanel stackPanel = new StackPanel();
		bool flag = true;
		foreach (FrameworkElement row in rows)
		{
			if ((object)row != null)
			{
				if (!flag)
				{
					stackPanel.Children.Add(Divider(dividerInset));
				}
				stackPanel.Children.Add(row);
				flag = false;
			}
		}
		Border item = new Border
		{
			CornerRadius = new CornerRadius(14.0),
			Background = Ui.Brush(Ink.WithAlpha(0.05)),
			BorderBrush = Ui.Brush(Ink.WithAlpha(0.06)),
			BorderThickness = new Thickness(1.0),
			Child = stackPanel,
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		StackPanel stackPanel2 = new StackPanel
		{
			Spacing = 7.0
		};
		if (label != null)
		{
			if (caption == null)
			{
				stackPanel2.Children.Add(MicroLabel(label, 10.0, FontWeights.Bold, 100));
			}
			else
			{
				StackPanel stackPanel3 = Ui.H(8.0, MicroLabel(label, 10.0, FontWeights.Bold, 100), Ui.Text(caption, 11.0, FontWeights.Normal, TextSecondary));
				stackPanel3.Margin = new Thickness(4.0, 0.0, 4.0, 0.0);
				stackPanel2.Children.Add(stackPanel3);
			}
		}
		stackPanel2.Children.Add(item);
		if (footnote != null)
		{
			TextBlock textBlock = Ui.Text(footnote, 10.5, FontWeights.Medium, TextTertiary, TextAlignment.Left, 0, wrap: true);
			textBlock.Margin = new Thickness(4.0, 0.0, 4.0, 0.0);
			stackPanel2.Children.Add(textBlock);
		}
		return stackPanel2;
	}

	public static StackPanel Group(string? label, params FrameworkElement?[] rows)
	{
		return Group(label, null, rows);
	}

	public static TextBlock MicroLabel(string text, double size, FontWeight weight, int spacing)
	{
		TextBlock textBlock = Ui.Text(text.ToUpperInvariant(), size, weight, TextTertiary);
		textBlock.CharacterSpacing = spacing;
		textBlock.Margin = new Thickness(4.0, 0.0, 4.0, 0.0);
		return textBlock;
	}

	public static Border Divider(double leadingInset = 14.0)
	{
		return new Border
		{
			Height = 1.0,
			Background = Ui.Brush(Ink.WithAlpha(0.06)),
			Margin = new Thickness(leadingInset, 0.0, 0.0, 0.0)
		};
	}

	public static Grid Row(string title, string? subtitle, FrameworkElement? trailing, Color? titleColor = null)
	{
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 2.5,
			VerticalAlignment = VerticalAlignment.Center,
			Children = { (UIElement)Ui.Text(title, 13.0, FontWeights.Medium, titleColor ?? TextPrimary, TextAlignment.Left, 0, wrap: true) }
		};
		if (!string.IsNullOrEmpty(subtitle))
		{
			stackPanel.Children.Add(Ui.Text(subtitle, 11.0, FontWeights.Normal, TextTertiary, TextAlignment.Left, 0, wrap: true));
		}
		Grid grid = new Grid
		{
			Padding = new Thickness(14.0, 11.0, 14.0, 11.0),
			ColumnSpacing = 12.0
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid.Children.Add(stackPanel);
		if ((object)trailing != null)
		{
			Grid.SetColumn(trailing, 1);
			trailing.VerticalAlignment = VerticalAlignment.Center;
			grid.Children.Add(trailing);
		}
		if (trailing is Pressable && string.IsNullOrEmpty(AutomationProperties.GetName(trailing)))
		{
			AutomationProperties.SetName(trailing, title);
		}
		return grid;
	}

	public static Grid ToggleRow(string title, string? subtitle, bool isOn, Action<bool> changed)
	{
		return Row(title, subtitle, Switch(isOn, changed), null);
	}

	public static FrameworkElement TrailingSymbol(string glyph)
	{
		object glyph2;
		switch (glyph)
		{
		case "arrow.up.right":
			return Icons.Vector(8.5, TextTertiary, null, "M3.5,20.5 L20,4 M7,4 L20,4 L20,17", 3.6);
		case "play.fill":
			return Icons.Vector(9.0, TextTertiary, "M5,3.5 C5,2.4 6.2,1.7 7.2,2.3 L20.6,10.7 C21.5,11.3 21.5,12.7 20.6,13.3 L7.2,21.7 C6.2,22.3 5,21.6 5,20.5 Z");
		default:
			glyph2 = glyph;
			break;
		case "chevron":
			glyph2 = "\ue76c";
			break;
		}
		TextBlock textBlock = Icons.Glyph((string)glyph2, 10.0, TextTertiary);
		textBlock.FontWeight = FontWeights.SemiBold;
		return textBlock;
	}

	public static Pressable ActionRow(string title, string? subtitle, Action onClick, string? trailingText = null, string? trailingGlyph = "chevron", bool destructive = false)
	{
		Pressable p = new Pressable();
		StackPanel stackPanel = Ui.H(7.0);
		if (trailingText != null)
		{
			TextBlock textBlock = Ui.Text(trailingText, 11.5, FontWeights.Medium, TextTertiary, TextAlignment.Left, 1);
			textBlock.MaxWidth = 220.0;
			textBlock.TextAlignment = TextAlignment.Right;
			stackPanel.Children.Add(textBlock);
		}
		if (trailingGlyph != null && trailingGlyph.Length > 0)
		{
			stackPanel.Children.Add(TrailingSymbol(trailingGlyph));
		}
		p.Children.Add(Row(title, subtitle, (stackPanel.Children.Count > 0) ? stackPanel : null, destructive ? new Color?(Destructive) : ((Color?)null)));
		Color ink = Ink;
		p.HoverChanged += delegate(bool h)
		{
			if (p.Enabled)
			{
				p.Background = Ui.Brush(ink.WithAlpha(h ? 0.05 : 0.0));
			}
		};
		p.Click += onClick;
		return p;
	}

	public static Pressable Switch(bool on, Action<bool> changed)
	{
		Pressable p = new Pressable
		{
			Width = 46.0,
			Height = 24.0,
			AccessibleToggleState = on
		};
		Color ink = Ink;
		Color windowBackground = WindowBackground;
		ShadowLayer glow = new ShadowLayer(12.0, ink.WithAlpha(0.25), 5.0, 0.0)
		{
			Opacity = 0.0
		};
		Border track = new Border
		{
			Background = Ui.Brush(on ? ink : ink.WithAlpha(0.16))
		}.Capsule();
		GelSurface gel = new GelSurface(GelAppearance.PrimaryBlue, 12.0)
		{
			Opacity = (on ? 1.0 : 0.16),
			ShowsHoverSheen = false
		};
		Border rim = new Border
		{
			BorderThickness = new Thickness(1.0),
			BorderBrush = Ui.Brush(ink.WithAlpha(on ? 0.0 : 0.1)),
			IsHitTestVisible = false
		}.Capsule();
		ShadowLayer item = new ShadowLayer(9.0, DS.Black(on ? 0.18 : 0.08), 1.5, 0.5);
		Border knob = new Border
		{
			Background = Ui.Brush(on ? windowBackground : ink.WithAlpha(0.65))
		}.Capsule();
		Grid knobHost = new Grid
		{
			Width = 18.0,
			Height = 18.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			Margin = new Thickness(3.0, 0.0, 0.0, 0.0),
			Children = 
			{
				(UIElement)item,
				(UIElement)knob
			}
		};
		knobHost.Translation = new Vector3(on ? ((float)Travel()) : 0f, 0f, 0f);
		p.Children.Add(glow);
		p.Children.Add(track);
		p.Children.Add(gel);
		p.Children.Add(rim);
		p.Children.Add(knobHost);
		p.HoverChanged += delegate(bool h)
		{
			gel.Hovered = h;
			glow.Opacity = ((on && h) ? 1 : 0);
		};
		p.PressedChanged += delegate(bool pressed)
		{
			knobHost.Width = (pressed ? 23.0 : 18.0);
			Slide(0.18);
		};
		p.Click += delegate
		{
			on = !on;
			p.AccessibleToggleState = on;
			track.Background = Ui.Brush(on ? ink : ink.WithAlpha(0.16));
			gel.Opacity = (on ? 1.0 : 0.16);
			rim.BorderBrush = Ui.Brush(ink.WithAlpha(on ? 0.0 : 0.1));
			knob.Background = Ui.Brush(on ? windowBackground : ink.WithAlpha(0.65));
			glow.Opacity = ((on && p.IsHovered) ? 1 : 0);
			Slide(0.28);
			changed(on);
		};
		return p;
		void Slide(double seconds)
		{
			Anim.Translate(knobHost, new Vector3(on ? ((float)Travel()) : 0f, 0f, 0f), seconds, Easing.EaseInOut(Ui.Compositor));
		}
		double Travel()
		{
			return 46.0 - knobHost.Width - 6.0;
		}
	}

	public static Pressable Chip(string label, bool selected, bool gelSelection, Action onClick)
	{
		Pressable p = new Pressable
		{
			CornerRadius = new CornerRadius(100.0)
		};
		Color ink = Ink;
		GelSurface gel = null;
		if (selected && gelSelection)
		{
			gel = new GelSurface(GelAppearance.PrimaryBlue, 100.0);
			p.Children.Add(gel);
		}
		else
		{
			p.Background = Ui.Brush(selected ? Ink : Ink.WithAlpha(0.07));
		}
		TextBlock textBlock = Ui.Text(label, 11.5, FontWeights.SemiBold, (!selected) ? TextSecondary : (gelSelection ? Colors.Black : WindowBackground));
		textBlock.Margin = new Thickness(13.0, 6.0, 13.0, 6.0);
		p.Children.Add(textBlock);
		p.HoverChanged += delegate(bool h)
		{
			if ((object)gel != null)
			{
				gel.Hovered = h;
			}
			else if (!selected)
			{
				p.Background = Ui.Brush(ink.WithAlpha(h ? 0.13 : 0.07));
			}
		};
		p.Click += onClick;
		return p;
	}

	public static Pressable PillButton(string label, Action onClick, bool prominent = false, bool destructive = false, bool warning = false)
	{
		Pressable p = new Pressable
		{
			CornerRadius = new CornerRadius(100.0)
		};
		Color color = (prominent ? WindowBackground : (destructive ? Destructive : (warning ? Warning : TextPrimary)));
		Color ink = Ink;
		p.Background = Ui.Brush(Bg(hover: false));
		TextBlock textBlock = Ui.Text(label, 11.0, FontWeights.SemiBold, color);
		textBlock.Margin = new Thickness(12.0, 5.5, 12.0, 5.5);
		p.Children.Add(textBlock);
		p.HoverChanged += delegate(bool h)
		{
			p.Background = Ui.Brush(Bg(h));
		};
		p.Click += onClick;
		return p;
		Color Bg(bool hover)
		{
			if (!prominent)
			{
				return (warning ? Warning : ink).WithAlpha(hover ? 0.18 : (warning ? 0.12 : 0.08));
			}
			return ink;
		}
	}

	public static Pressable MenuPicker(IReadOnlyList<string> options, int selected, Action<int> picked, IReadOnlyList<string>? labels = null)
	{
		Pressable p = new Pressable
		{
			Padding = new Thickness(2.0, 3.0, 2.0, 3.0)
		};
		if (labels == null)
		{
			labels = options;
		}
		TextBlock label = Ui.Text(labels[Math.Clamp(selected, 0, options.Count - 1)], 11.5, FontWeights.Medium, TextSecondary, TextAlignment.Left, 1);
		TextBlock chevron = Icons.Glyph("\ue70d", 8.0, TextSecondary);
		chevron.FontWeight = FontWeights.Bold;
		p.Children.Add(Ui.H(5.0, label, chevron));
		MenuFlyout menu = new MenuFlyout
		{
			Placement = FlyoutPlacementMode.BottomEdgeAlignedRight
		};
		for (int i = 0; i < options.Count; i++)
		{
			int index = i;
			ToggleMenuFlyoutItem item = new ToggleMenuFlyoutItem
			{
				Text = options[i],
				IsChecked = (i == selected)
			};
			item.Click += delegate
			{
				label.Text = labels[index];
				foreach (ToggleMenuFlyoutItem item2 in menu.Items.OfType<ToggleMenuFlyoutItem>())
				{
					item2.IsChecked = item2 == item;
				}
				picked(index);
			};
			menu.Items.Add(item);
		}
		Color primary = TextPrimary;
		Color secondary = TextSecondary;
		p.HoverChanged += delegate(bool h)
		{
			label.Foreground = Ui.Brush(h ? primary : secondary);
			chevron.Foreground = label.Foreground;
		};
		p.Click += delegate
		{
			menu.ShowAt(p);
		};
		return p;
	}

	public static (Grid Box, TextBox Input) SearchField(string placeholder, string text, Action<string> changed, bool magnifier = true, double fontSize = 12.0, Color? placeholderColor = null)
	{
		TextBox input = new TextBox
		{
			PlaceholderText = placeholder,
			Text = text,
			FontSize = fontSize,
			FontFamily = DS.Fonts.Text,
			Padding = new Thickness(magnifier ? 26 : 11, 6.0, 9.0, 6.0),
			MinHeight = 0.0,
			IsSpellCheckEnabled = false,
			IsTextScaleFactorEnabled = false,
			BorderThickness = new Thickness(0.0),
			CornerRadius = new CornerRadius(9.0),
			Foreground = Ui.Brush(TextPrimary),
			VerticalAlignment = VerticalAlignment.Center
		};
		ResourceDictionary resources = input.Resources;
		string[] array = new string[3] { "TextControlBackground", "TextControlBackgroundPointerOver", "TextControlBackgroundFocused" };
		foreach (string key in array)
		{
			resources[key] = Ui.Brush(Ink.WithAlpha(0.07));
		}
		array = new string[3] { "TextControlBorderBrush", "TextControlBorderBrushPointerOver", "TextControlBorderBrushFocused" };
		foreach (string key2 in array)
		{
			resources[key2] = Ui.Transparent;
		}
		array = new string[3] { "TextControlForeground", "TextControlForegroundPointerOver", "TextControlForegroundFocused" };
		foreach (string key3 in array)
		{
			resources[key3] = Ui.Brush(TextPrimary);
		}
		object obj = (resources["TextControlPlaceholderForegroundFocused"] = Ui.Brush(placeholderColor ?? TextTertiary));
		object value = (resources["TextControlPlaceholderForegroundPointerOver"] = obj);
		resources["TextControlPlaceholderForeground"] = value;
		resources["TextControlBorderThemeThicknessFocused"] = new Thickness(0.0);
		input.TextChanged += delegate
		{
			changed(input.Text);
		};
		Grid grid = new Grid();
		grid.Children.Add(input);
		if (magnifier)
		{
			TextBlock textBlock = Icons.Glyph("\ue721", 11.0, TextTertiary);
			textBlock.HorizontalAlignment = HorizontalAlignment.Left;
			textBlock.Margin = new Thickness(10.0, 0.0, 0.0, 0.0);
			textBlock.IsHitTestVisible = false;
			grid.Children.Add(textBlock);
		}
		return (Box: grid, Input: input);
	}

	public static StackPanel KeyCaps(HotkeyBinding binding, bool rendersDoubleTapAsPair = true)
	{
		StackPanel stackPanel = Ui.H(4.0);
		IReadOnlyList<string> keyNames = binding.KeyNames;
		foreach (string item in (binding.Trigger == HotkeyTrigger.DoubleTap && rendersDoubleTapAsPair) ? keyNames.Concat(keyNames).ToList() : keyNames.ToList())
		{
			stackPanel.Children.Add(new Border
			{
				CornerRadius = new CornerRadius(5.0),
				Padding = new Thickness(6.0, 3.0, 6.0, 3.0),
				Background = Ui.Brush(DS.Hex(4210752u)),
				Child = Ui.Text(item, 11.0, FontWeights.SemiBold, DS.Colors.TextPrimary)
			});
		}
		ToolTipService.SetToolTip(stackPanel, binding.DisplayText);
		return stackPanel;
	}

	public static Pressable Tile(FrameworkElement content, bool selected, double radius, Action onClick, double checkSize = 20.0, bool topRightCheck = true)
	{
		Color ink = Ink;
		Pressable tile = new Pressable
		{
			CornerRadius = new CornerRadius(radius),
			Background = Ui.Brush(ink.WithAlpha(selected ? 0.09 : 0.04))
		};
		if (!selected)
		{
			tile.Children.Add(new Border
			{
				CornerRadius = new CornerRadius(radius),
				BorderBrush = Ui.Brush(ink.WithAlpha(0.12)),
				BorderThickness = new Thickness(1.0),
				IsHitTestVisible = false
			});
		}
		tile.Children.Add(content);
		if (selected)
		{
			tile.Children.Add(new GelSelectionBorder(radius)
			{
				Margin = new Thickness(0.0)
			});
			if (topRightCheck)
			{
				FrameworkElement frameworkElement = IntroWindow.GelCheckmark(checkSize);
				frameworkElement.HorizontalAlignment = HorizontalAlignment.Right;
				frameworkElement.VerticalAlignment = VerticalAlignment.Top;
				frameworkElement.Margin = new Thickness(0.0, 8.0, 8.0, 0.0);
				tile.Children.Add(frameworkElement);
			}
		}
		tile.HoverChanged += delegate(bool h)
		{
			if (!selected)
			{
				tile.Background = Ui.Brush(ink.WithAlpha(h ? 0.07 : 0.04));
			}
		};
		tile.Click += onClick;
		return tile;
	}

	public static FrameworkElement Alert(string text, Color color)
	{
		TextBlock textBlock = Icons.Glyph("\ue7ba", 10.0, color);
		TextBlock textBlock2 = Ui.Text(text, 11.0, FontWeights.SemiBold, color, TextAlignment.Left, 0, wrap: true);
		Grid obj = new Grid
		{
			ColumnSpacing = 6.0,
			ColumnDefinitions = 
			{
				new ColumnDefinition
				{
					Width = GridLength.Auto
				},
				new ColumnDefinition()
			}
		};
		textBlock.VerticalAlignment = VerticalAlignment.Top;
		textBlock.Margin = new Thickness(0.0, 2.0, 0.0, 0.0);
		Grid.SetColumn(textBlock2, 1);
		obj.Children.Add(textBlock);
		obj.Children.Add(textBlock2);
		return obj;
	}

	public static WrapPanel Wrap(double spacing)
	{
		return new WrapPanel
		{
			Spacing = spacing
		};
	}
}
