using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public static class InkViewModifiers
{
	public static T Padding<T>(this T e, double left, double top, double right, double bottom) where T : FrameworkElement
	{
		Thickness margin = e.Margin;
		e.Margin = new Thickness(margin.Left + left, margin.Top + top, margin.Right + right, margin.Bottom + bottom);
		return e;
	}

	public static T WithOpacity<T>(this T e, double opacity) where T : UIElement
	{
		e.Opacity *= Math.Clamp(opacity, 0.0, 1.0);
		return e;
	}

	public static T Transformed<T>(this T e, double x = 0.0, double y = 0.0, double degrees = 0.0, double scale = 1.0, Point? anchor = null) where T : UIElement
	{
		TransformGroup transformGroup = (e.RenderTransform as TransformGroup) ?? new TransformGroup();
		if (scale != 1.0)
		{
			transformGroup.Children.Add(new ScaleTransform
			{
				ScaleX = scale,
				ScaleY = scale
			});
		}
		if (degrees != 0.0)
		{
			transformGroup.Children.Add(new RotateTransform
			{
				Angle = degrees
			});
		}
		if (x != 0.0 || y != 0.0)
		{
			transformGroup.Children.Add(new TranslateTransform
			{
				X = x,
				Y = y
			});
		}
		e.RenderTransform = transformGroup;
		e.RenderTransformOrigin = anchor ?? new Point(0.5, 0.5);
		return e;
	}

	public static T Frame<T>(this T e, double? width = null, double? height = null) where T : FrameworkElement
	{
		if (width.HasValue)
		{
			double valueOrDefault = width.GetValueOrDefault();
			e.Width = Math.Max(0.0, valueOrDefault);
		}
		if (height.HasValue)
		{
			double valueOrDefault2 = height.GetValueOrDefault();
			e.Height = Math.Max(0.0, valueOrDefault2);
		}
		return e;
	}

	public static FrameworkElement Column(this FrameworkElement e, double width)
	{
		e.HorizontalAlignment = HorizontalAlignment.Left;
		return new Grid
		{
			Width = Math.Max(0.0, width),
			HorizontalAlignment = HorizontalAlignment.Left,
			Children = { (UIElement)e }
		};
	}
}
