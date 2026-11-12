using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public static class ClickyInkBottomClip
{
	public static void Apply(FrameworkElement element)
	{
		element.SizeChanged += delegate
		{
			Update();
		};
		void Update()
		{
			element.Clip = new RectangleGeometry
			{
				Rect = new Rect(-60.0, -60.0, element.ActualWidth + 120.0, element.ActualHeight + 60.0)
			};
		}
	}
}
