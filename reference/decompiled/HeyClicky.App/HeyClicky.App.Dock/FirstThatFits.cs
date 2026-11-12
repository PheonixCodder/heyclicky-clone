using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace HeyClicky.App.Dock;

public sealed class FirstThatFits : Panel
{
	private int chosen = -1;

	private readonly FitDecision decision = new FitDecision();

	protected override Size MeasureOverride(Size available)
	{
		int num = decision.FirstCandidate(available, base.Children, chosen);
		chosen = -1;
		for (int i = 0; i < base.Children.Count; i++)
		{
			UIElement uIElement = base.Children[i];
			uIElement.Measure(new Size(available.Width, double.PositiveInfinity));
			if (chosen < 0 && i >= num && uIElement.DesiredSize.Height <= available.Height + 0.5)
			{
				chosen = i;
			}
		}
		if (chosen < 0)
		{
			chosen = base.Children.Count - 1;
		}
		decision.Remember(available, base.Children, chosen);
		for (int j = 0; j < base.Children.Count; j++)
		{
			double num2 = ((j == chosen) ? 1 : 0);
			if (base.Children[j].Opacity != num2)
			{
				base.Children[j].Opacity = num2;
			}
			if (base.Children[j].IsHitTestVisible != (j == chosen))
			{
				base.Children[j].IsHitTestVisible = j == chosen;
			}
		}
		if (chosen < 0)
		{
			return new Size(0f, 0f);
		}
		base.Children[chosen].Measure(available);
		Size desiredSize = base.Children[chosen].DesiredSize;
		return new Size(double.IsInfinity(available.Width) ? desiredSize.Width : available.Width, Math.Min(desiredSize.Height, available.Height));
	}

	protected override Size ArrangeOverride(Size final)
	{
		for (int i = 0; i < base.Children.Count; i++)
		{
			base.Children[i].Arrange((i == chosen) ? new Rect(0.0, 0.0, final.Width, final.Height) : new Rect(0f, 0f, 0f, 0f));
		}
		return final;
	}
}
