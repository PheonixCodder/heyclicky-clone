using System;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using Windows.UI;
using Windows.UI.Composition;

namespace HeyClicky.App.Notch;

public sealed class NotchWorkingSweep
{
	private const float LightLength = 30f;

	private const double CrossSeconds = 1.1;

	private const double RestSeconds = 0.5;

	private const double FirstRunDelaySeconds = 0.3;

	private const double TintAlpha = 0.26;

	private const double HairlineAlpha = 0.75;

	private static readonly Color Green = DS.Hex(1755979u);

	private static readonly Color Mint = DS.Hex(12124107u);

	private readonly Compositor c;

	private readonly CompositionPropertySet props;

	private readonly CompositionPropertySet run;

	private readonly CompositionSpriteShape hairline;

	private bool showing;

	public ContainerVisual Root { get; }

	public bool IsShowing => showing;

	public NotchWorkingSweep(Compositor compositor, NotchSystemShape shape)
	{
		c = compositor;
		props = shape.Props;
		run = c.CreatePropertySet();
		run.InsertScalar("Progress", 0f);
		Root = c.CreateContainerVisual();
		Root.RelativeSizeAdjustment = Vector2.One;
		Root.Clip = c.CreateGeometricClip(shape.ClipGeometry);
		Root.Opacity = 0f;
		ContainerVisual containerVisual = c.CreateContainerVisual();
		containerVisual.RelativeSizeAdjustment = Vector2.One;
		Bind(containerVisual, "Opacity", "p.Sliver * Clamp((16 - p.Height) / 8, 0, 1)");
		Root.Children.InsertAtTop(containerVisual);
		SpriteVisual spriteVisual = c.CreateSpriteVisual();
		spriteVisual.Brush = c.CreateColorBrush(Green.WithAlpha(0.26));
		Bind(spriteVisual, "Offset", "Vector3((p.EnvW - p.Width) / 2, p.Gap, 0)");
		Bind(spriteVisual, "Size", "Vector2(p.Width, p.Height)");
		containerVisual.Children.InsertAtTop(spriteVisual);
		ShapeVisual shapeVisual = c.CreateShapeVisual();
		shapeVisual.RelativeSizeAdjustment = Vector2.One;
		CompositionRoundedRectangleGeometry geometry = c.CreateRoundedRectangleGeometry();
		shape.BindHairline(geometry);
		hairline = c.CreateSpriteShape(geometry);
		hairline.StrokeBrush = c.CreateColorBrush(Green.WithAlpha(0.75));
		hairline.StrokeThickness = shape.Hairline;
		shapeVisual.Shapes.Add(hairline);
		containerVisual.Children.InsertAtTop(shapeVisual);
		shape.HairlineChanged += delegate(float px)
		{
			hairline.StrokeThickness = px;
		};
		CompositionLinearGradientBrush compositionLinearGradientBrush = c.CreateLinearGradientBrush();
		compositionLinearGradientBrush.StartPoint = new Vector2(0f, 0.5f);
		compositionLinearGradientBrush.EndPoint = new Vector2(1f, 0.5f);
		(float, Color)[] array = new(float, Color)[5]
		{
			(0f, Green.WithAlpha(0.0)),
			(0.28f, Green.WithAlpha(0.5)),
			(0.5f, Mint.WithAlpha(0.95)),
			(0.72f, Green.WithAlpha(0.5)),
			(1f, Green.WithAlpha(0.0))
		};
		for (int i = 0; i < array.Length; i++)
		{
			var (offset, color) = array[i];
			compositionLinearGradientBrush.ColorStops.Add(c.CreateColorGradientStop(offset, color));
		}
		SpriteVisual spriteVisual2 = c.CreateSpriteVisual();
		spriteVisual2.Brush = compositionLinearGradientBrush;
		ExpressionAnimation expressionAnimation = c.CreateExpressionAnimation(FormattableString.Invariant($"Vector3((p.EnvW - p.Width) / 2 - {30f} + (p.Width + {30f}) * r.Progress, p.Gap, 0)"));
		expressionAnimation.SetReferenceParameter("p", props);
		expressionAnimation.SetReferenceParameter("r", run);
		spriteVisual2.StartAnimation("Offset", expressionAnimation);
		Bind(spriteVisual2, "Size", FormattableString.Invariant($"Vector2({30f}, p.Height)"));
		containerVisual.Children.InsertAtTop(spriteVisual2);
	}

	public void Show(bool working)
	{
		if (working != showing)
		{
			showing = working;
			if (working)
			{
				StartRun();
			}
			Fade(working ? 1 : 0);
		}
	}

	private void StartRun()
	{
		if (!Anim.ReduceMotion && !Anim.Instant)
		{
			double num = 1.6;
			ScalarKeyFrameAnimation scalarKeyFrameAnimation = c.CreateScalarKeyFrameAnimation();
			scalarKeyFrameAnimation.InsertKeyFrame(0f, 0f);
			scalarKeyFrameAnimation.InsertKeyFrame((float)(1.1 / num), 1f, SystemEasing.EaseInOut(c));
			scalarKeyFrameAnimation.InsertKeyFrame(1f, 1f);
			scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(num);
			scalarKeyFrameAnimation.IterationBehavior = AnimationIterationBehavior.Forever;
			scalarKeyFrameAnimation.DelayTime = TimeSpan.FromSeconds(0.3);
			scalarKeyFrameAnimation.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
			run.StartAnimation("Progress", scalarKeyFrameAnimation);
		}
	}

	private void StopRun()
	{
		ScalarKeyFrameAnimation scalarKeyFrameAnimation = c.CreateScalarKeyFrameAnimation();
		scalarKeyFrameAnimation.InsertKeyFrame(1f, 0f);
		scalarKeyFrameAnimation.Duration = TimeSpan.FromMilliseconds(1L, 0L);
		run.StartAnimation("Progress", scalarKeyFrameAnimation);
	}

	private void Fade(float to)
	{
		if (Anim.Instant)
		{
			Root.StopAnimation("Opacity");
			Root.Opacity = to;
			if (to == 0f)
			{
				StopRun();
			}
			return;
		}
		CompositionScopedBatch compositionScopedBatch = c.CreateScopedBatch(CompositionBatchTypes.Animation);
		ScalarKeyFrameAnimation scalarKeyFrameAnimation = c.CreateScalarKeyFrameAnimation();
		scalarKeyFrameAnimation.InsertKeyFrame(1f, to, SystemEasing.Expressive(c));
		scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(0.36);
		Root.StartAnimation("Opacity", scalarKeyFrameAnimation);
		compositionScopedBatch.End();
		if (to != 0f)
		{
			return;
		}
		compositionScopedBatch.Completed += delegate
		{
			if (!showing)
			{
				StopRun();
			}
		};
	}

	private void Bind(CompositionObject target, string property, string expression)
	{
		ExpressionAnimation expressionAnimation = c.CreateExpressionAnimation(expression);
		expressionAnimation.SetReferenceParameter("p", props);
		target.StartAnimation(property, expressionAnimation);
	}
}
