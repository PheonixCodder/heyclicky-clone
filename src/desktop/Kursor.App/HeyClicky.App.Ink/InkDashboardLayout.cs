using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

internal static class InkDashboardLayout
{
	public static FrameworkElement Fixed(UIElement content)
	{
		return new Grid
		{
			Children = { content }
		};
	}

	public static StackPanel VStack(double spacing, params UIElement?[] children)
	{
		StackPanel stackPanel = new StackPanel
		{
			Spacing = spacing,
			HorizontalAlignment = HorizontalAlignment.Left
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

	public static ClickyInkHStack HStack(double spacing, InkVerticalAlignment alignment, params UIElement?[] children)
	{
		ClickyInkHStack clickyInkHStack = new ClickyInkHStack(spacing, alignment);
		foreach (UIElement uIElement in children)
		{
			if ((object)uIElement != null)
			{
				clickyInkHStack.Children.Add(uIElement);
			}
		}
		return clickyInkHStack;
	}

	public static T Width<T>(T element, double width) where T : FrameworkElement
	{
		element.Width = Math.Max(0.0, width);
		element.HorizontalAlignment = HorizontalAlignment.Left;
		return element;
	}

	public static PaperPath RotatedRoundedRect(Rect rect, double radius, double degrees)
	{
		Point center = new Point(rect.X + rect.Width / 2.0, rect.Y + rect.Height / 2.0);
		double num = degrees * Math.PI / 180.0;
		double cos = Math.Cos(num);
		double sin = Math.Sin(num);
		double num2 = Math.Max(0.0, Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2.0));
		List<Point> list = new List<Point>();
		(double, double, double)[] array = new(double, double, double)[4]
		{
			(rect.X + num2, rect.Y + num2, Math.PI),
			(rect.X + rect.Width - num2, rect.Y + num2, 4.71238898038469),
			(rect.X + rect.Width - num2, rect.Y + rect.Height - num2, 0.0),
			(rect.X + num2, rect.Y + rect.Height - num2, Math.PI / 2.0)
		};
		for (int i = 0; i < array.Length; i++)
		{
			(double, double, double) tuple = array[i];
			double item = tuple.Item1;
			double item2 = tuple.Item2;
			double item3 = tuple.Item3;
			for (int j = 0; j <= 6; j++)
			{
				double num3 = item3 + (double)j / 6.0 * (Math.PI / 2.0);
				list.Add(Turn(item + num2 * Math.Cos(num3), item2 + num2 * Math.Sin(num3)));
			}
		}
		return new PaperPath().AddLines(list).Close();
		Point Turn(double x, double y)
		{
			double num4 = x - center.X;
			double num5 = y - center.Y;
			return new Point(center.X + num4 * cos - num5 * sin, center.Y + num4 * sin + num5 * cos);
		}
	}
}
