using System;
using System.Numerics;
using HeyClicky.App.Design;
using Microsoft.UI.Composition;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Shapes;

namespace HeyClicky.App.UI;

public sealed class RingCloseButton : Pressable
{
	private readonly CompositionSpriteShape ring;

	private readonly CompositionEllipseGeometry ringGeo;

	public RingCloseButton(bool showRing)
	{
		base.Width = 24.0;
		base.Height = 24.0;
		AutomationProperties.SetName(this, "Close");
		Ellipse item = new Ellipse
		{
			Width = 25.8,
			Height = 25.8,
			Margin = new Thickness(-0.9),
			Stroke = Ui.Brush(DS.White(0.12)),
			StrokeThickness = 1.8,
			Visibility = ((!showRing) ? Visibility.Collapsed : Visibility.Visible)
		};
		Ellipse item2 = new Ellipse
		{
			Width = 20.0,
			Height = 20.0,
			Fill = Ui.Brush(DS.White(0.1)),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		TextBlock textBlock = Icons.Glyph("\ue711", 8.5, DS.Colors.TextSecondary);
		textBlock.FontWeight = FontWeights.Bold;
		Grid grid = new Grid
		{
			Width = 25.8,
			Height = 25.8,
			Margin = new Thickness(-0.9),
			IsHitTestVisible = false
		};
		Compositor compositor = Ui.Compositor;
		ringGeo = compositor.CreateEllipseGeometry();
		ringGeo.Center = new Vector2(12.9f);
		ringGeo.Radius = new Vector2(12f);
		ring = compositor.CreateSpriteShape(ringGeo);
		ring.StrokeBrush = compositor.CreateColorBrush(DS.White(0.45));
		ring.StrokeThickness = 1.8f;
		ring.StrokeStartCap = (ring.StrokeEndCap = CompositionStrokeCap.Round);
		ring.CenterPoint = new Vector2(12.9f);
		ring.RotationAngleInDegrees = -90f;
		ShapeVisual shapeVisual = compositor.CreateShapeVisual();
		shapeVisual.Size = new Vector2(25.8f);
		shapeVisual.Shapes.Add(ring);
		ElementCompositionPreview.SetElementChildVisual(grid, shapeVisual);
		grid.Visibility = ((!showRing) ? Visibility.Collapsed : Visibility.Visible);
		base.Children.Add(item);
		base.Children.Add(grid);
		base.Children.Add(item2);
		base.Children.Add(textBlock);
		WithIconHover(24.0);
	}

	public void StartCountdown(double seconds, double fraction = 1.0)
	{
		Compositor compositor = Ui.Compositor;
		ringGeo.TrimEnd = (float)fraction;
		if (!Anim.Instant)
		{
			ScalarKeyFrameAnimation scalarKeyFrameAnimation = compositor.CreateScalarKeyFrameAnimation();
			scalarKeyFrameAnimation.InsertKeyFrame(0f, (float)fraction);
			scalarKeyFrameAnimation.InsertKeyFrame(1f, 0f, compositor.CreateLinearEasingFunction());
			scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(Math.Max(0.01, seconds));
			ringGeo.StartAnimation("TrimEnd", scalarKeyFrameAnimation);
		}
	}

	public void SetProgress(double fraction)
	{
		ringGeo.TrimEnd = (float)Math.Clamp(fraction, 0.0, 1.0);
	}

	public void GlideTo(double fraction, double seconds)
	{
		Compositor compositor = Ui.Compositor;
		ScalarKeyFrameAnimation scalarKeyFrameAnimation = compositor.CreateScalarKeyFrameAnimation();
		scalarKeyFrameAnimation.InsertKeyFrame(1f, (float)Math.Clamp(fraction, 0.0, 1.0), compositor.CreateLinearEasingFunction());
		scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(Anim.Instant ? 0.001 : Math.Max(0.001, seconds));
		ringGeo.StartAnimation("TrimEnd", scalarKeyFrameAnimation);
	}
}
