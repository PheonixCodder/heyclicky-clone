using System;
using System.Collections.Generic;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.Drive;
using Microsoft.Graphics.Canvas;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.DirectX;
using Windows.UI;

namespace HeyClicky.App.Dock;

internal static class ArcPaperTexture
{
	private static readonly Dictionary<int, WriteableBitmap?> cache = new Dictionary<int, WriteableBitmap>();

	public static FrameworkElement Create()
	{
		Grid host = new Grid
		{
			IsHitTestVisible = false
		};
		host.Loaded += delegate
		{
			if (host.Children.Count <= 0)
			{
				WriteableBitmap writeableBitmap = Bitmap(Math.Max(1.0, host.XamlRoot?.RasterizationScale ?? 1.0));
				if ((object)writeableBitmap != null)
				{
					host.Children.Add(new Image
					{
						Source = writeableBitmap,
						Stretch = Stretch.Fill
					});
				}
			}
		};
		return host;
	}

	private static WriteableBitmap? Bitmap(double scale)
	{
		int key = (int)Math.Round(scale * 100.0);
		if (cache.TryGetValue(key, out WriteableBitmap value))
		{
			return value;
		}
		WriteableBitmap writeableBitmap = null;
		try
		{
			CanvasDevice sharedDevice = CanvasDevice.GetSharedDevice();
			float num = 480f;
			float num2 = 300f;
			int num3 = (int)Math.Ceiling((double)num * scale);
			int num4 = (int)Math.Ceiling((double)num2 * scale);
			double[] array = GrainTile();
			byte[] array2 = new byte[num3 * num4 * 4];
			for (int i = 0; i < num4; i++)
			{
				for (int j = 0; j < num3; j++)
				{
					int num5 = (int)((double)j / scale * 2.0) % 160;
					int num6 = (int)((double)i / scale * 2.0) % 160;
					byte b = (byte)Math.Round(array[num6 * 160 + num5] * 0.45 * 0.6 * 255.0);
					array2[(i * num3 + j) * 4 + 3] = b;
				}
			}
			using CanvasRenderTarget canvasRenderTarget = new CanvasRenderTarget(sharedDevice, num3, num4, 96f);
			using (CanvasDrawingSession canvasDrawingSession = canvasRenderTarget.CreateDrawingSession())
			{
				canvasDrawingSession.Clear(Color.FromArgb(0, 0, 0, 0));
				using (CanvasBitmap image = CanvasBitmap.CreateFromBytes(sharedDevice, array2, num3, num4, DirectXPixelFormat.B8G8R8A8UIntNormalized, 96f))
				{
					canvasDrawingSession.DrawImage(image);
				}
				canvasDrawingSession.Transform = Matrix3x2.CreateScale((float)scale);
				float num7 = 6.4f;
				for (float num8 = 16f; num8 <= num2 - num7; num8 += 16f)
				{
					for (float num9 = 16f; num9 <= num - num7; num9 += 16f)
					{
						canvasDrawingSession.FillCircle(num9, num8, 1.1f, DS.Black(0.11));
					}
				}
			}
			writeableBitmap = DriveArt.Copy(canvasRenderTarget);
		}
		catch (Exception ex)
		{
			Log.Warn("dock", "paper texture: " + ex.Message);
		}
		cache[key] = writeableBitmap;
		return writeableBitmap;
	}

	private static double[] GrainTile()
	{
		DriveArt.PaperRandom paperRandom = new DriveArt.PaperRandom(7uL);
		double[] array = new double[25600];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = paperRandom.NextUnit();
		}
		double[] array2 = new double[25600];
		for (int j = 0; j < 160; j++)
		{
			for (int k = 0; k < 160; k++)
			{
				double num = 0.0;
				for (int l = -1; l <= 1; l++)
				{
					for (int m = -1; m <= 1; m++)
					{
						num += array[(j + l + 160) % 160 * 160 + (k + m + 160) % 160] * (double)((m != 0 || l != 0) ? 1 : 4);
					}
				}
				array2[j * 160 + k] = Math.Min(1.0, Math.Pow(num / 12.0, 2.2) * 0.3);
			}
		}
		return array2;
	}
}
