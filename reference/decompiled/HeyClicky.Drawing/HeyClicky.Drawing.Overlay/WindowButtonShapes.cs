using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Windows.UI;
using Windows.UI.Composition;

namespace HeyClicky.Drawing.Overlay;

internal static class WindowButtonShapes
{
	public const int Close = 0;

	public const int Pin = 1;

	public const int FullScreen = 2;

	public const bool IsPinningEnabled = false;

	public static readonly int[] Shown = new int[2] { 0, 2 };

	public static readonly int[] CaptionsFromRight = new int[2] { 0, 2 };

	public const double CaptionButtonWidthPoints = 40.0;

	public const bool IsDownloadButtonEnabled = false;

	internal static bool OverrideDownloadButton;

	public const double DownloadButtonSizePoints = 20.0;

	public const double DownloadButtonCenterFromRightPoints = 18.0;

	public static bool ShowsDownloadButton => OverrideDownloadButton;

	public static void AddDownloadGlyph(Compositor compositor, IList<CompositionShape> shapes, (double X, double Y, double Width, double Height) rect, bool hovered, bool pressed, bool done)
	{
		if (pressed || hovered)
		{
			CompositionRoundedRectangleGeometry compositionRoundedRectangleGeometry = compositor.CreateRoundedRectangleGeometry();
			compositionRoundedRectangleGeometry.Offset = new Vector2((float)rect.X, (float)rect.Y);
			compositionRoundedRectangleGeometry.Size = new Vector2((float)rect.Width, (float)rect.Height);
			compositionRoundedRectangleGeometry.CornerRadius = new Vector2((float)(rect.Height * 0.3));
			CompositionSpriteShape compositionSpriteShape = compositor.CreateSpriteShape(compositionRoundedRectangleGeometry);
			compositionSpriteShape.FillBrush = compositor.CreateColorBrush(Color.FromArgb((byte)(pressed ? 31u : 15u), 0, 0, 0));
			shapes.Add(compositionSpriteShape);
		}
		CompositionColorBrush brush = compositor.CreateColorBrush(Color.FromArgb((byte)((hovered || pressed || done) ? 217u : 128u), 51, 51, 51));
		float num = (float)rect.Width / 20f;
		Vector2 vector = new Vector2((float)(rect.X + rect.Width / 2.0), (float)(rect.Y + rect.Height / 2.0));
		using CanvasPathBuilder canvasPathBuilder = new CanvasPathBuilder(CanvasDevice.GetSharedDevice());
		if (done)
		{
			canvasPathBuilder.BeginFigure(vector + new Vector2(-4.5f, 0.2f) * num);
			canvasPathBuilder.AddLine(vector + new Vector2(-1.4f, 3.3f) * num);
			canvasPathBuilder.AddLine(vector + new Vector2(4.8f, -3.4f) * num);
			canvasPathBuilder.EndFigure(CanvasFigureLoop.Open);
		}
		else
		{
			canvasPathBuilder.BeginFigure(vector + new Vector2(0f, -5.5f) * num);
			canvasPathBuilder.AddLine(vector + new Vector2(0f, 1.8f) * num);
			canvasPathBuilder.EndFigure(CanvasFigureLoop.Open);
			canvasPathBuilder.BeginFigure(vector + new Vector2(-3.2f, -1.4f) * num);
			canvasPathBuilder.AddLine(vector + new Vector2(0f, 1.8f) * num);
			canvasPathBuilder.AddLine(vector + new Vector2(3.2f, -1.4f) * num);
			canvasPathBuilder.EndFigure(CanvasFigureLoop.Open);
			canvasPathBuilder.BeginFigure(vector + new Vector2(-5.5f, 1.5f) * num);
			canvasPathBuilder.AddLine(vector + new Vector2(-5.5f, 5.2f) * num);
			canvasPathBuilder.AddLine(vector + new Vector2(5.5f, 5.2f) * num);
			canvasPathBuilder.AddLine(vector + new Vector2(5.5f, 1.5f) * num);
			canvasPathBuilder.EndFigure(CanvasFigureLoop.Open);
		}
		CompositionSpriteShape compositionSpriteShape2 = Stroke(compositor, canvasPathBuilder, brush, Math.Max(1f, 1.5f * num));
		compositionSpriteShape2.StrokeLineJoin = CompositionStrokeLineJoin.Round;
		shapes.Add(compositionSpriteShape2);
	}

