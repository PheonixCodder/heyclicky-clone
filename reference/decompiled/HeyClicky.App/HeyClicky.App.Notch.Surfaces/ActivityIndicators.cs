using System;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace HeyClicky.App.Notch.Surfaces;

public static class ActivityIndicators
{
	private static CompositionPropertySet Clock(Compositor c, double period = 3600.0)
	{
		CompositionPropertySet compositionPropertySet = c.CreatePropertySet();
		compositionPropertySet.InsertScalar("T", 0f);
		compositionPropertySet.InsertScalar("Level", 0.15f);
		ScalarKeyFrameAnimation scalarKeyFrameAnimation = c.CreateScalarKeyFrameAnimation();
		scalarKeyFrameAnimation.InsertKeyFrame(0f, 0f);
		scalarKeyFrameAnimation.InsertKeyFrame(1f, (float)period, c.CreateLinearEasingFunction());
		scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(period);
		scalarKeyFrameAnimation.IterationBehavior = AnimationIterationBehavior.Forever;
		compositionPropertySet.StartAnimation("T", scalarKeyFrameAnimation);
		return compositionPropertySet;
	}

	private static void Bind(UIElement e, string target, string expr, CompositionPropertySet clock, Compositor c)
	{
		ExpressionAnimation expressionAnimation = c.CreateExpressionAnimation(expr);
		expressionAnimation.SetReferenceParameter("k", clock);
		expressionAnimation.Target = target;
		e.StartAnimation(expressionAnimation);
	}

	public static (FrameworkElement view, CompositionPropertySet clock) ListeningBars(Color tint)
	{
		Compositor compositor = Ui.Compositor;
		CompositionPropertySet compositionPropertySet = Clock(compositor);
		return (view: Bars(compositor, compositionPropertySet, tint, (int i) => FormattableString.Invariant($"Max(3, (Sin(k.T * 3.6 + {(double)i * 0.85:0.##}) * 0.5 + 0.5) * 12 * Clamp(k.Level, 0.15, 1) + 3)"), (int i) => FormattableString.Invariant($"0.7 + 0.3 * (Sin(k.T * 3.6 + {(double)i * 0.85:0.##}) * 0.5 + 0.5)")), clock: compositionPropertySet);
	}

	private static FrameworkElement Bars(Compositor c, CompositionPropertySet clock, Color tint, Func<int, string> height, Func<int, string>? opacity = null)
	{
		Grid grid = new Grid
		{
			Width = 14.0,
			Height = 14.0,
			VerticalAlignment = VerticalAlignment.Center,
			IsHitTestVisible = false
		};
		ContainerVisual containerVisual = c.CreateContainerVisual();
		containerVisual.Size = new Vector2(14f, 14f);
		CompositionColorBrush fillBrush = c.CreateColorBrush(tint);
		for (int i = 0; i < 4; i++)
		{
			ShapeVisual shapeVisual = c.CreateShapeVisual();
			shapeVisual.Size = new Vector2(14f, 14f);
			CompositionRoundedRectangleGeometry compositionRoundedRectangleGeometry = c.CreateRoundedRectangleGeometry();
			compositionRoundedRectangleGeometry.CornerRadius = new Vector2(1f, 1f);
			ExpressionAnimation expressionAnimation = c.CreateExpressionAnimation("Vector2(2, " + height(i) + ")");
			expressionAnimation.SetReferenceParameter("k", clock);
			compositionRoundedRectangleGeometry.StartAnimation("Size", expressionAnimation);
			ExpressionAnimation expressionAnimation2 = c.CreateExpressionAnimation($"Vector2({i * 4}, 7 - ({height(i)}) / 2)");
			expressionAnimation2.SetReferenceParameter("k", clock);
			compositionRoundedRectangleGeometry.StartAnimation("Offset", expressionAnimation2);
			CompositionSpriteShape compositionSpriteShape = c.CreateSpriteShape(compositionRoundedRectangleGeometry);
			compositionSpriteShape.FillBrush = fillBrush;
			shapeVisual.Shapes.Add(compositionSpriteShape);
			if (opacity != null)
			{
				ExpressionAnimation expressionAnimation3 = c.CreateExpressionAnimation(opacity(i));
				expressionAnimation3.SetReferenceParameter("k", clock);
				shapeVisual.StartAnimation("Opacity", expressionAnimation3);
			}
			containerVisual.Children.InsertAtTop(shapeVisual);
		}
		ElementCompositionPreview.SetElementChildVisual(grid, containerVisual);
		return grid;
	}

