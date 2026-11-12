using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace HeyClicky.App.Settings;

public sealed class WrapPanel : Panel
{
	public double Spacing { get; set; } = 6.0;

	protected override Size MeasureOverride(Size available)
	{
		double num = 0.0;
		double num2 = 0.0;
		double num3 = 0.0;
		double num4 = 0.0;
		foreach (UIElement child in base.Children)
		{
			child.Measure(new Size(available.Width, double.PositiveInfinity));
			Size desiredSize = child.DesiredSize;
			if (num > 0.0 && num + desiredSize.Width > available.Width)
			{
				num2 += num3 + Spacing;
				num = 0.0;
				num3 = 0.0;
			}
			num += desiredSize.Width + Spacing;
			num3 = Math.Max(num3, desiredSize.Height);
			num4 = Math.Max(num4, num - Spacing);
		}
		return new Size(double.IsInfinity(available.Width) ? num4 : Math.Min(num4, available.Width), num2 + num3);
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
				num2 += num3 + Spacing;
				num = 0.0;
				num3 = 0.0;
			}
			child.Arrange(new Rect(num, num2, desiredSize.Width, desiredSize.Height));
			num += desiredSize.Width + Spacing;
			num3 = Math.Max(num3, desiredSize.Height);
		}
		return final;
	}
}
