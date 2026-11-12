using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using HeyClicky.App.Design;
using HeyClicky.App.Home;
using HeyClicky.App.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI;

namespace HeyClicky.App.Notch.Peek;

public sealed class ChatSentAttachments : StackPanel
{
	private const double PreviewSize = 52.0;

	private static readonly double[] Tilts = new double[4] { -5.0, 4.0, -2.5, 6.0 };

	private const int MaximumShown = 4;

	public ChatSentAttachments(IReadOnlyList<string> paths, Action<string> open)
	{
		MessagesPalette current = MessagesPalette.Current;
		base.Orientation = Orientation.Horizontal;
		List<string> list = paths.Take(4).ToList();
		base.Spacing = ((list.Count > 1) ? (-18) : 0);
		base.Padding = new Thickness(4.0);
		for (int i = 0; i < list.Count; i++)
		{
			base.Children.Add(Preview(list[i], (list.Count > 1) ? Tilts[i % Tilts.Length] : 0.0, open, current));
		}
		if (paths.Count > 4)
		{
			Border border = new Border
			{
				Height = 24.0,
				Padding = new Thickness(8.0, 0.0, 8.0, 0.0),
				CornerRadius = new CornerRadius(12.0),
				Background = Ui.Brush(current.InkAt(0.14)),
				Margin = new Thickness(24.0, 0.0, 0.0, 0.0),
				VerticalAlignment = VerticalAlignment.Center,
				Child = Ui.Text($"+{paths.Count - 4}", 11.0, FontWeights.SemiBold, current.InkAt(0.85)).Center()
			};
			Canvas.SetZIndex(border, 200);
			base.Children.Add(border);
		}
	}

	private static FrameworkElement Preview(string path, double tilt, Action<string> open, MessagesPalette look)
	{
		bool flag = File.Exists(path);
		Pressable p = new Pressable
		{
			Width = 58.0,
			Height = 58.0,
			Opacity = (flag ? 1.0 : 0.45)
		};
		p.Enabled = flag;
		ShadowLayer item = new ShadowLayer(11.0, DS.Black(0.35), 3.0, 1.5);
		Border item2 = new Border
		{
			CornerRadius = new CornerRadius(11.0),
			Background = Ui.Brush(look.InkAt(0.3))
		};
		Border border = new Border
		{
			Margin = new Thickness(3.0),
			CornerRadius = new CornerRadius(8.0),
			Background = Ui.Brush(look.InkAt(0.08))
		};
		if (flag && ChatBlockViews.IsImage(path))
		{
			border.Background = new ImageBrush
			{
				ImageSource = new BitmapImage(new Uri(path))
				{
					DecodePixelWidth = 156
				},
				Stretch = Stretch.UniformToFill
			};
		}
		else if (flag)
		{
			ShellIcon(border, path, look.SecondaryText);
		}
		p.Children.Add(item);
		p.Children.Add(item2);
		p.Children.Add(border);
		p.Rotation = (float)tilt;
		p.Loaded += delegate
		{
			p.CenterPoint = new Vector3((float)p.ActualWidth / 2f, (float)p.ActualHeight / 2f, 0f);
		};
		SwiftSpring spring = new SwiftSpring(0.28, 0.78);
		p.HoverChanged += delegate(bool h)
		{
			Canvas.SetZIndex(p, h ? 100 : 0);
			Anim.ScaleSpring(p, h ? 1.06f : 1f, spring, null);
			p.Rotation = (h ? 0f : ((float)tilt));
		};
		p.Click += delegate
		{
			open(path);
		};
		p.Tip(Path.GetFileName(path));
		return p;
	}

	private static async Task ShellIcon(Border inner, string path, Color fallbackInk)
	{
		_ = 2;
		try
		{
			using StorageItemThumbnail thumb = await (await StorageFile.GetFileFromPathAsync(path)).GetThumbnailAsync(ThumbnailMode.SingleItem, 96u);
			if ((object)thumb == null)
			{
				return;
			}
			BitmapImage bmp = new BitmapImage();
			await bmp.SetSourceAsync(thumb);
			inner.Child = new Image
			{
				Source = bmp,
				Margin = new Thickness(4.0),
				Stretch = Stretch.Uniform
			};
		}
		catch
		{
			inner.Child = Icons.Glyph("\ue8a5", 18.0, fallbackInk);
		}
	}
}
