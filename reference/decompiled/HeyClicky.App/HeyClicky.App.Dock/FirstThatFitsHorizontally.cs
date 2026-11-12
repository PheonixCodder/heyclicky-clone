using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace HeyClicky.App.Dock;

internal sealed class FirstThatFitsHorizontally : Panel
{
	private int chosen = -1;

	private readonly FitDecision decision = new FitDecision();

	protected override Size MeasureOverride(Size available)
	{
		int num = decision.FirstCandidate(available, base.Children, chosen);
		chosen = -1;
		for (int i = 0; i < base.Children.Count; i++)
		{
			bool flag = i == base.Children.Count - 1;
			base.Children[i].Measure(flag ? available : new Size(double.PositiveInfinity, available.Height));
			if (chosen < 0 && i >= num && (flag || base.Children[i].DesiredSize.Width <= available.Width + 0.5))
			{
				chosen = i;
			}
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
		return base.Children[chosen].DesiredSize;
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
