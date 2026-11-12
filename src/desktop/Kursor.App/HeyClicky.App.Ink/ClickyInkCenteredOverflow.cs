using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkCenteredOverflow : Panel
{
	private readonly UIElement content;

	private readonly double width;

	public ClickyInkCenteredOverflow(UIElement content, double width)
	{
		this.content = content;
		this.width = width;
		base.Children.Add(content);
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		content.Measure(new Size(double.PositiveInfinity, availableSize.Height));
		return new Size(width, content.DesiredSize.Height);
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		Size desiredSize = content.DesiredSize;
		content.Arrange(new Rect((finalSize.Width - desiredSize.Width) / 2.0, (finalSize.Height - desiredSize.Height) / 2.0, desiredSize.Width, desiredSize.Height));
		return finalSize;
	}
}