	private static (Color Base, Color Rim, Color Glyph) Colors(int kind)
	{
		return kind switch
		{
			0 => (Base: Color.FromArgb(byte.MaxValue, byte.MaxValue, 115, 106), Rim: Color.FromArgb(byte.MaxValue, 193, 88, 82), Glyph: Color.FromArgb(204, 77, 0, 0)), 
			1 => (Base: Color.FromArgb(byte.MaxValue, 254, 188, 46), Rim: Color.FromArgb(byte.MaxValue, 209, 155, 38), Glyph: Color.FromArgb(204, 107, 69, 0)), 
			_ => (Base: Color.FromArgb(byte.MaxValue, 25, 195, 50), Rim: Color.FromArgb(byte.MaxValue, 20, 160, 42), Glyph: Color.FromArgb(204, 0, 77, 0)), 
		};
	}

	public static void Add(Compositor compositor, IList<CompositionShape> shapes, int kind, (double X, double Y, double Width, double Height) rect, bool pressed, bool showsGlyph, bool pointsInward = false, bool isPinned = false)
	{
		(Color Base, Color Rim, Color Glyph) tuple = Colors(kind);
		Color item = tuple.Base;
		Color item2 = tuple.Rim;
		Color item3 = tuple.Glyph;
		float num = (float)rect.Width;
		float num2 = num / 12f;
		Vector2 vector = new Vector2((float)(rect.X + rect.Width / 2.0), (float)(rect.Y + rect.Height / 2.0));
		float num3 = 0.5f * num2;
		CompositionSpriteShape compositionSpriteShape = Ellipse(vector, new Vector2(num / 2f - num3 / 2f), item);
		compositionSpriteShape.StrokeBrush = compositor.CreateColorBrush(item2);
		compositionSpriteShape.StrokeThickness = num3;
		shapes.Add(compositionSpriteShape);
		shapes.Add(Ellipse(new Vector2(vector.X, (float)rect.Y + num * 0.08f + num * 0.21f), new Vector2(num * 0.3f, num * 0.21f), Color.FromArgb(82, byte.MaxValue, byte.MaxValue, byte.MaxValue)));
		if (pressed)
		{
			shapes.Add(Ellipse(vector, new Vector2(num / 2f), Color.FromArgb(46, 0, 0, 0)));
		}
		if (!showsGlyph)
		{
			return;
		}
		using (CanvasPathBuilder canvasPathBuilder = new CanvasPathBuilder(CanvasDevice.GetSharedDevice()))
		{
			CompositionColorBrush compositionColorBrush = compositor.CreateColorBrush(item3);
			switch (kind)
			{
			case 0:
			{
				float num4 = num * 0.2f;
				canvasPathBuilder.BeginFigure(vector + new Vector2(0f - num4, 0f - num4));
				canvasPathBuilder.AddLine(vector + new Vector2(num4, num4));
				canvasPathBuilder.EndFigure(CanvasFigureLoop.Open);
				canvasPathBuilder.BeginFigure(vector + new Vector2(0f - num4, num4));
				canvasPathBuilder.AddLine(vector + new Vector2(num4, 0f - num4));
				canvasPathBuilder.EndFigure(CanvasFigureLoop.Open);
				shapes.Add(Stroke(compositor, canvasPathBuilder, compositionColorBrush, Math.Max(1f, num * 0.11f)));
				return;
			}
			case 1:
			{
				CompositionSpriteShape compositionSpriteShape2 = Ellipse(vector + new Vector2(num * 0.07f, (0f - num) * 0.12f), new Vector2(num * 0.15f), item3);
				compositionSpriteShape2.FillBrush = compositionColorBrush;
				shapes.Add(compositionSpriteShape2);
				canvasPathBuilder.BeginFigure(vector + new Vector2((0f - num) * 0.12f, num * 0.04f));
				canvasPathBuilder.AddLine(vector + new Vector2(num * 0.2f, num * 0.04f));
				canvasPathBuilder.EndFigure(CanvasFigureLoop.Open);
				canvasPathBuilder.BeginFigure(vector + new Vector2(0f, num * 0.04f));
				canvasPathBuilder.AddLine(vector + new Vector2((0f - num) * 0.2f, num * 0.26f));
				canvasPathBuilder.EndFigure(CanvasFigureLoop.Open);
				if (isPinned)
				{
					canvasPathBuilder.BeginFigure(vector + new Vector2((0f - num) * 0.26f, (0f - num) * 0.26f));
					canvasPathBuilder.AddLine(vector + new Vector2(num * 0.26f, num * 0.26f));
					canvasPathBuilder.EndFigure(CanvasFigureLoop.Open);
				}
				shapes.Add(Stroke(compositor, canvasPathBuilder, compositionColorBrush, Math.Max(1f, num * 0.1f)));
				return;
			}
			}
			float num5 = num * 0.3f;
			float num6 = (pointsInward ? (num * 0.06f) : (num * 0.27f));
			float[] array = new float[2] { -1f, 1f };
			foreach (float num7 in array)
			{
				Vector2 vector2 = vector + new Vector2(num7 * num6);
				float num8 = (pointsInward ? num7 : (0f - num7)) * num5;
				canvasPathBuilder.BeginFigure(vector2);
				canvasPathBuilder.AddLine(vector2 + new Vector2(num8, 0f));
				canvasPathBuilder.AddLine(vector2 + new Vector2(0f, num8));
				canvasPathBuilder.EndFigure(CanvasFigureLoop.Closed);
			}
			CompositionSpriteShape compositionSpriteShape3 = compositor.CreateSpriteShape(compositor.CreatePathGeometry(new CompositionPath(CanvasGeometry.CreatePath(canvasPathBuilder))));
			compositionSpriteShape3.FillBrush = compositionColorBrush;
			shapes.Add(compositionSpriteShape3);
		}
		CompositionSpriteShape Ellipse(Vector2 middle, Vector2 radius, Color fill)
		{
			CompositionEllipseGeometry compositionEllipseGeometry = compositor.CreateEllipseGeometry();
			compositionEllipseGeometry.Center = middle;
			compositionEllipseGeometry.Radius = radius;
			CompositionSpriteShape compositionSpriteShape4 = compositor.CreateSpriteShape(compositionEllipseGeometry);
			compositionSpriteShape4.FillBrush = compositor.CreateColorBrush(fill);
			return compositionSpriteShape4;
		}
	}

