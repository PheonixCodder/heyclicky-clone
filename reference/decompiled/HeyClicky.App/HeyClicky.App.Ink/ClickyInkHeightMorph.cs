using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkHeightMorph : Panel
{
	private readonly double progress;

	public ClickyInkHeightMorph(double progress, UIElement now, UIElement before)
	{
		this.progress = progress;
		base.Children.Add(now);
		base.Children.Add(before);
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		Size availableSize2 = new Size(availableSize.Width, double.PositiveInfinity);
		base.Children[0].Measure(availableSize2);
		Size desiredSize = base.Children[0].DesiredSize;
		if (base.Children.Count < 2)
		{
			return desiredSize;
		}
		base.Children[1].Measure(availableSize2);
		double height = base.Children[1].DesiredSize.Height;
		return new Size(desiredSize.Width, height + (desiredSize.Height - height) * ClickyInkClock.EaseInOut(progress));
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		foreach (UIElement child in base.Children)
		{
			child.Arrange(new Rect(0.0, 0.0, finalSize.Width, child.DesiredSize.Height));
		}
		return finalSize;
	}
}