	public static FrameworkElement ThinkingDots(Color tint, double diameter = 4.0, double spacing = 3.0, double freq = 2.8, double phaseStep = 0.85, double scaleBase = 0.55, double scaleAmp = 0.45, double opBase = 0.4, double opAmp = 0.6, double height = 14.0)
	{
		Compositor compositor = Ui.Compositor;
		CompositionPropertySet clock = Clock(compositor);
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = spacing,
			Height = height,
			VerticalAlignment = VerticalAlignment.Center
		};
		for (int i = 0; i < 3; i++)
		{
			Grid grid = new Grid
			{
				Width = diameter,
				Height = diameter,
				VerticalAlignment = VerticalAlignment.Center
			};
			Grid grid2 = Ui.GlowSpot(tint, 0.55, diameter + 6.0, 0.3).Pad(-3.0);
			Ellipse ellipse = new Ellipse
			{
				Width = diameter,
				Height = diameter,
				Fill = Ui.Brush(tint)
			};
			grid.Children.Add(grid2);
			grid.Children.Add(ellipse);
			ellipse.CenterPoint = new Vector3((float)diameter / 2f, (float)diameter / 2f, 0f);
			string text = FormattableString.Invariant($"((Sin(k.T * {freq:0.##} + {(double)i * phaseStep:0.##}) + 1) * 0.5)");
			string expr = FormattableString.Invariant($"({opBase:0.##} + {opAmp:0.##} * {text})");
			Bind(ellipse, "Scale", FormattableString.Invariant($"Vector3({scaleBase:0.##} + {scaleAmp:0.##} * {text}, {scaleBase:0.##} + {scaleAmp:0.##} * {text}, 1)"), clock, compositor);
			Bind(grid, "Opacity", expr, clock, compositor);
			Bind(grid2, "Opacity", expr, clock, compositor);
			stackPanel.Children.Add(grid);
		}
		return stackPanel;
	}

	public static FrameworkElement AgentDots(Color tint)
	{
		return ThinkingDots(tint, 5.0, 4.0, 3.4, 1.05, 0.6, 0.6, 0.3, 0.7, 18.0);
	}

	public static FrameworkElement SpeakingBars(Color tint)
	{
		Compositor compositor = Ui.Compositor;
		return Bars(compositor, Clock(compositor), tint, (int i) => FormattableString.Invariant($"Max(3, (Sin(k.T * 4.2 + {(double)i * 1.7:0.##}) * 0.5 + 0.5) * 11 + 3)"));
	}

	public static FrameworkElement DictationDashes(Color tint, bool recording)
	{
		Compositor compositor = Ui.Compositor;
		CompositionPropertySet clock = Clock(compositor);
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 2.5,
			Height = 14.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		for (int i = 0; i < 4; i++)
		{
			Rectangle rectangle = new Rectangle
			{
				Width = 5.0,
				Height = 2.5,
				RadiusX = 1.0,
				RadiusY = 1.0,
				Fill = Ui.Brush(tint),
				VerticalAlignment = VerticalAlignment.Center
			};
			rectangle.CenterPoint = new Vector3(0f, 1.25f, 0f);
			if (recording)
			{
				rectangle.Opacity = 0.9;
				if (i > 0)
				{
					float num = (float)i / 4f;
					float num2 = 0.099999994f;
					ScalarKeyFrameAnimation scalarKeyFrameAnimation = compositor.CreateScalarKeyFrameAnimation();
					LinearEasingFunction easingFunction = compositor.CreateLinearEasingFunction();
					CubicBezierEasingFunction easingFunction2 = Easing.EaseOut(compositor);
					float normalizedProgressKey = Math.Min(1f, num + num2);
					scalarKeyFrameAnimation.InsertKeyFrame(0f, 0.9f);
					scalarKeyFrameAnimation.InsertKeyFrame(num2, 0f, easingFunction2);
					scalarKeyFrameAnimation.InsertKeyFrame(num, 0f, easingFunction);
					scalarKeyFrameAnimation.InsertKeyFrame(normalizedProgressKey, 0.9f, easingFunction2);
					scalarKeyFrameAnimation.InsertKeyFrame(1f, 0.9f, easingFunction);
					scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(1.6);
					scalarKeyFrameAnimation.IterationBehavior = AnimationIterationBehavior.Forever;
					scalarKeyFrameAnimation.Target = "Opacity";
					rectangle.StartAnimation(scalarKeyFrameAnimation);
					Vector3KeyFrameAnimation vector3KeyFrameAnimation = compositor.CreateVector3KeyFrameAnimation();
					vector3KeyFrameAnimation.InsertKeyFrame(0f, Vector3.One);
					vector3KeyFrameAnimation.InsertKeyFrame(num2, new Vector3(0.4f, 0.4f, 1f), easingFunction2);
					vector3KeyFrameAnimation.InsertKeyFrame(num, new Vector3(0.4f, 0.4f, 1f), easingFunction);
					vector3KeyFrameAnimation.InsertKeyFrame(normalizedProgressKey, Vector3.One, easingFunction2);
					vector3KeyFrameAnimation.InsertKeyFrame(1f, Vector3.One, easingFunction);
					vector3KeyFrameAnimation.Duration = TimeSpan.FromSeconds(1.6);
					vector3KeyFrameAnimation.IterationBehavior = AnimationIterationBehavior.Forever;
					vector3KeyFrameAnimation.Target = "Scale";
					rectangle.StartAnimation(vector3KeyFrameAnimation);
				}
			}
			else
			{
				string text = FormattableString.Invariant($"(Sin(k.T * 3.6 - {(double)i * Math.PI / 2.0:0.###}) * 0.5 + 0.5)");
				Bind(rectangle, "Opacity", "0.25 + 0.75 * " + text, clock, compositor);
				rectangle.CenterPoint = new Vector3(2.5f, 1.25f, 0f);
				Bind(rectangle, "Scale", "Vector3(1, 0.85 + 0.45 * " + text + ", 1)", clock, compositor);
			}
			stackPanel.Children.Add(rectangle);
		}
		return stackPanel;
	}

	public static FrameworkElement DockedCursor(Color theme)
	{
		Compositor compositor = Ui.Compositor;
		CompositionPropertySet compositionObject = Clock(compositor);
		Grid grid = new Grid
		{
			Width = 14.0,
			Height = 14.0,
			VerticalAlignment = VerticalAlignment.Center,
			RenderTransform = new RotateTransform
			{
				Angle = -35.0,
				CenterX = 7.0,
				CenterY = 7.0
			}
		};
		Path path = IntroMark.Cursor(14.0, theme, 0.0);
		Grid grid2 = new Grid
		{
			Width = 14.0,
			Height = 14.0,
			IsHitTestVisible = false
		};
		DropShadow dropShadow = compositor.CreateDropShadow();
		dropShadow.Color = theme;
		dropShadow.Opacity = 0.85f;
		dropShadow.Mask = path.GetAlphaMask();
		ExpressionAnimation expressionAnimation = compositor.CreateExpressionAnimation("2 * (3 + 3 * ((Sin(k.T * 2.4) + 1) * 0.5))");
		expressionAnimation.SetReferenceParameter("k", compositionObject);
		dropShadow.StartAnimation("BlurRadius", expressionAnimation);
		SpriteVisual spriteVisual = compositor.CreateSpriteVisual();
		spriteVisual.Size = new Vector2(14f, 14f);
		spriteVisual.Shadow = dropShadow;
		ElementCompositionPreview.SetElementChildVisual(grid2, spriteVisual);
		grid.Children.Add(grid2);
		grid.Children.Add(path);
		return grid;
	}
}
