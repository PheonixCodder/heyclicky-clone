using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkCentered : Panel, IInkBaseline
{
	private readonly UIElement content;

	private double Top => (base.Height - content.DesiredSize.Height) / 2.0;

	public double? FirstBaseline
	{
		get
		{
			double? num = InkBaseline.First(content);
			if (num.HasValue)
			{
				double valueOrDefault = num.GetValueOrDefault();
				return Top + valueOrDefault;
			}
			return null;
		}
	}

	public ClickyInkCentered(UIElement content, double width, double height)
	{
		this.content = content;
		base.Width = width;
		base.Height = height;
		base.Children.Add(content);
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		content.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
		return new Size(base.Width, base.Height);
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		Size desiredSize = content.DesiredSize;
		content.Arrange(new Rect((finalSize.Width - desiredSize.Width) / 2.0, (finalSize.Height - desiredSize.Height) / 2.0, desiredSize.Width, desiredSize.Height));
		return finalSize;
	}
}
