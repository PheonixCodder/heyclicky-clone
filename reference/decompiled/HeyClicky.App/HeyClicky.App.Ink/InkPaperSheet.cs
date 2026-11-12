using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class InkPaperSheet : Grid
{
	private sealed record Drawn(ImageSource Source, double Reach);

	private static readonly Dictionary<string, Drawn> cache = new Dictionary<string, Drawn>();

	private static readonly LinkedList<string> recency = new LinkedList<string>();

	private const int MaxEntries = 96;

	private readonly string key;

	private readonly IClickyShape shape;

	private readonly PaperFill fill;

	private readonly double? grain;

	private readonly PaperShadow[] shadows;

	private readonly PaperStroke? outline;

	private readonly PaperFill? wash;

	private readonly Canvas canvas = new Canvas
	{
		IsHitTestVisible = false
	};

	private readonly Image image = new Image
	{
		Stretch = Stretch.Fill,
		IsHitTestVisible = false
	};

	private string drawnKey = "";

	private new double Scale
	{
		get
		{
			double? num = base.XamlRoot?.RasterizationScale;
			if (num.HasValue)
			{
				double valueOrDefault = num.GetValueOrDefault();
				if (valueOrDefault > 0.0)
				{
					return valueOrDefault;
				}
			}
			return 2.0;
		}
	}

	public InkPaperSheet(string key, IClickyShape shape, PaperFill fill, double? grain = null, PaperShadow[]? shadows = null, PaperStroke? outline = null, PaperFill? wash = null)
	{
		this.key = key;
		this.shape = shape;
		this.fill = fill;
		this.grain = grain;
		this.shadows = shadows ?? Array.Empty<PaperShadow>();
		this.outline = outline;
		this.wash = wash;
		base.IsHitTestVisible = false;
		canvas.Children.Add(image);
		base.Children.Add(canvas);
		base.SizeChanged += delegate
		{
			Draw();
		};
		base.Loaded += delegate
		{
			Draw();
		};
	}

	private void Draw()
	{
		double actualWidth = base.ActualWidth;
		double actualHeight = base.ActualHeight;
		if (actualWidth <= 0.0 || actualHeight <= 0.0)
		{
			return;
		}
		double scale = Scale;
		string text = $"{key}|{actualWidth:0.##}x{actualHeight:0.##}@{scale:0.##}";
		if (text == drawnKey)
		{
			return;
		}
		drawnKey = text;
		if (!cache.TryGetValue(text, out Drawn value))
		{
			try
			{
				value = Render(actualWidth, actualHeight, scale);
			}
			catch (Exception ex)
			{
				Log.Warn("ink", "paper sheet " + key + ": " + ex.Message);
				return;
			}
			cache[text] = value;
			recency.AddFirst(text);
			while (recency.Count > 96)
			{
				cache.Remove(recency.Last.Value);
				recency.RemoveLast();
			}
		}
		image.Source = value.Source;
		image.Width = actualWidth + 2.0 * value.Reach;
		image.Height = actualHeight + 2.0 * value.Reach;
		Canvas.SetLeft(image, 0.0 - value.Reach);
		Canvas.SetTop(image, 0.0 - value.Reach);
	}

	private Drawn Render(double w, double h, double scale)
	{
		double num = outline?.Width ?? 0.0;
		PaperShadow[] array = shadows;
		foreach (PaperShadow paperShadow in array)
		{
			num = Math.Max(num, paperShadow.Radius * 0.5 * 3.0 + Math.Max(Math.Abs(paperShadow.X), Math.Abs(paperShadow.Y)) + 1.0);
		}
		num = Math.Ceiling(num);
		CanvasDevice sharedDevice = CanvasDevice.GetSharedDevice();
		CanvasImageSource canvasImageSource = new CanvasImageSource(sharedDevice, (float)(w + 2.0 * num), (float)(h + 2.0 * num), (float)(96.0 * scale));
		using (CanvasDrawingSession canvasDrawingSession = canvasImageSource.CreateDrawingSession(Colors.Transparent))
		{
			canvasDrawingSession.Transform = Matrix3x2.CreateTranslation((float)num, (float)num);
			Rect rect = new Rect(0.0, 0.0, w, h);
			using CanvasGeometry canvasGeometry = shape.PathIn(rect).ToCanvasGeometry(sharedDevice);
			array = shadows;
			foreach (PaperShadow paperShadow2 in array)
			{
				using CanvasCommandList canvasCommandList = new CanvasCommandList(sharedDevice);
				using (CanvasDrawingSession canvasDrawingSession2 = canvasCommandList.CreateDrawingSession())
				{
					canvasDrawingSession2.FillGeometry(canvasGeometry, Colors.Black);
				}
				using ShadowEffect shadowEffect = new ShadowEffect
				{
					Source = canvasCommandList,
					BlurAmount = (float)(paperShadow2.Radius * 0.5),
					ShadowColor = paperShadow2.Color,
					Optimization = EffectOptimization.Quality
				};
				canvasDrawingSession.DrawImage(shadowEffect, (float)paperShadow2.X, (float)paperShadow2.Y);
			}
			using (canvasDrawingSession.CreateLayer(1f, canvasGeometry))
			{
				using CanvasCommandList canvasCommandList2 = new CanvasCommandList(sharedDevice);
				using (CanvasDrawingSession canvasDrawingSession3 = canvasCommandList2.CreateDrawingSession())
				{
					canvasDrawingSession3.FillRectangle(rect, fill.Brush(sharedDevice, rect));
				}
				double? num2 = grain;
				if (num2.HasValue)
				{
					double valueOrDefault = num2.GetValueOrDefault();
					if (valueOrDefault > 0.0)
					{
						using (ICanvasImage foreground = ClickyPaperTextures.GrainImage(sharedDevice, rect, valueOrDefault))
						{
							using BlendEffect blendEffect = new BlendEffect
							{
								Background = canvasCommandList2,
								Foreground = foreground,
								Mode = BlendEffectMode.Multiply
							};
							canvasDrawingSession.DrawImage(blendEffect);
						}
						goto IL_0290;
					}
				}
				canvasDrawingSession.DrawImage(canvasCommandList2);
				goto IL_0290;
				IL_0290:
				if ((object)wash != null)
				{
					canvasDrawingSession.FillRectangle(rect, wash.Brush(sharedDevice, rect));
				}
			}
			PaperStroke paperStroke = outline;
			if ((object)paperStroke != null)
			{
				using CanvasStrokeStyle strokeStyle = new CanvasStrokeStyle
				{
					StartCap = ((paperStroke.Cap == PenLineCap.Round) ? CanvasCapStyle.Round : CanvasCapStyle.Flat),
					EndCap = ((paperStroke.Cap == PenLineCap.Round) ? CanvasCapStyle.Round : CanvasCapStyle.Flat),
					LineJoin = ((paperStroke.Join == PenLineJoin.Round) ? CanvasLineJoin.Round : CanvasLineJoin.Miter)
				};
				canvasDrawingSession.DrawGeometry(canvasGeometry, paperStroke.Color, (float)paperStroke.Width, strokeStyle);
			}
		}
		return new Drawn(canvasImageSource, num);
	}
}
