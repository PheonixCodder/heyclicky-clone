using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkSpacer : FrameworkElement
{
	protected override Size MeasureOverride(Size availableSize)
	{
		return new Size(0f, 0f);
	}
}
