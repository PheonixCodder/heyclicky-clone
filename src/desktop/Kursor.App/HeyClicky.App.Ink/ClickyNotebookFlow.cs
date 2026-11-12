using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyNotebookFlow : Panel
{
	public double Spacing { get; set; } = 8.0;

	public double LineSpacing { get; set; } = 4.0;

	protected override Size MeasureOverride(Size availableSize)
	{
		double width = availableSize.Width;
		double num = 0.0;
		double num2 = 0.0;
		double num3 = 0.0;
		double val = 0.0;
		foreach (UIElement child in base.Children)
		{
			child.Measure(new Size(width, double.PositiveInfinity));
			Size desiredSize = child.DesiredSize;
			if (num > 0.0 && num + desiredSize.Width > width)
			{
				num2 += num3 + LineSpacing;
				num = 0.0;
				num3 = 0.0;
			}
			num += desiredSize.Width + Spacing;
			val = Math.Max(val, num - Spacing);
			num3 = Math.Max(num3, desiredSize.Height);
		}
		return new Size(Math.Min(val, width), num2 + num3);
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		double num = 0.0;
		double num2 = 0.0;
		double num3 = 0.0;
		foreach (UIElement child in base.Children)
		{
			Size desiredSize = child.DesiredSize;
			if (num > 0.0 && num + desiredSize.Width > finalSize.Width)
			{
				num2 += num3 + LineSpacing;
				num = 0.0;
				num3 = 0.0;
			}
			child.Arrange(new Rect(num, num2, desiredSize.Width, desiredSize.Height));
			num += desiredSize.Width + Spacing;
			num3 = Math.Max(num3, desiredSize.Height);
		}
		return finalSize;
	}
}
