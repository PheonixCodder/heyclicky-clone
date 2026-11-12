using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Portraits;

public static class PortraitWash
{
	private const int Width = 320;

	private const int Height = 237;

	private const float Supersample = 2f;

	private static readonly Dictionary<(Hairstyle, Colorway, int), WriteableBitmap> cache = new Dictionary<(Hairstyle, Colorway, int), WriteableBitmap>();

	public static WriteableBitmap? For(PortraitAppearance a, (double X, double Y, double W, double H)? crop)
	{
		(Hairstyle, Colorway, int) key = (a.Hairstyle, a.HairColor, crop.HasValue ? 1 : 0);
		if (cache.TryGetValue(key, out WriteableBitmap value))
		{
			return value;
		}
		try
		{
			CanvasDevice sharedDevice = CanvasDevice.GetSharedDevice();
			using CanvasRenderTarget canvasRenderTarget = Render(sharedDevice, a);
			WriteableBitmap writeableBitmap;
			if (crop.HasValue)
			{
				(double, double, double, double) valueOrDefault = crop.GetValueOrDefault();
				writeableBitmap = Crop(sharedDevice, canvasRenderTarget, valueOrDefault);
			}
			else
			{
				writeableBitmap = Copy(canvasRenderTarget);
			}
			WriteableBitmap writeableBitmap2 = writeableBitmap;
			if (cache.Count > 64)
			{
				cache.Clear();
			}
			cache[key] = writeableBitmap2;
			return writeableBitmap2;
		}
		catch (Exception ex)
		{
			Log.Warn("portrait", "wash failed: " + ex.Message);
			return null;
		}
	}

	private static CanvasRenderTarget Render(CanvasDevice device, PortraitAppearance a)
	{
		CloudFaceTraits cloudFaceTraits = PortraitArt.ClassicTraits(a);
		(uint, uint, uint) tuple = a.HairColor.ClassicCloudPalette();
		CanvasRenderTarget canvasRenderTarget = new CanvasRenderTarget(device, 640f, 474f, 96f);
		using CanvasDrawingSession canvasDrawingSession = canvasRenderTarget.CreateDrawingSession();
		canvasDrawingSession.Clear(PortraitData.Rgb(tuple.Item1));
		int num = 0;
		foreach (CloudFaceTraits.Blob colorBlob in cloudFaceTraits.ColorBlobs)
		{
			float num2 = (float)(320.0 * colorBlob.Diameter) * 2f;
			Vector2 centerPoint = new Vector2((float)(320.0 * colorBlob.CenterX), (float)(237.0 * colorBlob.CenterY)) * 2f;
			Color color = PortraitData.Rgb(num switch
			{
				1 => tuple.Item3, 
				0 => tuple.Item2, 
				_ => tuple.Item1, 
			});
			using CanvasCommandList canvasCommandList = new CanvasCommandList(device);
			using (CanvasDrawingSession canvasDrawingSession2 = canvasCommandList.CreateDrawingSession())
			{
				canvasDrawingSession2.FillCircle(centerPoint, num2 / 2f, color);
			}
			using GaussianBlurEffect image = new GaussianBlurEffect
			{
				Source = canvasCommandList,
				BlurAmount = 102.399994f,
				BorderMode = EffectBorderMode.Soft,
				Optimization = EffectOptimization.Quality
			};
			canvasDrawingSession.DrawImage(image);
			num++;
		}
		return canvasRenderTarget;
	}

	private static WriteableBitmap Crop(CanvasDevice device, CanvasRenderTarget wash, (double X, double Y, double W, double H) crop)
	{
		(double, double, double, double) classicBodyRect = PortraitArt.ClassicBodyRect;
		int num = Math.Max(1, (int)Math.Ceiling(crop.W * 2.0));
		int num2 = Math.Max(1, (int)Math.Ceiling(crop.H * 2.0));
		using CanvasRenderTarget canvasRenderTarget = new CanvasRenderTarget(device, num, num2, 96f);
		using (CanvasDrawingSession canvasDrawingSession = canvasRenderTarget.CreateDrawingSession())
		{
			canvasDrawingSession.Clear(_003CPortraitWash_003EF2FF3F8DF461DCABE6D955F65C17EB9EF429F49A32432E29B2D7E392AB32ECE00__Colors.Transparent);
			Rect destinationRectangle = new Rect((classicBodyRect.Item1 - crop.X) * 2.0, (classicBodyRect.Item2 - crop.Y) * 2.0, classicBodyRect.Item3 * 2.0, classicBodyRect.Item4 * 2.0);
			canvasDrawingSession.DrawImage(wash, destinationRectangle, wash.Bounds, 1f, CanvasImageInterpolation.HighQualityCubic);
		}
		return Copy(canvasRenderTarget);
	}

	private static WriteableBitmap Copy(CanvasRenderTarget target)
	{
		byte[] pixelBytes = target.GetPixelBytes();
		WriteableBitmap writeableBitmap = new WriteableBitmap((int)target.SizeInPixels.Width, (int)target.SizeInPixels.Height);
		using (Stream stream = writeableBitmap.PixelBuffer.AsStream())
		{
			stream.Write(pixelBytes, 0, pixelBytes.Length);
		}
		writeableBitmap.Invalidate();
		return writeableBitmap;
	}
}
