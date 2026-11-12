using System;
using System.Linq;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Windows.UI;

namespace HeyClicky.App.Notch.Surfaces;

public sealed class ComputerMark : Grid
{
	private const float ViewW = 48f;

	private const float ViewH = 31f;

	private const float MarkWidth = 24f;

	private const float StrokeWeight = 1.25f;

	private const float Cycle = 6.6f;

	private const float LookStart = 0.6f;

	private const float LookPeak = 1.15f;

	private const float SettleStart = 1.6f;

	private const float SettleEnd = 2.1f;

	private const float BlinkShut = 0.09f;

	private const float BlinkGap = 0.18f;

	private const float DotRise = 9.5f;

	private const float LookDegrees = -4.5f;

	private static readonly (float X, float Y)[] Zig = new(float, float)[3]
	{
		(9.5f, 12.4f),
		(14.5f, 6.5f),
		(24.3f, 23.3f)
	};

	private static readonly (float X, float Y)[] Eye = new(float, float)[3]
	{
		(27.1f, 12.4f),
		(32.86f, 6.5f),
		(38.4f, 12.4f)
	};

	private static readonly (float X, float Y)[] Mouth = new(float, float)[2]
	{
		(24.3f, 23.3f),
		(33.4f, 23.3f)
	};

	private static readonly (float X, float Y)[] ShutZig = new(float, float)[3]
	{
		(9.5f, 9.5f),
		(14.5f, 9.5f),
		(24.3f, 23.3f)
	};

	private static readonly (float X, float Y)[] ShutEye = new(float, float)[2]
	{
		(27.1f, 9.5f),
		(38.4f, 9.5f)
	};

	private static readonly (float X, float Y) Pivot = (X: 24.3f, Y: 23.3f);

	private static readonly Color Coral = DS.Rgb(0.85, 0.33, 0.24);

	private const string RaisedHandPath = "M8,12 V5.6 a1.3,1.3 0 0 1 2.6,0 V10.5 V3.9 a1.3,1.3 0 0 1 2.6,0 V10.5 V4.6 a1.3,1.3 0 0 1 2.6,0 V11 V7.4 a1.3,1.3 0 0 1 2.6,0 V14.2 C18.4,18.4 15.9,21.5 12,21.5 C9.4,21.5 7.8,20.2 6.4,18 L3.9,13.9 a1.3,1.3 0 0 1 2.1,-1.5 Z";

	private readonly Grid markHost = new Grid
	{
		Width = 24.0,
		Height = 15.5,
		HorizontalAlignment = HorizontalAlignment.Center,
		VerticalAlignment = VerticalAlignment.Center
	};

	private readonly Grid attentionHost = new Grid
	{
		Visibility = Visibility.Collapsed
	};

	private bool? builtLooping;

	private bool working;

	private string? attention;

	private static float MarkHeight => 15.5f;

	private static float ViewScale => 0.5f;

	private static float BaseLineWidth => 1.875f;

	public ComputerMark()
	{
		base.Width = 27.0;
		base.Height = 24.0;
		base.IsHitTestVisible = false;
		base.Children.Add(markHost);
		base.Children.Add(attentionHost);
		BuildStrokes();
	}

	public void Set(bool isWorking, string? attention)
	{
		if (this.attention != attention)
		{
			this.attention = attention;
			attentionHost.Children.Clear();
			attentionHost.Visibility = ((attention == null) ? Visibility.Collapsed : Visibility.Visible);
			markHost.Visibility = ((attention != null) ? Visibility.Collapsed : Visibility.Visible);
			if (attention != null)
			{
				FrameworkElement item = Symbol(Colors.White);
				FrameworkElement coral = Symbol(Coral);
				attentionHost.Children.Add(item);
				attentionHost.Children.Add(coral);
				coral.Loaded += delegate
				{
					if (Anim.ReduceMotion)
					{
						coral.Opacity = 0.0;
					}
					else
					{
						Anim.LoopScalar(coral, "Opacity", 1.8, (double x) => (float)(0.5 - 0.5 * Math.Cos(Math.PI * 2.0 * x / 1.8)), 36);
					}
				};
			}
		}
		if (working != isWorking)
		{
			working = isWorking;
			BuildStrokes();
		}
		FrameworkElement Symbol(Color c)
		{
			if (!(attention == "hand"))
			{
				TextBlock textBlock = Icons.Glyph("\ue7ba", 14.0, c);
				textBlock.FontWeight = FontWeights.Bold;
				return textBlock;
			}
			return Icons.PathIcon("M8,12 V5.6 a1.3,1.3 0 0 1 2.6,0 V10.5 V3.9 a1.3,1.3 0 0 1 2.6,0 V10.5 V4.6 a1.3,1.3 0 0 1 2.6,0 V11 V7.4 a1.3,1.3 0 0 1 2.6,0 V14.2 C18.4,18.4 15.9,21.5 12,21.5 C9.4,21.5 7.8,20.2 6.4,18 L3.9,13.9 a1.3,1.3 0 0 1 2.1,-1.5 Z", 15.0, c);
		}
	}

