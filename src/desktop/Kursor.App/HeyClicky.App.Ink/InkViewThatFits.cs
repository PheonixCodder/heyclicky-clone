using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class InkViewThatFits : Panel
{
	private UIElement? shown;

	public InkViewThatFits(params UIElement[] options)
	{
		foreach (UIElement item in options)
		{
			base.Children.Add(item);
		}
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		shown = null;
		foreach (UIElement child in base.Children)
		{
			child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
			if (child.DesiredSize.Width <= availableSize.Width + 0.5)
			{
				shown = child;
				break;
			}
		}
		if ((object)shown == null)
		{
			object obj;
			if (base.Children.Count <= 0)
			{
				obj = null;
			}
			else
			{
				UIElementCollection children = base.Children;
				obj = children[children.Count - 1];
			}
			shown = (UIElement?)obj;
		}
		foreach (UIElement child2 in base.Children)
		{
			child2.Opacity = ((child2 == shown) ? 1 : 0);
			child2.IsHitTestVisible = child2 == shown;
		}
		if ((object)shown == null)
		{
			return new Size(0f, 0f);
		}
		shown.Measure(availableSize);
		return shown.DesiredSize;
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		foreach (UIElement child in base.Children)
		{
			child.Arrange((child == shown) ? new Rect(0.0, 0.0, finalSize.Width, finalSize.Height) : new Rect(0f, 0f, 0f, 0f));
		}
		return finalSize;
	}
}
