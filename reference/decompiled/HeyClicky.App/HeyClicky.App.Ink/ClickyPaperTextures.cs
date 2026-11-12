using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using HeyClicky.App.Design;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics.DirectX;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyPaperTextures
{
	public sealed record StampImage(byte[] Bgra, int PixelWidth, int PixelHeight, Size Size)
	{
		public WriteableBitmap Bitmap
		{
			get
			{
				if ((object)bitmap != null)
				{
					return bitmap;
				}
				bitmap = new WriteableBitmap(PixelWidth, PixelHeight);
				using (Stream stream = bitmap.PixelBuffer.AsStream())
				{
					stream.Write(Bgra, 0, Bgra.Length);
				}
				bitmap.Invalidate();
				return bitmap;
			}
		}

		private WriteableBitmap? bitmap;

		[CompilerGenerated]
		private bool PrintMembers(StringBuilder builder)
		{
			RuntimeHelpers.EnsureSufficientExecutionStack();
			builder.Append("Bgra = ");
			builder.Append(Bgra);
			builder.Append(", PixelWidth = ");
			builder.Append(PixelWidth.ToString());
			builder.Append(", PixelHeight = ");
			builder.Append(PixelHeight.ToString());
			builder.Append(", Size = ");
			builder.Append(Size.ToString());
			builder.Append(", Bitmap = ");
			builder.Append(Bitmap);
			return true;
		}
	}

	public const int GrainTileSize = 160;

	public const double GrainTileScale = 2.0;

	public static readonly byte[] GrainTileRgba = MakeGrainTile(160, 7uL);

	private static readonly Dictionary<CanvasDevice, CanvasBitmap> grainBitmaps = new Dictionary<CanvasDevice, CanvasBitmap>();

	private static readonly Dictionary<string, StampImage> stampCache = new Dictionary<string, StampImage>();

	private static byte[] GrainTileBgra
	{
		get
		{
			byte[] array = new byte[GrainTileRgba.Length];
			for (int i = 0; i < array.Length; i += 4)
			{
				array[i] = GrainTileRgba[i + 2];
				array[i + 1] = GrainTileRgba[i + 1];
				array[i + 2] = GrainTileRgba[i];
				array[i + 3] = GrainTileRgba[i + 3];
			}
			return array;
		}
	}

	private static byte[] MakeGrainTile(int size, ulong seed)
	{
		ClickyPaperRandom clickyPaperRandom = new ClickyPaperRandom(seed);
		double[] array = new double[size * size];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = clickyPaperRandom.NextUnit();
		}
		double[] array2 = new double[size * size];
		for (int j = 0; j < size; j++)
		{
			for (int k = 0; k < size; k++)
			{
				double num = 0.0;
				for (int l = -1; l <= 1; l++)
				{
					for (int m = -1; m <= 1; m++)
					{
						int num2 = (k + m + size) % size;
						int num3 = (j + l + size) % size;
						num += array[num3 * size + num2] * (double)((m != 0 || l != 0) ? 1 : 4);
					}
				}
				array2[j * size + k] = num / 12.0;
			}
		}
		byte[] array3 = new byte[size * size * 4];
		for (int n = 0; n < size * size; n++)
		{
			double num4 = Math.Pow(array2[n], 2.2);
			double num5 = Math.Min(1.0, num4 * 0.3);
			array3[n * 4] = (byte)(107.0 * num5);
			array3[n * 4 + 1] = (byte)(102.0 * num5);
			array3[n * 4 + 2] = (byte)(92.0 * num5);
			array3[n * 4 + 3] = (byte)(255.0 * num5);
		}
		return array3;
	}

	public static CanvasBitmap GrainBitmap(CanvasDevice device)
	{
		lock (grainBitmaps)
		{
			if (grainBitmaps.TryGetValue(device, out CanvasBitmap value))
			{
				return value;
			}
			value = CanvasBitmap.CreateFromBytes(device, GrainTileBgra, 160, 160, DirectXPixelFormat.B8G8R8A8UIntNormalized, 192f, CanvasAlphaMode.Premultiplied);
			grainBitmaps[device] = value;
			device.DeviceLost += delegate
			{
				lock (grainBitmaps)
				{
					grainBitmaps.Remove(device);
				}
			};
			return value;
		}
	}

	public static void DrawGrain(CanvasDrawingSession ds, Rect bounds, double opacity)
	{
		CanvasBitmap canvasBitmap = GrainBitmap(ds.Device);
		double num = 80.0;
		for (double num2 = bounds.Y - (bounds.Y % num + num) % num; num2 < bounds.Bottom; num2 += num)
		{
			for (double num3 = bounds.X - (bounds.X % num + num) % num; num3 < bounds.Right; num3 += num)
			{
				ds.DrawImage(canvasBitmap, new Rect(num3, num2, num, num), canvasBitmap.Bounds, (float)opacity, CanvasImageInterpolation.NearestNeighbor);
			}
		}
	}

	public static ICanvasImage GrainImage(ICanvasResourceCreator device, Rect bounds, double opacity)
	{
		CanvasCommandList canvasCommandList = new CanvasCommandList(device);
		using CanvasDrawingSession ds = canvasCommandList.CreateDrawingSession();
		DrawGrain(ds, bounds, opacity);
		return canvasCommandList;
	}

	private static PaperFont StampFont(double fontSize)
	{
		return new PaperFont(DS.Fonts.For(fontSize), (fontSize < 12.0) ? "Segoe UI Variable Small" : ((fontSize >= 20.0) ? "Segoe UI Variable Display" : "Segoe UI Variable Text"), fontSize, FontWeights.ExtraBold);
	}

	public static StampImage Stamp(string word, Color color, double fontSize)
	{
		string key = $"{word}|{color}|{fontSize}";
		lock (stampCache)
		{
			if (stampCache.TryGetValue(key, out StampImage value))
			{
				return value;
			}
		}
		CanvasDevice sharedDevice = CanvasDevice.GetSharedDevice();
		double num = fontSize * 0.09;
		using CanvasTextFormat textFormat = StampFont(fontSize).TextFormat();
		using CanvasTextLayout canvasTextLayout = new CanvasTextLayout(sharedDevice, word, textFormat, 10000f, 1000f);
		if (word.Length > 0)
		{
			canvasTextLayout.SetCharacterSpacing(0, word.Length, 0f, (float)num, 0f);
		}
		double num2 = canvasTextLayout.LayoutBoundsIncludingTrailingWhitespace.Width - num;
		Size size = new Size(Math.Ceiling(num2 + 16.0), Math.Ceiling(fontSize + 10.0));
		using CanvasRenderTarget canvasRenderTarget = new CanvasRenderTarget(sharedDevice, (float)size.Width, (float)size.Height, 288f);
		using (CanvasDrawingSession canvasDrawingSession = canvasRenderTarget.CreateDrawingSession())
		{
			canvasDrawingSession.Clear(Colors.Transparent);
			canvasDrawingSession.DrawRoundedRectangle(new Rect(1.0, 1.0, size.Width - 2.0, size.Height - 2.0), 3.5f, 3.5f, color, 1.7f);
			double num3 = (size.Height - fontSize * 0.72) / 2.0;
			double num4 = size.Height - num3;
			double num5 = ((canvasTextLayout.LineMetrics.Length != 0) ? ((double)canvasTextLayout.LineMetrics[0].Baseline) : (fontSize * 0.8));
			canvasDrawingSession.DrawTextLayout(canvasTextLayout, new Vector2((float)((size.Width - num2) / 2.0), (float)(num4 - num5)), color);
			ClickyPaperRandom clickyPaperRandom = new ClickyPaperRandom(ClickyPaperRandom.StableSeed(word) + 11);
			canvasDrawingSession.Blend = CanvasBlend.Copy;
			int num6 = (int)(size.Width * size.Height / 26.0);
			for (int i = 0; i < num6; i++)
			{
				double num7 = clickyPaperRandom.NextUnit() * size.Width;
				double num8 = clickyPaperRandom.NextUnit() * size.Height;
				double num9 = 0.18 + clickyPaperRandom.NextUnit() * 0.42;
				double num10 = 0.6 + clickyPaperRandom.NextUnit() * 0.8;
				double num11 = size.Height - (num8 - num9 + 2.0 * num9 * num10);
				canvasDrawingSession.FillEllipse(new Vector2((float)num7, (float)(num11 + num9 * num10)), (float)num9, (float)(num9 * num10), Colors.Transparent);
			}
		}
		StampImage stampImage = new StampImage(canvasRenderTarget.GetPixelBytes(), (int)canvasRenderTarget.SizeInPixels.Width, (int)canvasRenderTarget.SizeInPixels.Height, size);
		lock (stampCache)
		{
			stampCache[key] = stampImage;
		}
		return stampImage;
	}
}
