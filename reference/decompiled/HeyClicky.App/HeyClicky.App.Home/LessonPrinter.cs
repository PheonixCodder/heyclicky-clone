using System;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using HeyClicky.App.Design;
using HeyClicky.App.Shell;
using HeyClicky.App.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;

namespace HeyClicky.App.Home;

public sealed class LessonPrinter
{
	private readonly OverlayWindow window;

	private readonly DispatcherQueue dq;

	public nint Hwnd => window.Hwnd;

	public LessonPrinter(bool excludeFromCapture)
	{
		window = new OverlayWindow("HeyClicky print", excludeFromCapture, clickThroughEverywhere: true);
		dq = DispatcherQueue.GetForCurrentThread();
	}

	public void Print(byte[]? poster, string title, DisplayInfo display, (int X, int Y)? notch)
	{
		double s = display.Scale;
		double vw = display.BoundsPx.Width / s;
		double vh = display.BoundsPx.Height / s;
		window.PlacePx((int)display.BoundsPx.X, (int)display.BoundsPx.Y, (int)display.BoundsPx.Width, (int)display.BoundsPx.Height);
		window.Root.Children.Clear();
		Image image = new Image
		{
			Width = 360.0,
			Height = 360.0 * vh / vw,
			Stretch = Stretch.UniformToFill
		};
		if (poster != null)
		{
			BitmapImage bitmapImage = new BitmapImage();
			InMemoryRandomAccessStream ms = new InMemoryRandomAccessStream();
			LoadAsync(bitmapImage, ms, poster);
			image.Source = bitmapImage;
		}
		TextBlock textBlock = Ui.Text(title, 15.0, FontWeights.SemiBold, DS.Black(0.82), TextAlignment.Center, 2, wrap: true);
		textBlock.FontFamily = new FontFamily("Segoe Print, Segoe UI Variable Text");
		Border item = new Border
		{
			Background = Ui.Brush(DS.Hex(16513782u)),
			Padding = new Thickness(14.0, 14.0, 14.0, 10.0),
			CornerRadius = new CornerRadius(4.0),
			Child = new StackPanel
			{
				Spacing = 10.0,
				Children = 
				{
					(UIElement)new Border
					{
						Child = image,
						CornerRadius = new CornerRadius(2.0)
					},
					(UIElement)textBlock
				}
			},
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid host = new Grid
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Children = 
			{
				(UIElement)new ShadowLayer(4.0, DS.Black(0.35), 18.0, 10.0),
				(UIElement)item
			}
		};
		window.Root.Children.Add(host);
		window.ShowNoActivate();
		Sounds.Play("clicky-text-send", 0.4);
		After(0.06, delegate
		{
			host.UpdateLayout();
			Vector2 actualSize = host.ActualSize;
			host.CenterPoint = new Vector3(actualSize.X / 2f, actualSize.Y / 2f, 0f);
			host.Opacity = 0.0;
			host.Scale = new Vector3(0.85f, 0.85f, 1f);
			host.Translation = new Vector3(0f, 60f, 0f);
			host.Rotation = -4f;
			Anim.Opacity(host, 1f, 0.25);
			Anim.Scale(host, 1f, 0.45, null, host.CenterPoint);
			Anim.Translate(host, Vector3.Zero, 0.45);
			Anim.Rotate(host, 2f, 0.45);
			After(1.4, delegate
			{
				double num = 0.0 - vh / 2.0 + 8.0;
				double num2 = ((!notch.HasValue) ? 0.0 : (((double)notch.GetValueOrDefault().X - display.BoundsPx.X) / s - vw / 2.0));
				Anim.Translate(host, new Vector3((float)num2, (float)num, 0f), 0.55);
				Anim.Scale(host, 0.06f, 0.55, null, host.CenterPoint);
				Anim.Opacity(host, 0f, 0.55, null, 0.25);
				After(0.75, delegate
				{
					window.HideWindow();
					window.Root.Children.Clear();
				});
			});
		});
		Log.Info("lessons", "printing \"" + title + "\"");
	}

	private static async Task LoadAsync(BitmapImage bmp, InMemoryRandomAccessStream ms, byte[] png)
	{
		await ms.WriteAsync(png.AsBuffer());
		ms.Seek(0uL);
		await bmp.SetSourceAsync(ms);
	}

	private void After(double seconds, Action a)
	{
		DispatcherQueueTimer dispatcherQueueTimer = dq.CreateTimer().Rooted();
		dispatcherQueueTimer.Interval = TimeSpan.FromSeconds(seconds);
		dispatcherQueueTimer.IsRepeating = false;
		dispatcherQueueTimer.Tick += delegate
		{
			a();
		};
		dispatcherQueueTimer.Start();
	}
}
