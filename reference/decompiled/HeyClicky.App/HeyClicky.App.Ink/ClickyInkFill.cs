using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkFill : Panel
{
	private readonly UIElement content;

	private readonly double height;

	public ClickyInkFill(UIElement content, double height)
	{
		this.content = content;
		this.height = height;
		base.Children.Add(content);
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		double width = (double.IsInfinity(availableSize.Width) ? 0.0 : availableSize.Width);
		content.Measure(new Size(width, height));
		return new Size(width, height);
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		content.Arrange(new Rect(0.0, 0.0, finalSize.Width, height));
		return finalSize;
	}
}
