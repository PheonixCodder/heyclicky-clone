using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkOverlay : Panel, IInkBaseline
{
	private readonly UIElement content;

	public double? FirstBaseline => InkBaseline.First(content);

	public ClickyInkOverlay(UIElement content, params UIElement[] overlays)
	{
		this.content = content;
		base.Children.Add(content);
		foreach (UIElement item in overlays)
		{
			base.Children.Add(item);
		}
	}

	public ClickyInkOverlay Behind(UIElement background)
	{
		base.Children.Insert(0, background);
		return this;
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		content.Measure(availableSize);
		Size desiredSize = content.DesiredSize;
		foreach (UIElement child in base.Children)
		{
			if (child != content)
			{
				child.Measure(desiredSize);
			}
		}
		return desiredSize;
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		foreach (UIElement child in base.Children)
		{
			if (child == content)
			{
				child.Arrange(new Rect(0.0, 0.0, finalSize.Width, finalSize.Height));
				continue;
			}
			FrameworkElement frameworkElement = child as FrameworkElement;
			double num = (((object)frameworkElement != null && !double.IsNaN(frameworkElement.Width)) ? (frameworkElement.Width + frameworkElement.Margin.Left + frameworkElement.Margin.Right) : finalSize.Width);
			double num2 = (((object)frameworkElement != null && !double.IsNaN(frameworkElement.Height)) ? (frameworkElement.Height + frameworkElement.Margin.Top + frameworkElement.Margin.Bottom) : finalSize.Height);
			if ((object)frameworkElement != null && double.IsNaN(frameworkElement.Width) && frameworkElement.HorizontalAlignment != HorizontalAlignment.Stretch)
			{
				num = Math.Min(child.DesiredSize.Width, finalSize.Width);
			}
			if ((object)frameworkElement != null && double.IsNaN(frameworkElement.Height) && frameworkElement.VerticalAlignment != VerticalAlignment.Stretch)
			{
				num2 = Math.Min(child.DesiredSize.Height, finalSize.Height);
			}
			child.Arrange(new Rect(frameworkElement?.HorizontalAlignment switch
			{
				HorizontalAlignment.Right => finalSize.Width - num, 
				HorizontalAlignment.Center => (finalSize.Width - num) / 2.0, 
				_ => 0.0, 
			}, frameworkElement?.VerticalAlignment switch
			{
				VerticalAlignment.Bottom => finalSize.Height - num2, 
				VerticalAlignment.Center => (finalSize.Height - num2) / 2.0, 
				_ => 0.0, 
			}, num, num2));
		}
		return finalSize;
	}
}
