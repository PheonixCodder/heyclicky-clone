using System;
using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkGrow : Panel, IInkBaseline
{
	private readonly UIElement content;

	public double Fraction { get; }

	public double? FirstBaseline => InkBaseline.First(content);

	public ClickyInkGrow(UIElement content, double fraction)
	{
		this.content = content;
		Fraction = fraction;
		base.Children.Add(content);
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		content.Measure(new Size(availableSize.Width, double.PositiveInfinity));
		Size desiredSize = content.DesiredSize;
		return new Size(desiredSize.Width, desiredSize.Height * Math.Max(0.0, Math.Min(1.0, Fraction)));
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		content.Arrange(new Rect(0.0, 0.0, finalSize.Width, content.DesiredSize.Height));
		return finalSize;
	}

	public static FrameworkElement Around(UIElement view, double clock, double landsAt, double? vanishesAt)
	{
		double val = ClickyInkClock.EaseInOut(ClickyInkClock.Progress(clock, landsAt, landsAt + 0.4));
		double num;
		if (vanishesAt.HasValue)
		{
			double valueOrDefault = vanishesAt.GetValueOrDefault();
			num = ClickyInkClock.Progress(clock, valueOrDefault, valueOrDefault + 0.45);
		}
		else
		{
			num = 0.0;
		}
		double num2 = num;
		double val2 = 1.0 - ClickyInkClock.EaseInOut(num2);
		double num3 = 1.0 - ClickyInkClock.EaseInOut(Math.Min(1.0, num2 * 1.5));
		view.Opacity *= num3;
		return new ClickyInkGrow(view, Math.Min(val, val2));
	}
}
