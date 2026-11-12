using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using HeyClicky.App.Design;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Text;

namespace HeyClicky.App.UI;

public static class Ui
{
	private static readonly Dictionary<uint, SolidColorBrush> brushCache = new Dictionary<uint, SolidColorBrush>();

	public static readonly SolidColorBrush Transparent = new SolidColorBrush(Colors.Transparent);

	private static readonly ConditionalWeakTable<Grid, DropShadow> embosses = new ConditionalWeakTable<Grid, DropShadow>();

	public static Compositor Compositor => CompositionTarget.GetCompositorForCurrentThread();

	public static SolidColorBrush Brush(Color c)
	{
		uint key = (uint)((c.A << 24) | (c.R << 16) | (c.G << 8) | c.B);
		if (!brushCache.TryGetValue(key, out SolidColorBrush value))
		{
			value = (brushCache[key] = new SolidColorBrush(c));
		}
		return value;
	}

	public static TextBlock Text(string text, double size, FontWeight weight, Color color, TextAlignment align = TextAlignment.Left, int maxLines = 0, bool wrap = false, bool mono = false)
	{
		TextBlock textBlock = new TextBlock
		{
			Text = text,
			FontSize = size,
			FontWeight = weight,
			FontFamily = (mono ? DS.Fonts.Mono : DS.Fonts.For(size)),
			Foreground = Brush(color),
			TextAlignment = align,
			IsTextScaleFactorEnabled = false,
			TextWrapping = ((!wrap && maxLines <= 1) ? TextWrapping.NoWrap : TextWrapping.WrapWholeWords),
			TextTrimming = TextTrimming.CharacterEllipsis,
			OpticalMarginAlignment = OpticalMarginAlignment.TrimSideBearings
		};
		if (maxLines > 0)
		{
			textBlock.MaxLines = maxLines;
		}
		if (align == TextAlignment.Center)
		{
			textBlock.HorizontalAlignment = HorizontalAlignment.Center;
		}
		return textBlock;
	}

	public static FrameworkElement Shrink(TextBlock t, HorizontalAlignment align = HorizontalAlignment.Left, double minimumScale = 0.85)
	{
		return new ScaleDownText(t, minimumScale)
		{
			HorizontalAlignment = align
		};
	}

	public static TextBlock Semibold(string text, double size, Color color)
	{
		return Text(text, size, FontWeights.SemiBold, color);
	}

	public static TextBlock Medium(string text, double size, Color color)
	{
		return Text(text, size, FontWeights.Medium, color);
	}

	public static TextBlock Regular(string text, double size, Color color)
	{
		return Text(text, size, FontWeights.Normal, color);
	}

	public static Grid Embossed(TextBlock ink, double highlightAlpha = 0.8, double dy = 1.5, double radius = 1.0)
	{
		Canvas host = new Canvas
		{
			IsHitTestVisible = false
		};
		Grid grid = new Grid();
		grid.Children.Add(host);
		grid.Children.Add(ink);
		DropShadow drop = Compositor.CreateDropShadow();
		drop.Color = DS.White(highlightAlpha);
		drop.BlurRadius = (float)(radius * 2.0);
		drop.Offset = new Vector3(0f, (float)dy, 0f);
		SpriteVisual sprite = Compositor.CreateSpriteVisual();
		sprite.Shadow = drop;
		ElementCompositionPreview.SetElementChildVisual(host, sprite);
		embosses.AddOrUpdate(grid, drop);
		ink.SizeChanged += delegate
		{
			Place();
		};
		grid.SizeChanged += delegate
		{
			Place();
		};
		return grid;
		void Place()
		{
			if (!(ink.ActualWidth <= 0.0) && !(ink.ActualHeight <= 0.0))
			{
				DropShadow dropShadow = drop;
				if ((object)dropShadow.Mask == null)
				{
					CompositionBrush compositionBrush = (dropShadow.Mask = ink.GetAlphaMask());
				}
				sprite.Size = new Vector2((float)ink.ActualWidth, (float)ink.ActualHeight);
				Point point = ink.TransformToVisual(host).TransformPoint(default(Point));
				sprite.Offset = new Vector3((float)point.X, (float)point.Y, 0f);
			}
		}
	}

	public static void SetEmboss(Grid embossed, double highlightAlpha)
	{
		if (embosses.TryGetValue(embossed, out DropShadow value))
		{
			value.Color = DS.White(highlightAlpha);
		}
	}