	public static void AddCaption(Compositor compositor, IList<CompositionShape> shapes, int kind, (double X, double Y, double Width, double Height) rect, double unit, bool hovered, bool pressed, bool restores, double cornerRadius, double frameLine)
	{
		bool flag = hovered || pressed;
		float num = (float)rect.X;
		float num2 = (float)(rect.Y + frameLine);
		float num3 = (float)(rect.Width - ((kind == 0) ? frameLine : 0.0));
		float num4 = (float)(rect.Height - frameLine);
		if (flag)
		{
			using CanvasPathBuilder canvasPathBuilder = new CanvasPathBuilder(CanvasDevice.GetSharedDevice());
			float num5 = ((kind == 0) ? ((float)Math.Max(0.0, Math.Min(cornerRadius - frameLine, Math.Min(num3, num4)))) : 0f);
			canvasPathBuilder.BeginFigure(num, num2);
			canvasPathBuilder.AddLine(num + num3 - num5, num2);
			if (num5 > 0f)
			{
				canvasPathBuilder.AddArc(new Vector2(num + num3, num2 + num5), num5, num5, 0f, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);
			}
			canvasPathBuilder.AddLine(num + num3, num2 + num4);
			canvasPathBuilder.AddLine(num, num2 + num4);
			canvasPathBuilder.EndFigure(CanvasFigureLoop.Closed);
			CompositionSpriteShape compositionSpriteShape = compositor.CreateSpriteShape(compositor.CreatePathGeometry(new CompositionPath(CanvasGeometry.CreatePath(canvasPathBuilder))));
			compositionSpriteShape.FillBrush = compositor.CreateColorBrush((kind == 0) ? Color.FromArgb((byte)(pressed ? 230u : 255u), 196, 43, 28) : Color.FromArgb((byte)(pressed ? 10u : 15u), 0, 0, 0));
			shapes.Add(compositionSpriteShape);
		}
		Color color = ((kind == 0 && flag) ? Color.FromArgb((byte)(pressed ? 178u : 255u), byte.MaxValue, byte.MaxValue, byte.MaxValue) : Color.FromArgb((byte)(pressed ? 143u : 204u), 0, 0, 0));
		float num6 = (float)(10.0 * unit);
		float strokeThickness = (float)unit;
		Vector2 origin = new Vector2((float)(rect.X + rect.Width / 2.0) - num6 / 2f, (float)(rect.Y + rect.Height / 2.0) - num6 / 2f);
		CanvasPathBuilder builder = new CanvasPathBuilder(CanvasDevice.GetSharedDevice());
		try
		{
			if (kind == 0)
			{
				Lines(closed: false, new(float, float)[2]
				{
					(0f, 0f),
					(10f, 10f)
				});
				Lines(closed: false, new(float, float)[2]
				{
					(0f, 10f),
					(10f, 0f)
				});
			}
			else if (!restores)
			{
				Lines(closed: true, new(float, float)[4]
				{
					(0.5f, 0.5f),
					(9.5f, 0.5f),
					(9.5f, 9.5f),
					(0.5f, 9.5f)
				});
			}
			else
			{
				Lines(closed: true, new(float, float)[4]
				{
					(0.5f, 2.5f),
					(7.5f, 2.5f),
					(7.5f, 9.5f),
					(0.5f, 9.5f)
				});
				Lines(closed: false, new(float, float)[5]
				{
					(2.5f, 2.5f),
					(2.5f, 0.5f),
					(9.5f, 0.5f),
					(9.5f, 7.5f),
					(7.5f, 7.5f)
				});
			}
			CompositionSpriteShape compositionSpriteShape2 = compositor.CreateSpriteShape(compositor.CreatePathGeometry(new CompositionPath(CanvasGeometry.CreatePath(builder))));
			compositionSpriteShape2.StrokeBrush = compositor.CreateColorBrush(color);
			compositionSpriteShape2.StrokeThickness = strokeThickness;
			shapes.Add(compositionSpriteShape2);
		}
		finally
		{
			if ((object)builder != null)
			{
				((IDisposable)builder).Dispose();
			}
		}
		Vector2 At(float gx, float gy)
		{
			return origin + new Vector2(gx, gy) * (float)unit;
		}
		void Lines(bool closed, (float X, float Y)[] points)
		{
			builder.BeginFigure(At(points[0].X, points[0].Y));
			foreach (var (gx2, gy2) in points.Skip(1))
			{
				builder.AddLine(At(gx2, gy2));
			}
			builder.EndFigure(closed ? CanvasFigureLoop.Closed : CanvasFigureLoop.Open);
		}
	}

	private static CompositionSpriteShape Stroke(Compositor compositor, CanvasPathBuilder builder, CompositionBrush brush, float width)
	{
		CompositionSpriteShape compositionSpriteShape = compositor.CreateSpriteShape(compositor.CreatePathGeometry(new CompositionPath(CanvasGeometry.CreatePath(builder))));
		compositionSpriteShape.StrokeBrush = brush;
		compositionSpriteShape.StrokeThickness = width;
		CompositionStrokeCap strokeStartCap = (compositionSpriteShape.StrokeEndCap = CompositionStrokeCap.Round);
		compositionSpriteShape.StrokeStartCap = strokeStartCap;
		return compositionSpriteShape;
	}
}
