using System;
using System.Linq;
using System.Numerics;
using HeyClicky.App.Design;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.UI;

public sealed class BrandMark : Grid
{
	private const double ViewW = 48.0;

	private const double ViewH = 31.0;

	private const double LookStart = 1.9;

	private const double LookDuration = 0.55;

	private const double SettleStart = 3.2;

	private const double SettleDuration = 0.5;

	private const double FirstBlink = 4.2;

	private const double BlinkShut = 0.09;

	private const double BlinkGap = 0.18;

	private const double Cycle = 6.16;

	private static readonly Vector2 Pivot = new Vector2(24.3f, 23.3f);

	private readonly double markWidth;

	private readonly Canvas upper;

	private readonly Polyline openZig;

	private readonly Polyline openRightEye;

	private readonly Polyline shutZig;

	private readonly Polyline shutRightEye;

	private bool started;

	public BrandMark(double markWidth, Color stroke, double strokeWeight = 1.0)
	{
		BrandMark brandMark = this;
		this.markWidth = markWidth;
		double sx = markWidth / 48.0;
		double num = markWidth * 31.0 / 48.0;
		double sy = num / 31.0;
		base.Width = markWidth;
		base.Height = num;
		base.IsHitTestVisible = false;
		double lineWidth = markWidth * 3.0 / 48.0 * strokeWeight;
		Polyline item = Line(new(double, double)[2]
		{
			(24.3, 23.3),
			(33.4, 23.3)
		});
		openZig = Line(new(double, double)[3]
		{
			(9.5, 12.4),
			(14.5, 6.5),
			(24.3, 23.3)
		});
		openRightEye = Line(new(double, double)[3]
		{
			(27.1, 12.4),
			(32.86, 6.5),
			(38.4, 12.4)
		});
		shutZig = Line(new(double, double)[3]
		{
			(9.5, 9.5),
			(14.5, 9.5),
			(24.3, 23.3)
		});
		shutRightEye = Line(new(double, double)[2]
		{
			(27.1, 9.5),
			(38.4, 9.5)
		});
		ElementCompositionPreview.GetElementVisual(shutZig).Opacity = 0f;
		ElementCompositionPreview.GetElementVisual(shutRightEye).Opacity = 0f;
		Canvas canvas = new Canvas
		{
			Width = markWidth,
			Height = num
		};
		canvas.Children.Add(item);
		upper = new Canvas
		{
			Width = markWidth,
			Height = num
		};
		upper.Children.Add(openZig);
		upper.Children.Add(shutZig);
		Canvas rightEye = new Canvas
		{
			Width = markWidth,
			Height = num
		};
		rightEye.Children.Add(openRightEye);
		rightEye.Children.Add(shutRightEye);
		upper.Children.Add(rightEye);
		base.Children.Add(canvas);
		base.Children.Add(upper);
		base.Loaded += delegate
		{
			brandMark.Start(rightEye, (float)((double)Pivot.X * sx), (float)((double)Pivot.Y * sy));
		};
		Polyline Line((double x, double y)[] points)
		{
			Polyline polyline = new Polyline
			{
				Stroke = Ui.Brush(stroke),
				StrokeThickness = lineWidth,
				StrokeLineJoin = PenLineJoin.Round,
				StrokeStartLineCap = PenLineCap.Round,
				StrokeEndLineCap = PenLineCap.Round
			};
			for (int i = 0; i < points.Length; i++)
			{
				var (num2, num3) = points[i];
				polyline.Points.Add(new Point(num2 * sx, num3 * sy));
			}
			return polyline;
		}
	}

