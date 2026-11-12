using System.Threading.Tasks;
using HeyClicky.App.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public static class ClickyInkPolaroidPicture
{
	public static FrameworkElement Make(string? source, string? query, double develop, double width, double height)
	{
		Grid frame = new Grid
		{
			Width = width,
			Height = height,
			Background = Ui.Brush(PaperColor.Hex("#2B2D33")),
			Clip = new RectangleGeometry
			{
				Rect = new Rect(0.0, 0.0, width, height)
			}
		};
		ClickyInkPicture clickyInkPicture = ClickyInkImages.Image(source);
		if ((object)clickyInkPicture != null)
		{
			Show(clickyInkPicture);
			return frame;
		}
		TextBlock textBlock = ClickyInkIcon.Glyph("photo", 20.0, PaperColor.White.Opacity(0.35));
		if ((object)textBlock != null)
		{
			frame.Children.Add(textBlock);
		}
		if (source != null)
		{
			Task<ClickyInkPicture?> task = ClickyInkImages.LoadAsync(source);
			DispatcherQueue dispatcher = DispatcherQueue.GetForCurrentThread();
			task.ContinueWith(delegate(Task<ClickyInkPicture?> t)
			{
				if (t.IsCompletedSuccessfully)
				{
					ClickyInkPicture found = t.Result;
					if ((object)found != null)
					{
						dispatcher.TryEnqueue(delegate
						{
							Show(found);
						});
					}
				}
			}, TaskScheduler.Default);
		}
		return frame;
		void Show(ClickyInkPicture picture)
		{
			frame.Children.Clear();
			Grid grid = new Grid
			{
				Opacity = 0.25 + 0.75 * develop
			};
			grid.Children.Add(new Image
			{
				Source = picture.Gray,
				Stretch = Stretch.UniformToFill,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			});
			grid.Children.Add(new Image
			{
				Source = picture.Color,
				Stretch = Stretch.UniformToFill,
				Opacity = develop,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			});
			frame.Children.Add(grid);
		}
	}
}
