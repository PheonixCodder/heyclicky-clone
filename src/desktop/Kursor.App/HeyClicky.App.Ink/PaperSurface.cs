using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Ink;

public sealed class PaperSurface : Grid
{
	private readonly CanvasControl canvas = new CanvasControl
	{
		IsHitTestVisible = false
	};

	private IClickyShape shape;

	private double reach;

	public IClickyShape Shape
	{
		get
		{
			return shape;
		}
		set
		{
			shape = value;
			canvas.Invalidate();
		}
	}

	public PaperFill Fill { get; set; }

	public double? GrainOpacity { get; set; }

	public List<PaperWash> Washes { get; } = new List<PaperWash>();

	public PaperStroke? Outline { get; set; }

	public bool OutlineIsBorder { get; set; }

	public List<PaperShadow> Shadows { get; } = new List<PaperShadow>();

	public double SheetOpacity { get; set; } = 1.0;

	public PaperSurface(IClickyShape shape, PaperFill fill, double? grainOpacity = null)
	{
		this.shape = shape;
		Fill = fill;
		GrainOpacity = grainOpacity;
		base.IsHitTestVisible = false;
		base.Children.Add(canvas);
		canvas.Draw += delegate(CanvasControl _, CanvasDrawEventArgs e)
		{
			Draw(e.DrawingSession);
		};
		base.SizeChanged += delegate
		{
			Layout();
		};
	}

	public PaperSurface WithShadow(Color color, double radius, double x = 0.0, double y = 0.0)
	{
		Shadows.Add(new PaperShadow(color, radius, x, y));
		Layout();
		return this;
	}

	public PaperSurface WithWash(PaperFill fill, Func<Size, Rect>? frame = null, bool clipped = true)
	{
		Washes.Add(new PaperWash(fill, frame, clipped));
		canvas.Invalidate();
		return this;
	}

	public PaperSurface WithOutline(PaperStroke outline, bool isBorder = false)
	{
		Outline = outline;
		OutlineIsBorder = isBorder;
		Layout();
		return this;
	}

	public void Redraw()
	{
		canvas.Invalidate();
	}

	private void Layout()
	{
		reach = Outline?.Width ?? 0.0;
		foreach (PaperShadow shadow in Shadows)
		{
			reach = Math.Max(reach, shadow.Radius * 0.5 * 3.0 + Math.Max(Math.Abs(shadow.X), Math.Abs(shadow.Y)) + 1.0);
		}
		reach = Math.Ceiling(reach);
		canvas.Margin = new Thickness(0.0 - reach);
		canvas.Invalidate();
	}

	private void Draw(CanvasDrawingSession ds)
	{
		double actualWidth = base.ActualWidth;
		double actualHeight = base.ActualHeight;
		if (actualWidth <= 0.0 || actualHeight <= 0.0)
		{
			return;
		}
		Rect rect = new Rect(0.0, 0.0, actualWidth, actualHeight);
		ds.Transform = Matrix3x2.CreateTranslation((float)reach, (float)reach);
		CanvasDevice device = ds.Device;
		using CanvasGeometry canvasGeometry = shape.PathIn(rect).ToCanvasGeometry(device);
		using ((SheetOpacity < 1.0) ? ds.CreateLayer((float)SheetOpacity) : null)
		{
			foreach (PaperShadow shadow in Shadows)
			{
				using CanvasCommandList canvasCommandList = new CanvasCommandList(device);
				using (CanvasDrawingSession canvasDrawingSession = canvasCommandList.CreateDrawingSession())
				{
					canvasDrawingSession.FillGeometry(canvasGeometry, Colors.Black);
				}
				using ShadowEffect image = new ShadowEffect
				{
					Source = canvasCommandList,
					BlurAmount = (float)(shadow.Radius * 0.5),
					ShadowColor = shadow.Color,
					Optimization = EffectOptimization.Quality
				};
				ds.DrawImage(image, (float)shadow.X, (float)shadow.Y);
			}
			using (ds.CreateLayer(1f, canvasGeometry))
			{
				using CanvasCommandList canvasCommandList2 = new CanvasCommandList(device);
				using (CanvasDrawingSession canvasDrawingSession2 = canvasCommandList2.CreateDrawingSession())
				{
					canvasDrawingSession2.FillRectangle(rect, Fill.Brush(device, rect));
				}
				double? grainOpacity = GrainOpacity;
				if (grainOpacity.HasValue)
				{
					double valueOrDefault = grainOpacity.GetValueOrDefault();
					if (valueOrDefault > 0.0)
					{
						using (ICanvasImage foreground = ClickyPaperTextures.GrainImage(device, rect, valueOrDefault))
						{
							using BlendEffect image2 = new BlendEffect
							{
								Background = canvasCommandList2,
								Foreground = foreground,
								Mode = BlendEffectMode.Multiply
							};
							ds.DrawImage(image2);
						}
						goto IL_0233;
					}
				}
				ds.DrawImage(canvasCommandList2);
				goto IL_0233;
				IL_0233:
				foreach (PaperWash item in Washes.Where((PaperWash x) => x.Clipped))
				{
					DrawWash(ds, item, rect);
				}
			}
			foreach (PaperWash item2 in Washes.Where((PaperWash x) => !x.Clipped))
			{
				DrawWash(ds, item2, rect);
			}
			PaperStroke o = Outline;
			if ((object)o == null)
			{
				return;
			}
			using CanvasStrokeStyle strokeStyle = new CanvasStrokeStyle
			{
				StartCap = Cap(o.Cap),
				EndCap = Cap(o.Cap),
				DashCap = Cap(o.Cap),
				LineJoin = Join(o.Join),
				CustomDashStyle = o.Dash?.Select((double d) => (float)(d / Math.Max(o.Width, 0.0001))).ToArray()
			};
			if (OutlineIsBorder)
			{
				double num = o.Width / 2.0;
				using CanvasGeometry geometry = shape.PathIn(new Rect(num, num, Math.Max(0.0, actualWidth - 2.0 * num), Math.Max(0.0, actualHeight - 2.0 * num))).ToCanvasGeometry(device);
				ds.DrawGeometry(geometry, o.Color, (float)o.Width, strokeStyle);
				return;
			}
			ds.DrawGeometry(canvasGeometry, o.Color, (float)o.Width, strokeStyle);
		}
	}

	private static void DrawWash(CanvasDrawingSession ds, PaperWash wash, Rect sheet)
	{
		Rect rect = wash.Frame?.Invoke(new Size(sheet.Width, sheet.Height)) ?? sheet;
		Func<Size, PaperPath> path = wash.Path;
		if (path != null)
		{
			using (CanvasGeometry geometry = path(new Size(sheet.Width, sheet.Height)).ToCanvasGeometry(ds.Device))
			{
				ds.FillGeometry(geometry, wash.Fill.Brush(ds.Device, rect));
				return;
			}
		}
		ds.FillRectangle(rect, wash.Fill.Brush(ds.Device, rect));
	}

	private static CanvasCapStyle Cap(PenLineCap cap)
	{
		return cap switch
		{
			PenLineCap.Round => CanvasCapStyle.Round, 
			PenLineCap.Square => CanvasCapStyle.Square, 
			PenLineCap.Triangle => CanvasCapStyle.Triangle, 
			_ => CanvasCapStyle.Flat, 
		};
	}

	private static CanvasLineJoin Join(PenLineJoin join)
	{
		return join switch
		{
			PenLineJoin.Round => CanvasLineJoin.Round, 
			PenLineJoin.Bevel => CanvasLineJoin.Bevel, 
			_ => CanvasLineJoin.Miter, 
		};
	}
}
