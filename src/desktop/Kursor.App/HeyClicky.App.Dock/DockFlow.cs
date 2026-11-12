using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace HeyClicky.App.Dock;

internal sealed class DockFlow : Panel
{
	public double HorizontalSpacing { get; set; } = 6.0;

	public double VerticalSpacing { get; set; } = 4.0;

	protected override Size MeasureOverride(Size available)
	{
		double num = (double.IsInfinity(available.Width) ? double.MaxValue : available.Width);
		double num2 = 0.0;
		double num3 = 0.0;
		double num4 = 0.0;
		double num5 = 0.0;
		foreach (UIElement child in base.Children)
		{
			child.Measure(new Size(num, double.PositiveInfinity));
			Size desiredSize = child.DesiredSize;
			if (num2 > 0.0 && num2 + desiredSize.Width > num)
			{
				num3 += num4 + VerticalSpacing;
				num2 = 0.0;
				num4 = 0.0;
			}
			num2 += desiredSize.Width + HorizontalSpacing;
			num4 = Math.Max(num4, desiredSize.Height);
			num5 = Math.Max(num5, num2 - HorizontalSpacing);
		}
		return new Size(num5, (base.Children.Count == 0) ? 0.0 : (num3 + num4));
	}

	protected override Size ArrangeOverride(Size final)
	{
		double num = 0.0;
		double num2 = 0.0;
		double num3 = 0.0;
		foreach (UIElement child in base.Children)
		{
			Size desiredSize = child.DesiredSize;
			if (num > 0.0 && num + desiredSize.Width > final.Width)
			{
				num2 += num3 + VerticalSpacing;
				num = 0.0;
				num3 = 0.0;
			}
			child.Arrange(new Rect(num, num2, desiredSize.Width, desiredSize.Height));
			num += desiredSize.Width + HorizontalSpacing;
			num3 = Math.Max(num3, desiredSize.Height);
		}
		return final;
	}
}