	private void BuildStrokes()
	{
		bool flag = working && !Anim.ReduceMotion && !Anim.Instant;
		if (builtLooping == flag)
		{
			return;
		}
		builtLooping = flag;
		Compositor c = Ui.Compositor;
		Vector2 size = new Vector2(24f, MarkHeight);
		ContainerVisual containerVisual = c.CreateContainerVisual();
		containerVisual.Size = size;
		ContainerVisual containerVisual2 = c.CreateContainerVisual();
		containerVisual2.Size = size;
		containerVisual2.CenterPoint = new Vector3(Pivot.X * ViewScale, Pivot.Y * ViewScale, 0f);
		(ShapeVisual Visual, CompositionSpriteShape Shape, CompositionPathGeometry Path) tuple = Stroke(c, Mouth);
		ShapeVisual item = tuple.Visual;
		CompositionSpriteShape item2 = tuple.Shape;
		CompositionPathGeometry item3 = tuple.Path;
		(ShapeVisual Visual, CompositionSpriteShape Shape, CompositionPathGeometry Path) tuple2 = Stroke(c, Zig);
		ShapeVisual item4 = tuple2.Visual;
		CompositionSpriteShape item5 = tuple2.Shape;
		CompositionPathGeometry item6 = tuple2.Path;
		(ShapeVisual Visual, CompositionSpriteShape Shape, CompositionPathGeometry Path) tuple3 = Stroke(c, Eye);
		ShapeVisual item7 = tuple3.Visual;
		CompositionSpriteShape item8 = tuple3.Shape;
		CompositionPathGeometry item9 = tuple3.Path;
		ShapeVisual item10 = Stroke(c, ShutZig).Visual;
		ShapeVisual item11 = Stroke(c, ShutEye).Visual;
		float opacity = (item11.Opacity = 0f);
		item10.Opacity = opacity;
		containerVisual.Children.InsertAtTop(item);
		containerVisual.Children.InsertAtTop(containerVisual2);
		ShapeVisual[] array = new ShapeVisual[4] { item4, item7, item10, item11 };
		foreach (ShapeVisual newChild in array)
		{
			containerVisual2.Children.InsertAtTop(newChild);
		}
		LinearEasingFunction linear;
		CubicBezierEasingFunction lookOut;
		CubicBezierEasingFunction easeInOut;
		StepEasingFunction step;
		float firstBlink;
		float secondBlink;
		(float T, float L, float M, float R, bool Dots)[] timeline;
		if (flag)
		{
			linear = c.CreateLinearEasingFunction();
			lookOut = c.CreateCubicBezierEasingFunction(new Vector2(0.4f, 0f), new Vector2(0.6f, 1.2f));
			easeInOut = c.CreateCubicBezierEasingFunction(new Vector2(0.42f, 0f), new Vector2(0.58f, 1f));
			containerVisual2.StartAnimation("RotationAngleInDegrees", Look(-4.5f));
			ScalarKeyFrameAnimation animation = Look(-0.71999997f);
			item7.StartAnimation("Offset.X", animation);
			item11.StartAnimation("Offset.X", animation);
			step = c.CreateStepEasingFunction();
			step.IsFinalStepSingleFrame = true;
			firstBlink = 2.35f;
			secondBlink = firstBlink + 0.09f + 0.18f;
			item4.StartAnimation("Opacity", Blink(shutTwin: false));
			item7.StartAnimation("Opacity", Blink(shutTwin: false));
			item10.StartAnimation("Opacity", Blink(shutTwin: true));
			item11.StartAnimation("Opacity", Blink(shutTwin: true));
			timeline = new(float, float, float, float, bool)[13]
			{
				(0f, 16f, 16f, 16f, false),
				(3f, 16f, 16f, 16f, false),
				(3.35f, 16f, 16f, 16f, true),
				(3.6f, 6.5f, 16f, 16f, true),
				(3.85f, 16f, 6.5f, 16f, true),
				(4.1f, 16f, 16f, 6.5f, true),
				(4.35f, 16f, 16f, 16f, true),
				(4.6f, 6.5f, 16f, 16f, true),
				(4.85f, 16f, 6.5f, 16f, true),
				(5.1f, 16f, 16f, 6.5f, true),
				(5.35f, 16f, 16f, 16f, true),
				(5.8f, 16f, 16f, 16f, false),
				(6.6f, 16f, 16f, 16f, false)
			};
			Morph(item6, item5, Zig, ((float L, float M, float R) d) => (X: 8f, Y: d.L));
			Morph(item9, item8, Eye, ((float L, float M, float R) d) => (X: 40f, Y: d.R));
			Morph(item3, item2, Mouth, ((float L, float M, float R) d) => (X: 24f, Y: d.M));
		}
		ElementCompositionPreview.SetElementChildVisual(markHost, containerVisual);
		ScalarKeyFrameAnimation Blink(bool shutTwin)
		{
			ScalarKeyFrameAnimation scalarKeyFrameAnimation2 = c.CreateScalarKeyFrameAnimation();
			float value = ((!shutTwin) ? 1 : 0);
			float value2 = (shutTwin ? 1 : 0);
			scalarKeyFrameAnimation2.InsertKeyFrame(0f, value, step);
			scalarKeyFrameAnimation2.InsertKeyFrame(firstBlink / 6.6f, value2, step);
			scalarKeyFrameAnimation2.InsertKeyFrame((firstBlink + 0.09f) / 6.6f, value, step);
			scalarKeyFrameAnimation2.InsertKeyFrame(secondBlink / 6.6f, value2, step);
			scalarKeyFrameAnimation2.InsertKeyFrame((secondBlink + 0.09f) / 6.6f, value, step);
			scalarKeyFrameAnimation2.InsertKeyFrame(1f, value, step);
			return Loop(scalarKeyFrameAnimation2);
		}
		ScalarKeyFrameAnimation Look(float peak)
		{
			ScalarKeyFrameAnimation scalarKeyFrameAnimation = c.CreateScalarKeyFrameAnimation();
			scalarKeyFrameAnimation.InsertKeyFrame(0f, 0f, linear);
			scalarKeyFrameAnimation.InsertKeyFrame(1f / 11f, 0f, linear);
			scalarKeyFrameAnimation.InsertKeyFrame(23f / 132f, peak, lookOut);
			scalarKeyFrameAnimation.InsertKeyFrame(8f / 33f, peak, linear);
			scalarKeyFrameAnimation.InsertKeyFrame(7f / 22f, 0f, easeInOut);
			scalarKeyFrameAnimation.InsertKeyFrame(1f, 0f, linear);
			return Loop(scalarKeyFrameAnimation);
		}
		void Morph(CompositionPathGeometry path, CompositionSpriteShape shape, (float X, float Y)[] mark, Func<(float L, float M, float R), (float X, float Y)> dot)
		{
			PathKeyFrameAnimation pathKeyFrameAnimation = c.CreatePathKeyFrameAnimation();
			ScalarKeyFrameAnimation scalarKeyFrameAnimation3 = c.CreateScalarKeyFrameAnimation();
			for (int j = 0; j < timeline.Length; j++)
			{
				(float, float, float, float, bool) tuple4 = timeline[j];
				float normalizedProgressKey = tuple4.Item1 / 6.6f;
				(float, float)[] points = (tuple4.Item5 ? Enumerable.Repeat(dot((tuple4.Item2, tuple4.Item3, tuple4.Item4)), mark.Length).ToArray() : mark);
				CompositionEasingFunction easingFunction = ((j == 0) ? ((CompositionEasingFunction)linear) : ((CompositionEasingFunction)easeInOut));
				pathKeyFrameAnimation.InsertKeyFrame(normalizedProgressKey, PathOf(points), easingFunction);
				scalarKeyFrameAnimation3.InsertKeyFrame(normalizedProgressKey, BaseLineWidth * (tuple4.Item5 ? 2.3f : 1f), easingFunction);
			}
			path.StartAnimation("Path", Loop(pathKeyFrameAnimation));
			shape.StartAnimation("StrokeThickness", Loop(scalarKeyFrameAnimation3));
		}
	}

