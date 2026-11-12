using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkReviewCompactLine : Panel, IInkBaseline
{
	private const double Spacing = 8.0;

	private const double MinimumReasonWidth = 110.0;

	private readonly FrameworkElement head;

	private readonly FrameworkElement? reason;

	private Rect headFrame;

	private Rect reasonFrame;

	private double headBaseline;

	public double? FirstBaseline => headBaseline;

	public ClickyInkReviewCompactLine(FrameworkElement head, FrameworkElement? reason)
	{
		this.head = head;
		this.reason = reason;
		base.Children.Add(head);
		if ((object)reason != null)
		{
			base.Children.Add(reason);
		}
	}

	private static double Baseline(UIElement e)
	{
		return InkBaseline.First(e) ?? e.DesiredSize.Height;
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		head.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
		Size desiredSize = head.DesiredSize;
		Size? size = null;
		if ((object)reason != null)
		{
			reason.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			size = reason.DesiredSize;
		}
		double num = (double.IsInfinity(availableSize.Width) ? (desiredSize.Width + ((!size.HasValue) ? 0.0 : (8.0 + size.GetValueOrDefault().Width))) : availableSize.Width);
		double num2 = Math.Min(desiredSize.Width, num);
		head.Measure(new Size(num2, double.PositiveInfinity));
		Size size2 = new Size(num2, head.DesiredSize.Height);
		double num3 = Baseline(head);
		if ((object)reason != null && size.HasValue)
		{
			Size valueOrDefault = size.GetValueOrDefault();
			double num4 = num - num2 - 8.0;
			if (num4 >= Math.Min(valueOrDefault.Width, 110.0))
			{
				reason.Measure(new Size(Math.Min(valueOrDefault.Width, num4), double.PositiveInfinity));
				Size size3 = new Size(Math.Min(reason.DesiredSize.Width, num4), reason.DesiredSize.Height);
				double num5 = num3 - Baseline(reason);
				double num6 = Math.Min(0.0, num5);
				reasonFrame = new Rect(num2 + 8.0, num5 - num6, size3.Width, size3.Height);
				double height = Math.Max(size2.Height - num6, reasonFrame.Bottom);
				headFrame = new Rect(0.0, 0.0 - num6, size2.Width, size2.Height);
				headBaseline = num3 - num6;
				return new Size(num, height);
			}
			reason.Measure(new Size(num, double.PositiveInfinity));
			reasonFrame = new Rect(0.0, size2.Height + 1.0, Math.Min(reason.DesiredSize.Width, num), reason.DesiredSize.Height);
			headFrame = new Rect(0.0, 0.0, size2.Width, size2.Height);
			headBaseline = num3;
			return new Size(num, reasonFrame.Bottom);
		}
		headFrame = new Rect(0.0, 0.0, size2.Width, size2.Height);
		headBaseline = num3;
		return new Size(num, size2.Height);
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		head.Arrange(headFrame);
		reason?.Arrange(reasonFrame);
		return finalSize;
	}
}
