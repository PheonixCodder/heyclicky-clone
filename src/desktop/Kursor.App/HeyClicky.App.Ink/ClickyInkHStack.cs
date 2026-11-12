using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkHStack : Panel, IInkBaseline
{
	private double? firstBaseline;

	public static readonly DependencyProperty FlexibleProperty = DependencyProperty.RegisterAttached("Flexible", typeof(bool), typeof(ClickyInkHStack), new PropertyMetadata(false));

	public double Spacing { get; set; }

	public InkVerticalAlignment Alignment { get; set; } = InkVerticalAlignment.Center;

	public double? FirstBaseline => firstBaseline;

	public static T Flexible<T>(T e) where T : UIElement
	{
		e.SetValue(FlexibleProperty, true);
		return e;
	}

	public ClickyInkHStack(double spacing = 8.0, InkVerticalAlignment alignment = InkVerticalAlignment.Center, params UIElement[] children)
	{
		Spacing = spacing;
		Alignment = alignment;
		foreach (UIElement item in children)
		{
			base.Children.Add(item);
		}
	}

	private static bool IsFlexible(UIElement e)
	{
		if (!(bool)e.GetValue(FlexibleProperty) && !(e is InkTextView) && !(e is ClickyHandText) && !(e is ClickyInkSpacer))
		{
			if (e is TextBlock textBlock)
			{
				return textBlock.TextWrapping != TextWrapping.NoWrap;
			}
			return false;
		}
		return true;
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		List<UIElement> list = base.Children.Where((UIElement c) => c.Visibility == Visibility.Visible).ToList();
		double num = Spacing * (double)Math.Max(0, list.Count - 1);
		foreach (UIElement item3 in list.Where((UIElement c) => !IsFlexible(c)))
		{
			item3.Measure(new Size(double.PositiveInfinity, availableSize.Height));
			num += item3.DesiredSize.Width;
		}
		double num2 = Math.Max(0.0, availableSize.Width - num);
		foreach (UIElement item4 in list.Where((UIElement c) => IsFlexible(c) && !(c is ClickyInkSpacer)))
		{
			item4.Measure(new Size(double.IsInfinity(availableSize.Width) ? double.PositiveInfinity : num2, availableSize.Height));
			num2 = Math.Max(0.0, num2 - item4.DesiredSize.Width);
			num += item4.DesiredSize.Width;
		}
		foreach (ClickyInkSpacer item5 in list.OfType<ClickyInkSpacer>())
		{
			item5.Measure(new Size(0f, 0f));
		}
		double width = ((list.Any((UIElement c) => c is ClickyInkSpacer) && !double.IsInfinity(availableSize.Width)) ? Math.Max(num, availableSize.Width) : num);
		Dictionary<UIElement, double> tops;
		(double Height, double? Baseline) tuple = VerticalLayout(list, out tops);
		double item = tuple.Height;
		double? item2 = tuple.Baseline;
		firstBaseline = item2;
		return new Size(width, item);
	}

	private (double Height, double? Baseline) VerticalLayout(List<UIElement> visible, out Dictionary<UIElement, double> tops)
	{
		tops = new Dictionary<UIElement, double>();
		if (Alignment == InkVerticalAlignment.FirstBaseline)
		{
			double num = 0.0;
			double num2 = 0.0;
			Dictionary<UIElement, double> dictionary = new Dictionary<UIElement, double>();
			foreach (UIElement item2 in visible)
			{
				double num4 = (dictionary[item2] = InkBaseline.First(item2) ?? item2.DesiredSize.Height);
				num = Math.Max(num, num4);
				num2 = Math.Max(num2, item2.DesiredSize.Height - num4);
			}
			foreach (UIElement item3 in visible)
			{
				tops[item3] = num - dictionary[item3];
			}
			return (Height: num + num2, Baseline: (visible.Count == 0) ? ((double?)null) : new double?(num));
		}
		double num5 = ((visible.Count == 0) ? 0.0 : visible.Max((UIElement c) => c.DesiredSize.Height));
		double? item = null;
		foreach (UIElement item4 in visible)
		{
			double num6 = Alignment switch
			{
				InkVerticalAlignment.Top => 0.0, 
				InkVerticalAlignment.Bottom => num5 - item4.DesiredSize.Height, 
				_ => (num5 - item4.DesiredSize.Height) / 2.0, 
			};
			tops[item4] = num6;
			if (!item.HasValue)
			{
				double? num7 = InkBaseline.First(item4);
				if (num7.HasValue)
				{
					double valueOrDefault = num7.GetValueOrDefault();
					item = num6 + valueOrDefault;
				}
			}
		}
		return (Height: num5, Baseline: item);
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		List<UIElement> list = base.Children.Where((UIElement c) => c.Visibility == Visibility.Visible).ToList();
		double num = list.Where((UIElement c) => !(c is ClickyInkSpacer)).Sum((UIElement c) => c.DesiredSize.Width) + Spacing * (double)Math.Max(0, list.Count - 1);
		int num2 = list.Count((UIElement c) => c is ClickyInkSpacer);
		double num3 = ((num2 == 0) ? 0.0 : (Math.Max(0.0, finalSize.Width - num) / (double)num2));
		VerticalLayout(list, out Dictionary<UIElement, double> tops);
		double num4 = 0.0;
		foreach (UIElement item in list)
		{
			double num5 = ((item is ClickyInkSpacer) ? num3 : item.DesiredSize.Width);
			item.Arrange(new Rect(num4, tops[item], num5, item.DesiredSize.Height));
			num4 += num5 + Spacing;
		}
		foreach (UIElement item2 in base.Children.Where((UIElement c) => c.Visibility != Visibility.Visible))
		{
			item2.Arrange(new Rect(0f, 0f, 0f, 0f));
		}
		return finalSize;
	}
}
