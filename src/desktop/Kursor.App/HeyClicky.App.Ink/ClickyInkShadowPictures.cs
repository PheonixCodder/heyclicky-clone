using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public static class ClickyInkShadowPictures
{
	private const double Scale = 2.0;

	private static readonly Dictionary<string, (ImageSource Source, double Reach)> Cache = new Dictionary<string, (ImageSource, double)>();

	public static (ImageSource Source, double Reach) Picture(IClickyShape shape, string shapeKey, Size size, PaperShadow shadow)
	{
		string key = $"{shapeKey}|{size.Width:0.#}x{size.Height:0.#}|{shadow.Color}|{shadow.Radius}";
		if (Cache.TryGetValue(key, out (ImageSource, double) value))
		{
			return value;
		}
		double num = shadow.Radius * 0.5;
		double num2 = Math.Ceiling(num * 3.0 + 1.0);
		int num3 = (int)Math.Ceiling((size.Width + 2.0 * num2) * 2.0);
		int num4 = (int)Math.Ceiling((size.Height + 2.0 * num2) * 2.0);
		CanvasDevice sharedDevice = CanvasDevice.GetSharedDevice();
		using CanvasRenderTarget canvasRenderTarget = new CanvasRenderTarget(sharedDevice, num3, num4, 96f);
		using (CanvasDrawingSession canvasDrawingSession = canvasRenderTarget.CreateDrawingSession())
		{
			canvasDrawingSession.Clear(Colors.Transparent);
			canvasDrawingSession.Transform = Matrix3x2.CreateScale(2f) * Matrix3x2.CreateTranslation(0f, 0f);
			using CanvasGeometry geometry = shape.PathIn(new Rect(num2, num2, size.Width, size.Height)).ToCanvasGeometry(sharedDevice);
			using CanvasCommandList canvasCommandList = new CanvasCommandList(sharedDevice);
			using (CanvasDrawingSession canvasDrawingSession2 = canvasCommandList.CreateDrawingSession())
			{
				canvasDrawingSession2.FillGeometry(geometry, Colors.Black);
			}
			using ShadowEffect image = new ShadowEffect
			{
				Source = canvasCommandList,
				BlurAmount = (float)num,
				ShadowColor = shadow.Color,
				Optimization = EffectOptimization.Quality
			};
			canvasDrawingSession.DrawImage(image);
		}
		byte[] pixelBytes = canvasRenderTarget.GetPixelBytes();
		WriteableBitmap writeableBitmap = new WriteableBitmap(num3, num4);
		using (Stream stream = writeableBitmap.PixelBuffer.AsStream())
		{
			stream.Write(pixelBytes, 0, pixelBytes.Length);
		}
		writeableBitmap.Invalidate();
		if (Cache.Count > 300)
		{
			Cache.Clear();
		}
		Cache[key] = (writeableBitmap, num2);
		return (Source: writeableBitmap, Reach: num2);
	}
}