	private static T Loop<T>(T animation) where T : KeyFrameAnimation
	{
		animation.Duration = TimeSpan.FromSeconds(6.599999904632568);
		animation.IterationBehavior = AnimationIterationBehavior.Forever;
		return animation;
	}

	private static (ShapeVisual Visual, CompositionSpriteShape Shape, CompositionPathGeometry Path) Stroke(Compositor c, (float X, float Y)[] points)
	{
		CompositionPathGeometry compositionPathGeometry = c.CreatePathGeometry(PathOf(points));
		CompositionSpriteShape compositionSpriteShape = c.CreateSpriteShape(compositionPathGeometry);
		compositionSpriteShape.StrokeBrush = c.CreateColorBrush(Colors.White);
		compositionSpriteShape.StrokeThickness = BaseLineWidth;
		CompositionStrokeCap strokeStartCap = (compositionSpriteShape.StrokeEndCap = CompositionStrokeCap.Round);
		compositionSpriteShape.StrokeStartCap = strokeStartCap;
		compositionSpriteShape.StrokeLineJoin = CompositionStrokeLineJoin.Round;
		ShapeVisual shapeVisual = c.CreateShapeVisual();
		shapeVisual.Size = new Vector2(24f, MarkHeight);
		shapeVisual.Shapes.Add(compositionSpriteShape);
		return (Visual: shapeVisual, Shape: compositionSpriteShape, Path: compositionPathGeometry);
	}

	private static CompositionPath PathOf((float X, float Y)[] points)
	{
		CanvasPathBuilder canvasPathBuilder = new CanvasPathBuilder(CanvasDevice.GetSharedDevice());
		canvasPathBuilder.BeginFigure(points[0].X * ViewScale, points[0].Y * ViewScale);
		for (int i = 1; i < points.Length; i++)
		{
			canvasPathBuilder.AddLine(points[i].X * ViewScale, points[i].Y * ViewScale);
		}
		canvasPathBuilder.EndFigure(CanvasFigureLoop.Open);
		return new CompositionPath(CanvasGeometry.CreatePath(canvasPathBuilder));
	}
}
