using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyPaperGrain : Grid
{
	public ClickyPaperGrain(double opacity = 0.45)
	{
		base.IsHitTestVisible = false;
		CanvasControl canvasControl = new CanvasControl
		{
			IsHitTestVisible = false
		};
		canvasControl.Draw += delegate(CanvasControl s, CanvasDrawEventArgs e)
		{
			ClickyPaperTextures.DrawGrain(e.DrawingSession, new Rect(0.0, 0.0, s.ActualWidth, s.ActualHeight), opacity);
		};
		base.Children.Add(canvasControl);
	}
}
