using System;
using System.Numerics;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Windows.UI;

namespace HeyClicky.App.UI;

public sealed class ShadowLayer : Grid
{
	private readonly CompositionRoundedRectangleGeometry geo;

	private readonly SpriteVisual sprite;

	public ShadowLayer(double cornerRadius, Color color, double swiftRadius, double offsetY, double offsetX = 0.0)
	{
		ShadowLayer shadowLayer = this;
		base.IsHitTestVisible = false;
		Compositor compositor = Ui.Compositor;
		geo = compositor.CreateRoundedRectangleGeometry();
		geo.CornerRadius = new Vector2((float)cornerRadius);
		CompositionSpriteShape compositionSpriteShape = compositor.CreateSpriteShape(geo);
		compositionSpriteShape.FillBrush = compositor.CreateColorBrush(Colors.Black);
		ShapeVisual shapeVisual = compositor.CreateShapeVisual();
		shapeVisual.Shapes.Add(compositionSpriteShape);
		CompositionVisualSurface surface = compositor.CreateVisualSurface();
		surface.SourceVisual = shapeVisual;
		CompositionSurfaceBrush compositionSurfaceBrush = compositor.CreateSurfaceBrush(surface);
		compositionSurfaceBrush.Stretch = CompositionStretch.None;
		DropShadow dropShadow = compositor.CreateDropShadow();
		dropShadow.Color = color;
		dropShadow.BlurRadius = (float)(swiftRadius * 2.0);
		dropShadow.Offset = new Vector3((float)offsetX, (float)offsetY, 0f);
		dropShadow.Mask = compositionSurfaceBrush;
		sprite = compositor.CreateSpriteVisual();
		sprite.Shadow = dropShadow;
		ElementCompositionPreview.SetElementChildVisual(this, sprite);
		base.SizeChanged += delegate
		{
			Vector2 vector = new Vector2((float)shadowLayer.ActualWidth, (float)shadowLayer.ActualHeight);
			float value = (float)Math.Min(cornerRadius, Math.Min(shadowLayer.ActualWidth, shadowLayer.ActualHeight) / 2.0);
			shadowLayer.geo.CornerRadius = new Vector2(value);
			shadowLayer.geo.Size = vector;
			shapeVisual.Size = vector;
			surface.SourceSize = vector;
			shadowLayer.sprite.Size = vector;
		};
	}
}
