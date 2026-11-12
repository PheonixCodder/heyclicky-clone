using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace HeyClicky.App.UI;

public sealed class ScaleDownText : Panel
{
	private readonly TextBlock text;

	private readonly double minimumScale;

	private readonly ScaleTransform scale = new ScaleTransform();

	private double appliedScale = 1.0;

	public ScaleDownText(TextBlock text, double minimumScale)
	{
		this.text = text;
		this.minimumScale = minimumScale;
		text.TextWrapping = TextWrapping.NoWrap;
		text.TextTrimming = TextTrimming.CharacterEllipsis;
		text.RenderTransform = scale;
		base.Children.Add(text);
	}

	protected override Size MeasureOverride(Size available)
	{
		text.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
		Size desiredSize = text.DesiredSize;
		double num = 1.0;
		if (!double.IsInfinity(available.Width) && desiredSize.Width > available.Width && desiredSize.Width > 0.0)
		{
			num = Math.Max(minimumScale, available.Width / desiredSize.Width);
		}
		appliedScale = num;
		if (num >= 1.0)
		{
			return desiredSize;
		}
		double num2 = Math.Min(desiredSize.Width, available.Width / num);
		text.Measure(new Size(num2, double.PositiveInfinity));
		return new Size(Math.Min(num2 * num, available.Width), desiredSize.Height * num);
	}

	protected override Size ArrangeOverride(Size final)
	{
		double num = appliedScale;
		ScaleTransform scaleTransform = scale;
		double scaleX = (scale.ScaleY = num);
		scaleTransform.ScaleX = scaleX;
		text.Arrange(new Rect(0.0, 0.0, final.Width / num, final.Height / num));
		return final;
	}
}
