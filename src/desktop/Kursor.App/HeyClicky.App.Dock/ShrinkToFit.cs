using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace HeyClicky.App.Dock;

internal sealed class ShrinkToFit : Panel
{
	private readonly FrameworkElement child;

	private readonly double minimumScale;

	private readonly TextBlock? unlimitedProbe;

	private readonly TextBlock? oneLineProbe;

	private readonly int maxLines;

	private readonly ScaleTransform scale = new ScaleTransform();

	private double appliedScale = 1.0;

	public ShrinkToFit(FrameworkElement child, double minimumScale, int maxLines = 0)
	{
		this.child = child;
		this.minimumScale = minimumScale;
		this.maxLines = maxLines;
		child.RenderTransform = scale;
		base.Children.Add(child);
		TextBlock text = child as TextBlock;
		if ((object)text != null && maxLines > 0)
		{
			unlimitedProbe = Probe(0);
			oneLineProbe = Probe(1);
			base.Children.Add(unlimitedProbe);
			base.Children.Add(oneLineProbe);
		}
		TextBlock Probe(int lines)
		{
			return new TextBlock
			{
				Text = text.Text,
				FontSize = text.FontSize,
				FontWeight = text.FontWeight,
				FontFamily = text.FontFamily,
				TextWrapping = TextWrapping.Wrap,
				MaxLines = lines,
				IsTextScaleFactorEnabled = text.IsTextScaleFactorEnabled,
				Opacity = 0.0,
				IsHitTestVisible = false
			};
		}
	}

	protected override Size MeasureOverride(Size available)
	{
		double num = double.PositiveInfinity;
		if ((object)oneLineProbe != null)
		{
			oneLineProbe.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			num = oneLineProbe.DesiredSize.Height * (double)maxLines + 0.5;
		}
		double num2 = 1.0;
		while (true)
		{
			Size availableSize = new Size(double.IsInfinity(available.Width) ? double.PositiveInfinity : (available.Width / num2), double.PositiveInfinity);
			child.Measure(availableSize);
			double num3 = child.DesiredSize.Height;
			if ((object)unlimitedProbe != null)
			{
				unlimitedProbe.Measure(availableSize);
				if (unlimitedProbe.DesiredSize.Height > num)
				{
					num3 = double.PositiveInfinity;
				}
			}
			if (num3 * num2 <= available.Height + 0.5 || num2 <= minimumScale)
			{
				break;
			}
			num2 = Math.Max(minimumScale, num2 - 0.025);
		}
		appliedScale = num2;
		Size desiredSize = child.DesiredSize;
		return new Size(Math.Min(desiredSize.Width * num2, available.Width), Math.Min(desiredSize.Height * num2, available.Height));
	}

	protected override Size ArrangeOverride(Size final)
	{
		double num = appliedScale;
		ScaleTransform scaleTransform = scale;
		double scaleX = (scale.ScaleY = num);
		scaleTransform.ScaleX = scaleX;
		child.Arrange(new Rect(0.0, 0.0, final.Width / num, child.DesiredSize.Height));
		unlimitedProbe?.Arrange(new Rect(0f, 0f, 0f, 0f));
		oneLineProbe?.Arrange(new Rect(0f, 0f, 0f, 0f));
		return final;
	}
}
