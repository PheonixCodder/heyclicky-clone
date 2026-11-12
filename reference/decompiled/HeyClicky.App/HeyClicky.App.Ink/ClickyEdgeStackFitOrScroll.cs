using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyEdgeStackFitOrScroll : Grid
{
	private readonly ScrollViewer scroller;

	public double MaximumHeight { get; }

	public ClickyEdgeStackFitOrScroll(double maximumHeight, FrameworkElement content)
	{
		MaximumHeight = maximumHeight;
		scroller = new ScrollViewer
		{
			Content = content,
			VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			VerticalScrollMode = ScrollMode.Auto
		};
		base.Children.Add(scroller);
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		FrameworkElement obj = (FrameworkElement)scroller.Content;
		obj.Measure(new Size(availableSize.Width, double.PositiveInfinity));
		double height = obj.DesiredSize.Height;
		scroller.Measure(new Size(availableSize.Width, Math.Min(height, MaximumHeight)));
		return new Size(obj.DesiredSize.Width, Math.Min(height, MaximumHeight));
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		scroller.Arrange(new Rect(0.0, 0.0, finalSize.Width, finalSize.Height));
		return finalSize;
	}
}