	public static StackPanel H(double spacing, params UIElement?[] children)
	{
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = spacing
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

	public static StackPanel V(double spacing, params UIElement?[] children)
	{
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Vertical,
			Spacing = spacing
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

	public static Grid Row(double spacing, UIElement[] leading, UIElement[] trailing, double minSpacer = 0.0)
	{
		Grid grid = new Grid
		{
			ColumnSpacing = spacing
		};
		int num = 0;
		UIElement[] array = leading;
		foreach (UIElement uIElement in array)
		{
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = GridLength.Auto
			});
			Grid.SetColumn((FrameworkElement)uIElement, num++);
			grid.Children.Add(uIElement);
		}
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star),
			MinWidth = minSpacer
		});
		num++;
		array = trailing;
		foreach (UIElement uIElement2 in array)
		{
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = GridLength.Auto
			});
			Grid.SetColumn((FrameworkElement)uIElement2, num++);
			grid.Children.Add(uIElement2);
		}
		return grid;
	}

	public static Border Box(double w, double h, Color fill, double radius = 0.0)
	{
		return new Border
		{
			Width = w,
			Height = h,
			Background = Brush(fill),
			CornerRadius = new CornerRadius(radius)
		};
	}

	public static Border Capsule(this Border b)
	{
		b.SizeChanged += delegate
		{
			Fit();
		};
		return b;
		void Fit()
		{
			double num = Math.Min(b.ActualWidth, b.ActualHeight) / 2.0;
			if (num > 0.0 && b.CornerRadius.TopLeft != num)
			{
				b.CornerRadius = new CornerRadius(num);
			}
		}
	}

	public static T Pad<T>(this T e, double l, double t, double r, double b) where T : FrameworkElement
	{
		e.Margin = new Thickness(l, t, r, b);
		return e;
	}

	public static T Pad<T>(this T e, double all) where T : FrameworkElement
	{
		e.Margin = new Thickness(all);
		return e;
	}

	public static T Size<T>(this T e, double w, double h) where T : FrameworkElement
	{
		if (w > 0.0)
		{
			e.Width = w;
		}
		if (h > 0.0)
		{
			e.Height = h;
		}
		return e;
	}

	public static T Align<T>(this T e, HorizontalAlignment h, VerticalAlignment v) where T : FrameworkElement
	{
		e.HorizontalAlignment = h;
		e.VerticalAlignment = v;
		return e;
	}

	public static T Center<T>(this T e) where T : FrameworkElement
	{
		e.HorizontalAlignment = HorizontalAlignment.Center;
		e.VerticalAlignment = VerticalAlignment.Center;
		return e;
	}

	public static T Tip<T>(this T e, string tip) where T : FrameworkElement
	{
		if (!string.IsNullOrEmpty(tip))
		{
			ToolTipService.SetToolTip(e, tip);
		}
		return e;
	}

	public static LinearGradientBrush Vertical(params (double offset, Color color)[] stops)
	{
		LinearGradientBrush linearGradientBrush = new LinearGradientBrush
		{
			StartPoint = new Point(0f, 0f),
			EndPoint = new Point(0f, 1f)
		};
		for (int i = 0; i < stops.Length; i++)
		{
			var (offset, color) = stops[i];
			linearGradientBrush.GradientStops.Add(new GradientStop
			{
				Offset = offset,
				Color = color
			});
		}
		return linearGradientBrush;
	}

	public static LinearGradientBrush Horizontal(params (double offset, Color color)[] stops)
	{
		LinearGradientBrush linearGradientBrush = new LinearGradientBrush
		{
			StartPoint = new Point(0f, 0f),
			EndPoint = new Point(1f, 0f)
		};
		for (int i = 0; i < stops.Length; i++)
		{
			var (offset, color) = stops[i];
			linearGradientBrush.GradientStops.Add(new GradientStop
			{
				Offset = offset,
				Color = color
			});
		}
		return linearGradientBrush;
	}

	public static Grid GlowSpot(Color color, double alpha, double size, double solidUntil = 0.15)
	{
		Grid obj = new Grid
		{
			Width = size,
			Height = size,
			Margin = new Thickness((0.0 - size) / 2.0),
			IsHitTestVisible = false,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		Compositor compositor = Compositor;
		CompositionRadialGradientBrush compositionRadialGradientBrush = compositor.CreateRadialGradientBrush();
		compositionRadialGradientBrush.ColorStops.Add(compositor.CreateColorGradientStop(0f, color.WithAlphaFactor(alpha)));
		compositionRadialGradientBrush.ColorStops.Add(compositor.CreateColorGradientStop((float)solidUntil, color.WithAlphaFactor(alpha)));
		compositionRadialGradientBrush.ColorStops.Add(compositor.CreateColorGradientStop((float)(solidUntil + (1.0 - solidUntil) * 0.45), color.WithAlphaFactor(alpha * 0.4)));
		compositionRadialGradientBrush.ColorStops.Add(compositor.CreateColorGradientStop(1f, color.WithAlphaFactor(0.0)));
		SpriteVisual spriteVisual = compositor.CreateSpriteVisual();
		spriteVisual.Brush = compositionRadialGradientBrush;
		spriteVisual.Size = new Vector2((float)size, (float)size);
		ElementCompositionPreview.SetElementChildVisual(obj, spriteVisual);
		return obj;
	}

	private static Color WithAlphaFactor(this Color c, double a)
	{
		return Color.FromArgb((byte)Math.Round(Math.Clamp(a, 0.0, 1.0) * (double)(int)c.A), c.R, c.G, c.B);
	}

	public static void ClipRounded(FrameworkElement e, double radius)
	{
		Visual elementVisual = ElementCompositionPreview.GetElementVisual(e);
		Compositor compositor = elementVisual.Compositor;
		CompositionRoundedRectangleGeometry geo = compositor.CreateRoundedRectangleGeometry();
		geo.CornerRadius = new Vector2((float)radius);
		CompositionGeometricClip clip = compositor.CreateGeometricClip(geo);
		elementVisual.Clip = clip;
		e.SizeChanged += delegate
		{
			Update();
		};
		Update();
		void Update()
		{
			geo.Size = new Vector2((float)e.ActualWidth, (float)e.ActualHeight);
			double num = Math.Min(e.ActualWidth, e.ActualHeight) / 2.0;
			geo.CornerRadius = new Vector2((float)((num > 0.0) ? Math.Min(radius, num) : radius));
		}
	}
}
