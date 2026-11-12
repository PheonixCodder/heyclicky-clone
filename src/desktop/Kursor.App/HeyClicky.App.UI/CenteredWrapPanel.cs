using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace HeyClicky.App.UI;

public sealed class CenteredWrapPanel : Panel
{
	public double HorizontalSpacing { get; set; } = 8.0;

	public double VerticalSpacing { get; set; } = 8.0;

	private List<List<UIElement>> Rows(double width)
	{
		List<List<UIElement>> list = new List<List<UIElement>>
		{
			new List<UIElement>()
		};
		double num = 0.0;
		foreach (UIElement child in base.Children)
		{
			double width2 = child.DesiredSize.Width;
			if (list[list.Count - 1].Count > 0 && num + HorizontalSpacing + width2 > width)
			{
				list.Add(new List<UIElement>());
				num = 0.0;
			}
			num += ((list[list.Count - 1].Count > 0) ? HorizontalSpacing : 0.0) + width2;
			list[list.Count - 1].Add(child);
		}
		return list;
	}

	protected override Size MeasureOverride(Size available)
	{
		foreach (UIElement child in base.Children)
		{
			child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
		}
		double width = (double.IsInfinity(available.Width) ? 392.0 : available.Width);
		List<List<UIElement>> list = Rows(width);
		double height = list.Sum((List<UIElement> r) => (r.Count != 0) ? r.Max((UIElement c) => c.DesiredSize.Height) : 0.0) + VerticalSpacing * (double)Math.Max(0, list.Count - 1);
		return new Size(width, height);
	}

	protected override Size ArrangeOverride(Size final)
	{
		double num = 0.0;
		foreach (List<UIElement> item in Rows(final.Width))
		{
			if (item.Count == 0)
			{
				continue;
			}
			double num2 = item.Sum((UIElement c) => c.DesiredSize.Width) + HorizontalSpacing * (double)(item.Count - 1);
			double num3 = item.Max((UIElement c) => c.DesiredSize.Height);
			double num4 = (final.Width - num2) / 2.0;
			foreach (UIElement item2 in item)
			{
				item2.Arrange(new Rect(num4, num + (num3 - item2.DesiredSize.Height) / 2.0, item2.DesiredSize.Width, item2.DesiredSize.Height));
				num4 += item2.DesiredSize.Width + HorizontalSpacing;
			}
			num += num3 + VerticalSpacing;
		}
		return final;
	}
}
