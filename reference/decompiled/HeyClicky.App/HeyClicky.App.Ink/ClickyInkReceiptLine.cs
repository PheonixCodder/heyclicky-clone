using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkReceiptLine : Panel, IInkBaseline
{
	private const double Spacing = 4.0;

	private const double MinimumLeader = 6.0;

	private readonly InkTextView label;

	private readonly InkTextView? amount;

	private readonly double? amountMaximumWidth;

	private readonly ClickyShapeView leader;

	private double baseline;

	public double? FirstBaseline => baseline;

	public ClickyInkReceiptLine(InkTextView label, InkTextView? amount, double? amountMaximumWidth)
	{
		this.label = label;
		this.amount = amount;
		this.amountMaximumWidth = amountMaximumWidth;
		leader = ClickyShapeView.Stroke(new ClickyInkLeader(), new PaperStroke(PaperColor.Hex("#2A2A2A").Opacity(0.4), 1.4, PenLineCap.Round, PenLineJoin.Round, new double[2] { 0.1, 4.5 }));
		base.Children.Add(label);
		base.Children.Add(leader);
		if ((object)amount != null)
		{
			base.Children.Add(amount);
		}
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		double num = (double.IsInfinity(availableSize.Width) ? 1000.0 : availableSize.Width);
		double num2 = 0.0;
		double num3 = 4.0;
		if ((object)amount != null)
		{
			amount.Measure(new Size(amountMaximumWidth ?? double.PositiveInfinity, double.PositiveInfinity));
			num2 = amount.DesiredSize.Width;
			num3 += 4.0;
		}
		label.Measure(new Size(Math.Max(0.0, num - num2 - num3 - 6.0), double.PositiveInfinity));
		leader.Measure(new Size(0f, 2f));
		double num4 = InkBaseline.First(label) ?? label.DesiredSize.Height;
		double num5 = (((object)amount == null) ? 0.0 : (InkBaseline.First(amount) ?? amount.DesiredSize.Height));
		baseline = Math.Max(Math.Max(num4, num5), 2.0);
		double num6 = Math.Max(label.DesiredSize.Height - num4, ((object)amount == null) ? 0.0 : (amount.DesiredSize.Height - num5));
		return new Size(num, baseline + num6);
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		double num = InkBaseline.First(label) ?? label.DesiredSize.Height;
		label.Arrange(new Rect(0.0, baseline - num, label.DesiredSize.Width, label.DesiredSize.Height));
		double num2 = finalSize.Width;
		if ((object)amount != null)
		{
			double num3 = InkBaseline.First(amount) ?? amount.DesiredSize.Height;
			num2 -= amount.DesiredSize.Width;
			amount.Arrange(new Rect(num2, baseline - num3, amount.DesiredSize.Width, amount.DesiredSize.Height));
			num2 -= 4.0;
		}
		double num4 = label.DesiredSize.Width + 4.0;
		leader.Arrange(new Rect(num4, baseline - 2.0, Math.Max(6.0, num2 - num4), 2.0));
		return finalSize;
	}
}
