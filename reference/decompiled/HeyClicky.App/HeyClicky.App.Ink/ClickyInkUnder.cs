using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkUnder : Panel
{
	private readonly UIElement top;

	private readonly UIElement under;

	private readonly double drop;

	public ClickyInkUnder(UIElement top, UIElement under, double drop)
	{
		this.top = top;
		this.under = under;
		this.drop = drop;
		base.Children.Add(top);
		base.Children.Add(under);
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		top.Measure(availableSize);
		under.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
		return new Size(top.DesiredSize.Width, top.DesiredSize.Height + drop);
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		Size desiredSize = top.DesiredSize;
		top.Arrange(new Rect(0.0, 0.0, desiredSize.Width, desiredSize.Height));
		Size desiredSize2 = under.DesiredSize;
		under.Arrange(new Rect((desiredSize.Width - desiredSize2.Width) / 2.0, desiredSize.Height - desiredSize2.Height + drop, desiredSize2.Width, desiredSize2.Height));
		return finalSize;
	}
}
