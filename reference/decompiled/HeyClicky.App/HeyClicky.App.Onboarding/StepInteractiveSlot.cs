using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace HeyClicky.App.Onboarding;

public sealed class StepInteractiveSlot : Panel
{
	public StepInteractiveSlot(FrameworkElement content)
	{
		base.Children.Add(content);
	}

	protected override Size MeasureOverride(Size available)
	{
		double val = 0.0;
		double num = 0.0;
		foreach (UIElement child in base.Children)
		{
			child.Measure(new Size(available.Width, double.PositiveInfinity));
			val = Math.Max(val, child.DesiredSize.Width);
			num = Math.Max(num, child.DesiredSize.Height);
		}
		return new Size(Math.Min(val, available.Width), double.IsInfinity(available.Height) ? num : Math.Min(num, available.Height));
	}

	protected override Size ArrangeOverride(Size final)
	{
		foreach (UIElement child in base.Children)
		{
			Size desiredSize = child.DesiredSize;
			double num = Math.Max(desiredSize.Width, final.Width);
			child.Arrange(new Rect((final.Width - num) / 2.0, (final.Height - desiredSize.Height) / 2.0, num, desiredSize.Height));
		}
		return final;
	}
}