	public void SetStroke(Color stroke)
	{
		SolidColorBrush stroke2 = Ui.Brush(stroke);
		Polyline[] array = new Polyline[4] { openZig, openRightEye, shutZig, shutRightEye };
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Stroke = stroke2;
		}
		foreach (Canvas item in base.Children.OfType<Canvas>())
		{
			foreach (Polyline item2 in item.Children.OfType<Polyline>())
			{
				item2.Stroke = stroke2;
			}
		}
	}

	public void BlinkOnce()
	{
		double duration;
		if (!Anim.Instant && !Anim.ReduceMotion && (object)base.XamlRoot != null)
		{
			Compositor compositor = Ui.Compositor;
			StepEasingFunction stepEasingFunction = compositor.CreateStepEasingFunction();
			stepEasingFunction.IsFinalStepSingleFrame = false;
			duration = 0.36;
			CompositionScopedBatch compositionScopedBatch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
			(UIElement, bool)[] array = new(UIElement, bool)[4]
			{
				(openZig, false),
				(openRightEye, false),
				(shutZig, true),
				(shutRightEye, true)
			};
			for (int i = 0; i < array.Length; i++)
			{
				(UIElement, bool) tuple = array[i];
				UIElement item = tuple.Item1;
				bool item2 = tuple.Item2;
				ScalarKeyFrameAnimation scalarKeyFrameAnimation = compositor.CreateScalarKeyFrameAnimation();
				scalarKeyFrameAnimation.InsertKeyFrame(0f, item2 ? 1 : 0, stepEasingFunction);
				scalarKeyFrameAnimation.InsertKeyFrame(K(0.09), (!item2) ? 1 : 0, stepEasingFunction);
				scalarKeyFrameAnimation.InsertKeyFrame(K(0.27), item2 ? 1 : 0, stepEasingFunction);
				scalarKeyFrameAnimation.InsertKeyFrame(1f, (!item2) ? 1 : 0, stepEasingFunction);
				scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(duration);
				ElementCompositionPreview.GetElementVisual(item).StartAnimation("Opacity", scalarKeyFrameAnimation);
			}
			compositionScopedBatch.End();
			compositionScopedBatch.Completed += delegate
			{
				base.DispatcherQueue.TryEnqueue(RestartEyeCycle);
			};
		}
		float K(double seconds)
		{
			return (float)(seconds / duration);
		}
	}

	private void RestartEyeCycle()
	{
		if (started && upper.Children.Count >= 3)
		{
			UIElement uIElement = upper.Children[2];
			if ((object)uIElement != null)
			{
				started = false;
				double num = markWidth / 48.0;
				double num2 = markWidth * 31.0 / 48.0 / 31.0;
				Start(uIElement, (float)((double)Pivot.X * num), (float)((double)Pivot.Y * num2));
			}
		}
	}

	private void Start(UIElement rightEye, float pivotX, float pivotY)
	{
		Compositor c;
		LinearEasingFunction linear;
		CubicBezierEasingFunction look;
		CubicBezierEasingFunction settle;
		StepEasingFunction step;
		double second;
		if (!started && !Anim.Instant)
		{
			started = true;
			c = Ui.Compositor;
			linear = c.CreateLinearEasingFunction();
			look = c.CreateCubicBezierEasingFunction(new Vector2(0.4f, 0f), new Vector2(0.6f, 1.2f));
			settle = Easing.EaseInOut(c);
			step = c.CreateStepEasingFunction();
			step.IsFinalStepSingleFrame = false;
			Visual elementVisual = ElementCompositionPreview.GetElementVisual(upper);
			elementVisual.CenterPoint = new Vector3(pivotX, pivotY, 0f);
			elementVisual.StartAnimation("RotationAngleInDegrees", Look(-4.5f));
			ScalarKeyFrameAnimation animation = Look((float)((0.0 - markWidth) * 0.03));
			ElementCompositionPreview.SetIsTranslationEnabled(rightEye, value: true);
			ElementCompositionPreview.GetElementVisual(rightEye).StartAnimation("Translation.X", animation);
			second = 4.47;
			(UIElement, bool)[] array = new(UIElement, bool)[4]
			{
				(openZig, false),
				(openRightEye, false),
				(shutZig, true),
				(shutRightEye, true)
			};
			for (int i = 0; i < array.Length; i++)
			{
				(UIElement, bool) tuple = array[i];
				UIElement item = tuple.Item1;
				bool item2 = tuple.Item2;
				Visual elementVisual2 = ElementCompositionPreview.GetElementVisual(item);
				elementVisual2.Opacity = ((!item2) ? 1 : 0);
				elementVisual2.StartAnimation("Opacity", Blink(item2));
			}
		}
		ScalarKeyFrameAnimation Blink(bool shutTwin)
		{
			float value = ((!shutTwin) ? 1 : 0);
			float value2 = (shutTwin ? 1 : 0);
			ScalarKeyFrameAnimation scalarKeyFrameAnimation2 = c.CreateScalarKeyFrameAnimation();
			scalarKeyFrameAnimation2.InsertKeyFrame(0f, value, step);
			scalarKeyFrameAnimation2.InsertKeyFrame(K(4.2), value2, step);
			scalarKeyFrameAnimation2.InsertKeyFrame(K(4.29), value, step);
			scalarKeyFrameAnimation2.InsertKeyFrame(K(second), value2, step);
			scalarKeyFrameAnimation2.InsertKeyFrame(K(second + 0.09), value, step);
			scalarKeyFrameAnimation2.InsertKeyFrame(1f, value, step);
			scalarKeyFrameAnimation2.Duration = TimeSpan.FromSeconds(6.16);
			scalarKeyFrameAnimation2.IterationBehavior = AnimationIterationBehavior.Forever;
			return scalarKeyFrameAnimation2;
		}
		static float K(double seconds)
		{
			return (float)(seconds / 6.16);
		}
		ScalarKeyFrameAnimation Look(float peak)
		{
			ScalarKeyFrameAnimation scalarKeyFrameAnimation = c.CreateScalarKeyFrameAnimation();
			scalarKeyFrameAnimation.InsertKeyFrame(0f, 0f, linear);
			scalarKeyFrameAnimation.InsertKeyFrame(K(1.9), 0f, linear);
			scalarKeyFrameAnimation.InsertKeyFrame(K(2.45), peak, look);
			scalarKeyFrameAnimation.InsertKeyFrame(K(3.2), peak, linear);
			scalarKeyFrameAnimation.InsertKeyFrame(K(3.7), 0f, settle);
			scalarKeyFrameAnimation.InsertKeyFrame(1f, 0f, linear);
			scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(6.16);
			scalarKeyFrameAnimation.IterationBehavior = AnimationIterationBehavior.Forever;
			return scalarKeyFrameAnimation;
		}
	}
}
